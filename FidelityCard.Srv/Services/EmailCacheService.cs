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
    /// Segna un'email come registrazione completata
    /// </summary>
    public void MarkRegistrationComplete(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        
        if (_emailCache.TryGetValue(normalizedEmail, out var entry))
        {
            entry.IsRegistrationComplete = true;
            _logger.LogInformation("Email '{Email}' segnata come registrazione completata", email);
        }
    }

    /// <summary>
    /// Conteggio totale email in cache
    /// </summary>
    public int Count => _emailCache.Count;
}
