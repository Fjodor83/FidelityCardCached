using System.Collections.Concurrent;

namespace FidelityCard.Srv.Services;

/// <summary>
/// Implementazione del servizio di cache email usando ConcurrentDictionary.
/// La cache è permanente finché l'applicazione è attiva (Singleton).
/// Non accede MAI alla tabella Fidelity - è l'unica fonte di verità per le verifiche.
/// </summary>
public class EmailCacheService : IEmailCacheService
{
    // ConcurrentDictionary thread-safe per memorizzare le email
    // Key: email (lowercase per case-insensitive matching)
    // Value: EmailCacheEntry con info aggiuntive
    private readonly ConcurrentDictionary<string, EmailCacheEntry> _emailCache = new(StringComparer.OrdinalIgnoreCase);
    
    private readonly ILogger<EmailCacheService> _logger;

    public EmailCacheService(ILogger<EmailCacheService> logger)
    {
        _logger = logger;
        _logger.LogInformation("EmailCacheService inizializzato - Cache vuota pronta per nuove registrazioni");
    }

    /// <summary>
    /// Verifica se un'email esiste nella cache
    /// </summary>
    public bool EmailExists(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var exists = _emailCache.ContainsKey(email.Trim().ToLowerInvariant());
        _logger.LogDebug("Verifica email '{Email}' in cache: {Result}", email, exists ? "TROVATA" : "NON TROVATA");
        
        return exists;
    }

    /// <summary>
    /// Aggiunge un'email alla cache
    /// </summary>
    public void AddEmail(string email, string store)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var entry = new EmailCacheEntry
        {
            Email = normalizedEmail,
            Store = store ?? "NE001",
            CdFidelity = null,
            AddedAt = DateTime.UtcNow,
            IsRegistrationComplete = false
        };

        var added = _emailCache.TryAdd(normalizedEmail, entry);
        
        if (added)
        {
            _logger.LogInformation("Email '{Email}' aggiunta alla cache (Store: {Store}). Totale in cache: {Count}", 
                email, store, _emailCache.Count);
        }
        else
        {
            _logger.LogDebug("Email '{Email}' già presente in cache, non aggiunta", email);
        }
    }

    /// <summary>
    /// Rimuove un'email dalla cache
    /// </summary>
    public void RemoveEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var removed = _emailCache.TryRemove(normalizedEmail, out _);
        
        if (removed)
        {
            _logger.LogInformation("Email '{Email}' rimossa dalla cache. Totale in cache: {Count}", 
                email, _emailCache.Count);
        }
    }

    /// <summary>
    /// Ottiene le informazioni di un'email dalla cache
    /// </summary>
    public EmailCacheEntry? GetEmailInfo(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        _emailCache.TryGetValue(normalizedEmail, out var entry);
        
        return entry;
    }

    /// <summary>
    /// Aggiorna la cache con il CdFidelity dopo la registrazione completata
    /// </summary>
    public void UpdateWithCdFidelity(string email, string cdFidelity)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(cdFidelity))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        if (_emailCache.TryGetValue(normalizedEmail, out var entry))
        {
            entry.CdFidelity = cdFidelity;
            entry.IsRegistrationComplete = true;
            _logger.LogInformation("Cache aggiornata per '{Email}' con CdFidelity={CdFidelity}", email, cdFidelity);
        }
        else
        {
            // Se l'email non è in cache (caso raro), la aggiungiamo con il CdFidelity
            var newEntry = new EmailCacheEntry
            {
                Email = normalizedEmail,
                Store = "NE001",
                CdFidelity = cdFidelity,
                AddedAt = DateTime.UtcNow,
                IsRegistrationComplete = true
            };
            _emailCache.TryAdd(normalizedEmail, newEntry);
            _logger.LogInformation("Email '{Email}' aggiunta alla cache con CdFidelity={CdFidelity}", email, cdFidelity);
        }
    }

    /// <summary>
    /// Aggiorna la cache con tutti i dati dell'utente
    /// </summary>
    public void UpdateWithFullUserData(string email, EmailCacheEntry userData)
    {
        if (string.IsNullOrWhiteSpace(email) || userData == null)
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        if (_emailCache.TryGetValue(normalizedEmail, out var entry))
        {
            // Aggiorna tutti i campi
            entry.CdFidelity = userData.CdFidelity ?? entry.CdFidelity;
            entry.Nome = userData.Nome;
            entry.Cognome = userData.Cognome;
            entry.Cellulare = userData.Cellulare;
            entry.Indirizzo = userData.Indirizzo;
            entry.Localita = userData.Localita;
            entry.Cap = userData.Cap;
            entry.Provincia = userData.Provincia;
            entry.Nazione = userData.Nazione;
            entry.Sesso = userData.Sesso;
            entry.DataNascita = userData.DataNascita;
            entry.Store = userData.Store ?? entry.Store;
            entry.IsRegistrationComplete = true;
            
            _logger.LogInformation("Cache aggiornata con dati completi per '{Email}' - Nome={Nome} {Cognome}, CdFidelity={CdFidelity}", 
                email, entry.Nome, entry.Cognome, entry.CdFidelity);
        }
        else
        {
            // Aggiungi nuovo entry con tutti i dati
            userData.Email = normalizedEmail;
            userData.IsRegistrationComplete = true;
            _emailCache.TryAdd(normalizedEmail, userData);
            _logger.LogInformation("Email '{Email}' aggiunta alla cache con dati completi", email);
        }
    }

    /// <summary>
    /// Conteggio totale email in cache
    /// </summary>
    public int Count => _emailCache.Count;
}
