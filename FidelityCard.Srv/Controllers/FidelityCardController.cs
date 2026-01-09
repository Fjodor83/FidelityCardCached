using FidelityCard.Lib.Models;
using FidelityCard.Lib.Services;
using FidelityCard.Srv.Data;
using FidelityCard.Srv.Services; // Namespace for ITokenService
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace FidelityCard.Srv.Controllers;

[ApiController]
[Route("api/[controller]")]
//  [Route("api/Fidelity")]
public class FidelityCardController(FidelityCardDbContext context, 
        ILogger<FidelityCardController> logger,
        IOptions<EmailSettings> emailSettings,
        IConfiguration config,
        IWebHostEnvironment env,
        ICardGeneratorService cardGenerator,
        IEmailService emailService,
        ITokenService tokenService,
        IEmailCacheService emailCacheService) : ControllerBase
{
    private readonly FidelityCardDbContext _context = context;

    private readonly ILogger<FidelityCardController> _logger = logger;
    private readonly IOptions<EmailSettings> _emailSettings = emailSettings;
    private readonly IConfiguration _config = config;
    private readonly IWebHostEnvironment _env = env;
    private readonly ICardGeneratorService _cardGenerator = cardGenerator;
    private readonly IEmailService _emailService = emailService;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IEmailCacheService _emailCacheService = emailCacheService;

    // GET: api/FidelityCard
    [HttpGet]
    public async Task<Fidelity> Get(string email)
    {
        return await _context.Fidelity.FirstOrDefaultAsync(f => f.Email == email) ?? new Fidelity();
    }


    // GET: api/FidelityCard/EmailValidation
    [HttpGet("[action]")]
    public async Task<IActionResult> EmailValidation(string email, string? store)
    {
        // Verifica se l'utente esiste già usando la CACHE (non più il database)
        var userExists = _emailCacheService.EmailExists(email);

        // Genero token usando il servizio
        var token = _tokenService.GenerateToken(email, store ?? "NE001");

        if (userExists)
        {
            // UTENTE ESISTENTE IN CACHE: Invio link per ACCEDERE AL PROFILO
            var cachedInfo = _emailCacheService.GetEmailInfo(email);
            var url = $"{Request.Scheme}://{_config.GetValue<string>("ClientHost")}/profilo?token={token}";
            await _emailService.InviaEmailAccessoProfiloAsync(email, "Cliente", url);
            
            _logger.LogInformation("Email '{Email}' trovata in cache - Inviato link accesso profilo", email);
            return Ok(new { userExists = true });
        }
        else
        {
            // NUOVO UTENTE (non in cache): Aggiungo alla cache e invio link per COMPLETARE REGISTRAZIONE
            _emailCacheService.AddEmail(email, store ?? "NE001");
            
            var url = $"{Request.Scheme}://{_config.GetValue<string>("ClientHost")}/Fidelity-form?token={token}";
            await _emailService.InviaEmailVerificaAsync(email, "Cliente", token, url, store);
            
            _logger.LogInformation("Email '{Email}' aggiunta in cache - Inviato link registrazione (Store: {Store})", email, store ?? "NE001");
            return Ok(new { userExists = false });
        }
    }

    // GET: api/FidelityCard/EmailConfirmation
    [HttpGet("[action]")]
    public async Task<string> EmailConfirmation(string token)
    {
        // Utilizzo il servizio per recuperare i dati del token
        // Questo include anche la logica di cleanup (implementata nel servizio per ora)
        return await _tokenService.GetTokenDataAsync(token);
    }

    // GET: api/FidelityCard/Profile
    [HttpGet("Profile")]
    public async Task<IActionResult> GetProfile(string token)
    {
        // Recupero contenuto token dal servizio
        string fileContent = await _tokenService.GetTokenDataAsync(token);

        if (string.IsNullOrEmpty(fileContent))
        {
             _logger.LogWarning("GetProfile: Token non valido o scaduto - token={Token}", token);
             return NotFound("Token non valido o scaduto");
        }

        string[] param = fileContent.Split("\r\n");
        if (param.Length < 2) 
        {
            _logger.LogWarning("GetProfile: Formato token non valido - contenuto={Content}", fileContent);
            return BadRequest("Formato token non valido");
        }

        string email = param[1].Trim().ToLowerInvariant();
        _logger.LogInformation("GetProfile: Cercando utente con email={Email}", email);

        // Cerco nel database usando confronto case-insensitive
        var user = await _context.Fidelity.FirstOrDefaultAsync(f => f.Email.ToLower() == email);
        
        if (user == null)
        {
             _logger.LogWarning("GetProfile: Utente non trovato per email={Email}", email);
             return NotFound("Utente non trovato");
        }

        _logger.LogInformation("GetProfile: Trovato utente {Nome} {Cognome} con CdFidelity={CdFidelity}", 
            user.Nome, user.Cognome, user.CdFidelity);
        return Ok(user);
    }

    // GET: api/FidelityCard/QRCode/{code}
    [HttpGet("QRCode/{code}")]
    public async Task<IActionResult> GetQrCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest();

        // Genera solo il QR Code (immagine)
         using var generator = new QRCoder.QRCodeGenerator();
         var qrCodeData = generator.CreateQrCode(code, QRCoder.QRCodeGenerator.ECCLevel.Q);
         using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
         var qrCodeBytes = qrCode.GetGraphic(20);

         return File(qrCodeBytes, "image/png");
    }

    // POST: api/FidelityCard
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Fidelity fidelity)
    {

        if (fidelity == null)
        {
            Console.WriteLine("Modello nullo");
            return BadRequest();
        }

        _context.Fidelity.Add(fidelity);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Utente salvato nel database: {Email}, CdFidelity: {CdFidelity}", fidelity.Email, fidelity.CdFidelity);
            
            // Generazione Card e Invio Email
            try
            {
                _logger.LogInformation("Inizio generazione card digitale per {Email}...", fidelity.Email);
                var cardBytes = await _cardGenerator.GeneraCardDigitaleAsync(fidelity, fidelity.Store);
                _logger.LogInformation("Card generata con successo ({Size} bytes). Invio email benvenuto...", cardBytes?.Length ?? 0);
                
                var emailResult = await _emailService.InviaEmailBenvenutoAsync(fidelity.Email ?? "", fidelity.Nome ?? "Cliente", fidelity.CdFidelity ?? "", cardBytes);
                
                if (emailResult)
                {
                    _logger.LogInformation("Email di benvenuto inviata con successo a {Email}", fidelity.Email);
                }
                else
                {
                    _logger.LogWarning("Invio email di benvenuto fallito per {Email} - Il servizio ha restituito false", fidelity.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante generazione card o invio email benvenuto per {Email}. Dettaglio: {Message}", fidelity.Email, ex.Message);
                // Non blocchiamo il ritorno OK, la registrazione è avvenuta
            }

            return Ok(fidelity);
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
            {
                return StatusCode(500, ex.InnerException.Message);
            }
            else 
            {
                return StatusCode(500, ex.Message);
            }

        }
    }

    // GET: api/FidelityCard/CacheStatus - Endpoint di debug per visualizzare lo stato della cache
    [HttpGet("[action]")]
    public IActionResult CacheStatus()
    {
        return Ok(new 
        { 
            totalEmailsInCache = _emailCacheService.Count,
            message = "Cache attiva - Le email vengono memorizzate solo durante il processo di verifica"
        });
    }

    // DELETE: api/FidelityCard/ClearEmailFromCache - Rimuove un'email dalla cache (opzionale)
    [HttpDelete("ClearEmailFromCache")]
    public IActionResult ClearEmailFromCache(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email richiesta");

        _emailCacheService.RemoveEmail(email);
        _logger.LogInformation("Email '{Email}' rimossa dalla cache tramite API", email);
        
        return Ok(new { message = $"Email '{email}' rimossa dalla cache", currentCount = _emailCacheService.Count });
    }

}

