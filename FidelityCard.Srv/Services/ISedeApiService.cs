namespace FidelityCard.Srv.Services;

/// <summary>
/// Modello per la risposta dell'utente dalla sede
/// </summary>
public class SedeUserInfo
{
    public string? CdFidelity { get; set; }
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
    public string? Email { get; set; }
    public string? Cellulare { get; set; }
    public string? Indirizzo { get; set; }
    public string? Localita { get; set; }
    public string? Cap { get; set; }
    public string? Provincia { get; set; }
    public string? Nazione { get; set; }
    public string? Sesso { get; set; }
    public DateTime? DataNascita { get; set; }
    public string? Store { get; set; }
    public bool Found { get; set; } = false;
}

/// <summary>
/// Servizio per comunicare con l'API della sede centrale (NEFidelity)
/// </summary>
public interface ISedeApiService
{
    /// <summary>
    /// Cerca un utente per email nella tabella NEFidelity della sede
    /// </summary>
    /// <param name="email">Email da cercare</param>
    /// <returns>Informazioni utente se trovato, null se non trovato</returns>
    Task<SedeUserInfo?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Cerca un utente per CdFidelity nella tabella NEFidelity della sede
    /// </summary>
    /// <param name="cdFidelity">Codice fidelity da cercare</param>
    /// <returns>Informazioni utente se trovato, null se non trovato</returns>
    Task<SedeUserInfo?> GetUserByCdFidelityAsync(string cdFidelity);
}
