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
    /// <param name="email">Email da verificare</param>
    /// <returns>True se l'email è presente in cache, False altrimenti</returns>
    bool EmailExists(string email);

    /// <summary>
    /// Aggiunge un'email alla cache (quando inizia il processo di verifica)
    /// </summary>
    /// <param name="email">Email da aggiungere</param>
    /// <param name="store">Codice negozio associato</param>
    void AddEmail(string email, string store);

    /// <summary>
    /// Rimuove un'email dalla cache (opzionale, per cleanup)
    /// </summary>
    /// <param name="email">Email da rimuovere</param>
    void RemoveEmail(string email);

    /// <summary>
    /// Ottiene le informazioni associate a un'email in cache
    /// </summary>
    /// <param name="email">Email da cercare</param>
    /// <returns>Informazioni email o null se non trovata</returns>
    EmailCacheEntry? GetEmailInfo(string email);

    /// <summary>
    /// Ottiene il conteggio totale delle email in cache
    /// </summary>
    int Count { get; }
}

/// <summary>
/// Entry della cache per memorizzare info sull'email
/// </summary>
public class EmailCacheEntry
{
    public string Email { get; set; } = string.Empty;
    public string Store { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public bool IsRegistrationComplete { get; set; } = false;
}
