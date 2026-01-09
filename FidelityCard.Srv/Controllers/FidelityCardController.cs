using FidelityCard.Lib.Models;
using FidelityCard.Lib.Services;
using FidelityCard.Srv.Data;
using FidelityCard.Srv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace FidelityCard.Srv.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FidelityCardController(
        FidelityCardDbContext context,
        ILogger<FidelityCardController> logger,
        IOptions<EmailSettings> emailSettings,
        IConfiguration config,
        IWebHostEnvironment env,
        ICardGeneratorService cardGenerator,
        IEmailService emailService,
        ITokenService tokenService,
        IEmailCacheService emailCacheService,
        ISedeApiService sedeApiService) : ControllerBase
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
    private readonly ISedeApiService _sedeApiService = sedeApiService;

    // GET: api/FidelityCard/EmailValidation
    // FLUSSO SENZA USARE TABELLA FIDELITY LOCALE:
    // 1. Verifica in CACHE se l'email esiste e ha CdFidelity
    // 2. Se non in cache o senza CdFidelity -> Chiama API SEDE (NEFidelity)
    // 3. Se trovato in sede -> Salva in cache e invia link profilo
    // 4. Se non trovato -> Invia link registrazione
    [HttpGet("[action]")]
    public async Task<IActionResult> EmailValidation(string email, string? store)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? "";
        
        if (string.IsNullOrEmpty(normalizedEmail))
        {
            return BadRequest("Email richiesta");
        }

        // STEP 1: Verifica in CACHE
        var cachedInfo = _emailCacheService.GetEmailInfo(normalizedEmail);
        
        if (cachedInfo?.CdFidelity != null)
        {
            // UTENTE IN CACHE CON CdFidelity -> Invia link profilo
            _logger.LogInformation("EmailValidation: Email '{Email}' trovata in cache con CdFidelity={CdFidelity}", 
                normalizedEmail, cachedInfo.CdFidelity);
            
            var profileToken = _tokenService.GenerateProfileToken(normalizedEmail, store ?? cachedInfo.Store, cachedInfo.CdFidelity);
            var url = $"{Request.Scheme}://{_config.GetValue<string>("ClientHost")}/profilo?token={profileToken}";
            await _emailService.InviaEmailAccessoProfiloAsync(normalizedEmail, "Cliente", url);
            
            return Ok(new { userExists = true });
        }

        // STEP 2: Non in cache o senza CdFidelity -> Chiama API SEDE (NEFidelity)
        _logger.LogInformation("EmailValidation: Email '{Email}' non in cache, verifico in sede (NEFidelity)...", normalizedEmail);
        
        var sedeUser = await _sedeApiService.GetUserByEmailAsync(normalizedEmail);

        if (sedeUser != null && sedeUser.Found && !string.IsNullOrEmpty(sedeUser.CdFidelity))
        {
            // STEP 3: UTENTE TROVATO IN SEDE -> Salva in cache e invia link profilo
            _logger.LogInformation("EmailValidation: Utente trovato in sede - CdFidelity={CdFidelity}, Nome={Nome}", 
                sedeUser.CdFidelity, sedeUser.Nome);
            
            // Salvo in cache con tutti i dati dalla sede
            _emailCacheService.AddEmail(normalizedEmail, store ?? sedeUser.Store ?? "NE001");
            _emailCacheService.UpdateWithCdFidelity(normalizedEmail, sedeUser.CdFidelity);
            
            // Aggiorno la cache con i dati completi dell'utente
            UpdateCacheWithUserData(normalizedEmail, sedeUser);
            
            var profileToken = _tokenService.GenerateProfileToken(normalizedEmail, store ?? sedeUser.Store ?? "NE001", sedeUser.CdFidelity);
            var url = $"{Request.Scheme}://{_config.GetValue<string>("ClientHost")}/profilo?token={profileToken}";
            await _emailService.InviaEmailAccessoProfiloAsync(normalizedEmail, sedeUser.Nome ?? "Cliente", url);
            
            return Ok(new { userExists = true });
        }

        // STEP 4: UTENTE NON TROVATO -> Invia link registrazione
        _logger.LogInformation("EmailValidation: Utente non trovato in sede, invio link registrazione per '{Email}'", normalizedEmail);
        
        // Aggiungo alla cache (senza CdFidelity per ora)
        if (cachedInfo == null)
        {
            _emailCacheService.AddEmail(normalizedEmail, store ?? "NE001");
        }
        
        var token = _tokenService.GenerateToken(normalizedEmail, store ?? "NE001");
        var registrationUrl = $"{Request.Scheme}://{_config.GetValue<string>("ClientHost")}/Fidelity-form?token={token}";
        await _emailService.InviaEmailVerificaAsync(normalizedEmail, "Cliente", token, registrationUrl, store);
        
        return Ok(new { userExists = false });
    }

    // GET: api/FidelityCard/EmailConfirmation
    [HttpGet("[action]")]
    public async Task<string> EmailConfirmation(string token)
    {
        return await _tokenService.GetTokenDataAsync(token);
    }

    // GET: api/FidelityCard/Profile
    // Recupera i dati dell'utente dalla SEDE (NEFidelity), non dalla tabella locale
    [HttpGet("Profile")]
    public async Task<IActionResult> GetProfile(string token)
    {
        string fileContent = await _tokenService.GetTokenDataAsync(token);

        if (string.IsNullOrEmpty(fileContent))
        {
            _logger.LogWarning("GetProfile: Token non valido o scaduto");
            return NotFound("Token non valido o scaduto");
        }

        string[] param = fileContent.Split("\r\n");
        if (param.Length < 2) 
        {
            _logger.LogWarning("GetProfile: Formato token non valido");
            return BadRequest("Formato token non valido");
        }

        string store = param[0];
        string email = param[1].Trim().ToLowerInvariant();
        string? cdFidelity = param.Length >= 3 ? param[2].Trim() : null;

        SedeUserInfo? userInfo = null;

        // Prima provo a recuperare dalla cache (se ho già i dati completi)
        var cachedInfo = _emailCacheService.GetEmailInfo(email);
        
        // Se ho il CdFidelity, cerco per quello nella SEDE
        if (!string.IsNullOrEmpty(cdFidelity))
        {
            _logger.LogInformation("GetProfile: Cercando utente in sede con CdFidelity={CdFidelity}", cdFidelity);
            userInfo = await _sedeApiService.GetUserByCdFidelityAsync(cdFidelity);
        }
        
        // Fallback: cerca per email nella SEDE
        if (userInfo == null || !userInfo.Found)
        {
            _logger.LogInformation("GetProfile: Cercando utente in sede con email={Email}", email);
            userInfo = await _sedeApiService.GetUserByEmailAsync(email);
        }
        
        if (userInfo == null || !userInfo.Found)
        {
            _logger.LogWarning("GetProfile: Utente non trovato in sede per email={Email}, CdFidelity={CdFidelity}", email, cdFidelity);
            return NotFound("Utente non trovato");
        }

        // Converto SedeUserInfo in Fidelity per mantenere compatibilità con il frontend
        var fidelity = new Fidelity
        {
            CdFidelity = userInfo.CdFidelity ?? "",
            Nome = userInfo.Nome ?? "",
            Cognome = userInfo.Cognome ?? "",
            Email = userInfo.Email ?? email,
            Cellulare = userInfo.Cellulare ?? "",
            Indirizzo = userInfo.Indirizzo ?? "",
            Localita = userInfo.Localita ?? "",
            Cap = userInfo.Cap ?? "",
            Provincia = userInfo.Provincia ?? "",
            Nazione = userInfo.Nazione ?? "",
            Sesso = userInfo.Sesso ?? "",
            DataNascita = userInfo.DataNascita,
            Store = userInfo.Store ?? store
        };

        _logger.LogInformation("GetProfile: Trovato utente {Nome} {Cognome} con CdFidelity={CdFidelity}", 
            fidelity.Nome, fidelity.Cognome, fidelity.CdFidelity);
        
        return Ok(fidelity);
    }

    // GET: api/FidelityCard/QRCode/{code}
    [HttpGet("QRCode/{code}")]
    public async Task<IActionResult> GetQrCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest();

        using var generator = new QRCoder.QRCodeGenerator();
        var qrCodeData = generator.CreateQrCode(code, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        return File(qrCodeBytes, "image/png");
    }

    // POST: api/FidelityCard
    // Questo endpoint salva nella tabella locale Fidelity E aggiorna la cache
    // La registrazione principale avviene tramite API sede nel frontend
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Fidelity fidelity)
    {
        if (fidelity == null)
        {
            return BadRequest("Dati non validi");
        }

        fidelity.Email = fidelity.Email?.Trim().ToLowerInvariant() ?? "";

        _context.Fidelity.Add(fidelity);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Utente salvato nel database locale: {Email}, CdFidelity: {CdFidelity}", 
                fidelity.Email, fidelity.CdFidelity);
            
            // Aggiorno la cache con il CdFidelity
            _emailCacheService.UpdateWithCdFidelity(fidelity.Email, fidelity.CdFidelity);
            
            // Generazione Card e Invio Email
            try
            {
                var cardBytes = await _cardGenerator.GeneraCardDigitaleAsync(fidelity, fidelity.Store);
                var emailResult = await _emailService.InviaEmailBenvenutoAsync(
                    fidelity.Email ?? "", 
                    fidelity.Nome ?? "Cliente", 
                    fidelity.CdFidelity ?? "", 
                    cardBytes);
                
                if (emailResult)
                {
                    _logger.LogInformation("Email di benvenuto inviata a {Email}", fidelity.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante generazione card o invio email");
            }

            return Ok(fidelity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio dell'utente");
            return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
        }
    }

    // GET: api/FidelityCard/CacheStatus
    [HttpGet("[action]")]
    public IActionResult CacheStatus()
    {
        return Ok(new 
        { 
            totalEmailsInCache = _emailCacheService.Count,
            message = "Cache attiva - Verifica email tramite cache + API Sede (NEFidelity)"
        });
    }

    // DELETE: api/FidelityCard/ClearEmailFromCache
    [HttpDelete("ClearEmailFromCache")]
    public IActionResult ClearEmailFromCache(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Email richiesta");

        _emailCacheService.RemoveEmail(email);
        return Ok(new { message = $"Email '{email}' rimossa dalla cache", currentCount = _emailCacheService.Count });
    }

    // Helper per aggiornare la cache con i dati completi dell'utente dalla sede
    private void UpdateCacheWithUserData(string email, SedeUserInfo userInfo)
    {
        // Per ora salviamo solo il CdFidelity
        // In futuro potremmo estendere EmailCacheEntry per memorizzare più dati
        if (!string.IsNullOrEmpty(userInfo.CdFidelity))
        {
            _emailCacheService.UpdateWithCdFidelity(email, userInfo.CdFidelity);
        }
    }
}
