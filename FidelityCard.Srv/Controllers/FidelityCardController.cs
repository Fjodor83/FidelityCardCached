using FidelityCard.Lib.Models;
using FidelityCard.Lib.Services;
using FidelityCard.Srv.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace FidelityCard.Srv.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FidelityCardController(
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
            
            _logger.LogInformation("EmailValidation: Token generato={Token}, URL={Url}", profileToken, url);
            
            await _emailService.InviaEmailAccessoProfiloAsync(normalizedEmail, cachedInfo.Nome ?? "Cliente", url);
            
            return Ok(new { userExists = true });
        }

        // STEP 2: Non in cache -> Chiama API SEDE (se configurata)
        _logger.LogInformation("EmailValidation: Email '{Email}' non in cache, verifico in sede (NEFidelity)...", normalizedEmail);
        
        var sedeUser = await _sedeApiService.GetUserByEmailAsync(normalizedEmail);

        if (sedeUser != null && sedeUser.Found && !string.IsNullOrEmpty(sedeUser.CdFidelity))
        {
            // UTENTE TROVATO IN SEDE -> Salva in cache con tutti i dati
            _logger.LogInformation("EmailValidation: Utente trovato in sede - CdFidelity={CdFidelity}, Nome={Nome}", 
                sedeUser.CdFidelity, sedeUser.Nome);
            
            // Salvo tutti i dati in cache
            var cacheEntry = new EmailCacheEntry
            {
                Email = normalizedEmail,
                Store = store ?? sedeUser.Store ?? "NE001",
                CdFidelity = sedeUser.CdFidelity,
                Nome = sedeUser.Nome,
                Cognome = sedeUser.Cognome,
                Cellulare = sedeUser.Cellulare,
                Indirizzo = sedeUser.Indirizzo,
                Localita = sedeUser.Localita,
                Cap = sedeUser.Cap,
                Provincia = sedeUser.Provincia,
                Nazione = sedeUser.Nazione,
                Sesso = sedeUser.Sesso,
                DataNascita = sedeUser.DataNascita,
                IsRegistrationComplete = true
            };
            
            _emailCacheService.AddEmail(normalizedEmail, store ?? sedeUser.Store ?? "NE001");
            _emailCacheService.UpdateWithFullUserData(normalizedEmail, cacheEntry);
            
            var profileToken = _tokenService.GenerateProfileToken(normalizedEmail, store ?? sedeUser.Store ?? "NE001", sedeUser.CdFidelity);
            var url = $"{Request.Scheme}://{_config.GetValue<string>("ClientHost")}/profilo?token={profileToken}";
            await _emailService.InviaEmailAccessoProfiloAsync(normalizedEmail, sedeUser.Nome ?? "Cliente", url);
            
            return Ok(new { userExists = true });
        }

        // STEP 3: UTENTE NON TROVATO -> Invia link registrazione
        _logger.LogInformation("EmailValidation: Utente non trovato, invio link registrazione per '{Email}'", normalizedEmail);
        
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
    // Recupera i dati dalla CACHE, con fallback alla SEDE se configurata
    [HttpGet("Profile")]
    public async Task<IActionResult> GetProfile(string token)
    {
        _logger.LogInformation("GetProfile: Richiesta con token={Token}", token);
        
        string fileContent = await _tokenService.GetTokenDataAsync(token);

        if (string.IsNullOrEmpty(fileContent))
        {
            _logger.LogWarning("GetProfile: Token non valido o scaduto - token ricevuto={Token}", token);
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

        // STEP 1: Prova a recuperare dalla CACHE
        var cachedInfo = _emailCacheService.GetEmailInfo(email);
        
        if (cachedInfo != null && cachedInfo.IsRegistrationComplete && !string.IsNullOrEmpty(cachedInfo.CdFidelity))
        {
            _logger.LogInformation("GetProfile: Dati trovati in cache per {Email}, CdFidelity={CdFidelity}", 
                email, cachedInfo.CdFidelity);
            
            var fidelity = new Fidelity
            {
                CdFidelity = cachedInfo.CdFidelity ?? cdFidelity ?? "",
                Nome = cachedInfo.Nome ?? "",
                Cognome = cachedInfo.Cognome ?? "",
                Email = cachedInfo.Email ?? email,
                Cellulare = cachedInfo.Cellulare ?? "",
                Indirizzo = cachedInfo.Indirizzo ?? "",
                Localita = cachedInfo.Localita ?? "",
                Cap = cachedInfo.Cap ?? "",
                Provincia = cachedInfo.Provincia ?? "",
                Nazione = cachedInfo.Nazione ?? "",
                Sesso = cachedInfo.Sesso ?? "",
                DataNascita = cachedInfo.DataNascita,
                Store = cachedInfo.Store ?? store
            };
            
            _logger.LogInformation("GetProfile: Restituisco dati dalla cache - {Nome} {Cognome}, CdFidelity={CdFidelity}", 
                fidelity.Nome, fidelity.Cognome, fidelity.CdFidelity);
            
            return Ok(fidelity);
        }

        // STEP 2: Fallback alla SEDE (se configurata)
        SedeUserInfo? userInfo = null;

        if (!string.IsNullOrEmpty(cdFidelity))
        {
            _logger.LogInformation("GetProfile: Cache incompleta, cercando in sede con CdFidelity={CdFidelity}", cdFidelity);
            userInfo = await _sedeApiService.GetUserByCdFidelityAsync(cdFidelity);
        }
        
        if (userInfo == null || !userInfo.Found)
        {
            _logger.LogInformation("GetProfile: Cercando in sede con email={Email}", email);
            userInfo = await _sedeApiService.GetUserByEmailAsync(email);
        }
        
        if (userInfo != null && userInfo.Found)
        {
            // Salva in cache per future richieste
            var cacheEntry = new EmailCacheEntry
            {
                Email = email,
                Store = userInfo.Store ?? store,
                CdFidelity = userInfo.CdFidelity,
                Nome = userInfo.Nome,
                Cognome = userInfo.Cognome,
                Cellulare = userInfo.Cellulare,
                Indirizzo = userInfo.Indirizzo,
                Localita = userInfo.Localita,
                Cap = userInfo.Cap,
                Provincia = userInfo.Provincia,
                Nazione = userInfo.Nazione,
                Sesso = userInfo.Sesso,
                DataNascita = userInfo.DataNascita,
                IsRegistrationComplete = true
            };
            _emailCacheService.UpdateWithFullUserData(email, cacheEntry);

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

            _logger.LogInformation("GetProfile: Trovato in sede - {Nome} {Cognome}, CdFidelity={CdFidelity}", 
                fidelity.Nome, fidelity.Cognome, fidelity.CdFidelity);
            
            return Ok(fidelity);
        }

        // STEP 3: Se abbiamo almeno il CdFidelity dal token, restituiamo dati minimali
        if (!string.IsNullOrEmpty(cdFidelity))
        {
            _logger.LogWarning("GetProfile: Sede non disponibile, restituisco dati minimali per CdFidelity={CdFidelity}", cdFidelity);
            
            var fidelity = new Fidelity
            {
                CdFidelity = cdFidelity,
                Nome = cachedInfo?.Nome ?? "",
                Cognome = cachedInfo?.Cognome ?? "",
                Email = email,
                Cellulare = cachedInfo?.Cellulare ?? "",
                Indirizzo = cachedInfo?.Indirizzo ?? "",
                Localita = cachedInfo?.Localita ?? "",
                Cap = cachedInfo?.Cap ?? "",
                Provincia = cachedInfo?.Provincia ?? "",
                Nazione = cachedInfo?.Nazione ?? "",
                Sesso = cachedInfo?.Sesso ?? "",
                DataNascita = cachedInfo?.DataNascita,
                Store = cachedInfo?.Store ?? store
            };
            
            return Ok(fidelity);
        }

        _logger.LogWarning("GetProfile: Utente non trovato per email={Email}", email);
        return NotFound("Utente non trovato");
    }

    // GET: api/FidelityCard/QRCode/{code}
    [HttpGet("QRCode/{code}")]
    public IActionResult GetQrCode(string code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest();

        using var generator = new QRCoder.QRCodeGenerator();
        var qrCodeData = generator.CreateQrCode(code, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        return File(qrCodeBytes, "image/png");
    }

    // POST: api/FidelityCard
    // Salva TUTTI i dati dell'utente nella CACHE e invia email di benvenuto
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Fidelity fidelity)
    {
        if (fidelity == null)
        {
            return BadRequest("Dati non validi");
        }

        var normalizedEmail = fidelity.Email?.Trim().ToLowerInvariant() ?? "";
        
        if (string.IsNullOrEmpty(normalizedEmail) || string.IsNullOrEmpty(fidelity.CdFidelity))
        {
            return BadRequest("Email e CdFidelity sono richiesti");
        }

        _logger.LogInformation("Create: Salvataggio in cache per {Email}, CdFidelity={CdFidelity}", 
            normalizedEmail, fidelity.CdFidelity);

        // Salvo TUTTI i dati dell'utente nella cache
        var cacheEntry = new EmailCacheEntry
        {
            Email = normalizedEmail,
            Store = fidelity.Store ?? "NE001",
            CdFidelity = fidelity.CdFidelity,
            Nome = fidelity.Nome,
            Cognome = fidelity.Cognome,
            Cellulare = fidelity.Cellulare,
            Indirizzo = fidelity.Indirizzo,
            Localita = fidelity.Localita,
            Cap = fidelity.Cap,
            Provincia = fidelity.Provincia,
            Nazione = fidelity.Nazione,
            Sesso = fidelity.Sesso,
            DataNascita = fidelity.DataNascita,
            IsRegistrationComplete = true
        };
        
        _emailCacheService.AddEmail(normalizedEmail, fidelity.Store ?? "NE001");
        _emailCacheService.UpdateWithFullUserData(normalizedEmail, cacheEntry);
        
        // Generazione Card e Invio Email di benvenuto
        try
        {
            var cardBytes = await _cardGenerator.GeneraCardDigitaleAsync(fidelity, fidelity.Store);
            var emailResult = await _emailService.InviaEmailBenvenutoAsync(
                normalizedEmail, 
                fidelity.Nome ?? "Cliente", 
                fidelity.CdFidelity, 
                cardBytes);
            
            if (emailResult)
            {
                _logger.LogInformation("Email di benvenuto inviata con successo a {Email}", normalizedEmail);
            }
            else
            {
                _logger.LogWarning("Invio email di benvenuto fallito per {Email}", normalizedEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante generazione card o invio email per {Email}", normalizedEmail);
        }

        return Ok(fidelity);
    }

    // GET: api/FidelityCard/CacheStatus
    [HttpGet("[action]")]
    public IActionResult CacheStatus()
    {
        return Ok(new 
        { 
            totalEmailsInCache = _emailCacheService.Count,
            message = "Cache attiva con dati completi degli utenti"
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
}
