namespace FidelityCard.Srv.Services;

/// <summary>
/// Servizio di cache in memoria per la verifica delle email registrate.
/// Utilizza ConcurrentDictionary per gestire le email senza accedere al database.
/// </summary>
public interface IEmailCacheService
{
    /// <summary>
    /// Verifica se un'email esiste nella cache
    /// </summary>
    bool EmailExists(string email);

    /// <summary>
    /// Aggiunge un'email alla cache (quando inizia il processo di verifica)
    /// </summary>
    void AddEmail(string email, string store);

    /// <summary>
    /// Rimuove un'email dalla cache
    /// </summary>
    void RemoveEmail(string email);

    /// <summary>
    /// Ottiene le informazioni associate a un'email in cache
    /// </summary>
    EmailCacheEntry? GetEmailInfo(string email);

    /// <summary>
    /// Aggiorna la cache con il CdFidelity dopo la registrazione completata
    /// </summary>
    void UpdateWithCdFidelity(string email, string cdFidelity);

    /// <summary>
    /// Aggiorna la cache con tutti i dati dell'utente dopo la registrazione
    /// </summary>
    void UpdateWithFullUserData(string email, EmailCacheEntry userData);

    /// <summary>
    /// Ottiene il conteggio totale delle email in cache
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Entry della cache per memorizzare tutti i dati dell'utente
/// </summary>
public class EmailCacheEntry
{
    public string Email { get; set; } = string.Empty;
    public string Store { get; set; } = string.Empty;
    public string? CdFidelity { get; set; } = null;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsRegistrationComplete { get; set; } = false;
    
    // Dati anagrafici dell'utente
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Cellulare { get; set; }
    public string? Indirizzo { get; set; }
    public string? Localita { get; set; }
    public string? Cap { get; set; }
    public string? Provincia { get; set; }
    public string? Nazione { get; set; }
    public string? Sesso { get; set; }
    public DateTime? DataNascita { get; set; }
}
