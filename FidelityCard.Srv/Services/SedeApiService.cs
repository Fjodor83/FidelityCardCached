using System.Text.Json;
using FidelityCard.Lib.Models;

namespace FidelityCard.Srv.Services;

/// <summary>
/// Implementazione del servizio per comunicare con l'API della sede centrale (NEFidelity)
/// </summary>
public class SedeApiService : ISedeApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<SedeApiService> _logger;

    public SedeApiService(HttpClient httpClient, IConfiguration config, ILogger<SedeApiService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Cerca un utente per email nella tabella NEFidelity della sede
    /// </summary>
    public async Task<SedeUserInfo?> GetUserByEmailAsync(string email)
    {
        try
        {
            var endpointSede = _config.GetValue<string>("SedeSettings:EndpointSede");
            var dbNameSede = _config.GetValue<string>("SedeSettings:DbNameSede");

            if (string.IsNullOrEmpty(endpointSede) || string.IsNullOrEmpty(dbNameSede))
            {
                _logger.LogWarning("SedeApiService: EndpointSede o DbNameSede non configurati");
                return null;
            }

            var request = new RequestSede
            {
                Request = new Request
                {
                    DbName = dbNameSede,
                    SpName = "xTSP_API_Get_Fidelity_ByEmail",
                    CalledFrom = "APP FIDELITY",
                    CalledOperator = ""
                },
                Parameters = new[]
                {
                    new ParamElement { Name = "email", Value = email.Trim().ToLowerInvariant() }
                }
            };

            _logger.LogInformation("SedeApiService: Chiamata API sede per email={Email}", email);

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede. StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (json == null)
            {
                _logger.LogWarning("SedeApiService: Risposta JSON nulla dalla sede");
                return null;
            }

            return ParseUserFromResponse(json, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante la chiamata API sede per email={Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Registra o aggiorna un utente nella tabella NEFidelity della sede
    /// </summary>
    public async Task<string?> RegisterUserAsync(Fidelity fidelity)
    {
        try
        {
            var endpointSede = _config.GetValue<string>("SedeSettings:EndpointSede");
            var dbNameSede = _config.GetValue<string>("SedeSettings:DbNameSede");

            if (string.IsNullOrEmpty(endpointSede) || string.IsNullOrEmpty(dbNameSede))
            {
                _logger.LogWarning("SedeApiService: EndpointSede o DbNameSede non configurati");
                return null;
            }

            var request = new RequestSede
            {
                Request = new Request
                {
                    DbName = dbNameSede,
                    SpName = "xTSP_API_Put_Fidelity",
                    CalledFrom = "APP FIDELIT", 
                    CalledOperator = ""
                },
                Parameters = new[]
                {
                    new ParamElement { Name = "store", Value = fidelity.Store },
                    new ParamElement { Name = "tipo", Value = "D" }, // D = Digitale?
                    new ParamElement { Name = "nome", Value = fidelity.Nome },
                    new ParamElement { Name = "cognome", Value = fidelity.Cognome },
                    new ParamElement { Name = "sesso", Value = fidelity.Sesso },
                    new ParamElement { Name = "data_nascita", Value = fidelity.DataNascita?.ToString("yyyyMMdd") },
                    new ParamElement { Name = "indirizzo", Value = fidelity.Indirizzo },
                    new ParamElement { Name = "localita", Value = fidelity.Localita },
                    new ParamElement { Name = "cap", Value = fidelity.Cap },
                    new ParamElement { Name = "provincia", Value = fidelity.Provincia },
                    new ParamElement { Name = "nazione", Value = fidelity.Nazione },
                    new ParamElement { Name = "cellulare", Value = fidelity.Cellulare },
                    new ParamElement { Name = "email", Value = fidelity.Email }
                }
            };

            _logger.LogInformation("SedeApiService: Registrazione utente {Email} store {Store}", fidelity.Email, fidelity.Store);

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede (Register). StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (json == null)
            {
                _logger.LogWarning("SedeApiService: Risposta JSON nulla dalla sede");
                return null;
            }

            // Parse response to find 'codice_fidelity'
            var jsonRoot = json.RootElement;
            if (jsonRoot.TryGetProperty("response", out var responseArray))
            {
                foreach (var respElement in responseArray.EnumerateArray())
                {
                    if (respElement.TryGetProperty("dataset", out var datasetArray))
                    {
                        foreach (var dataElement in datasetArray.EnumerateArray())
                        {
                            var cdFidelity = GetStringProperty(dataElement, "codice_fidelity");
                            if (!string.IsNullOrEmpty(cdFidelity))
                            {
                                return cdFidelity;
                            }
                        }
                    }
                }
            }
            
            _logger.LogWarning("SedeApiService: codice_fidelity non trovato nella risposta");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante RegisterUserAsync per {Email}", fidelity.Email);
            return null;
        }
    }

    /// <summary>
    /// Cerca un utente per CdFidelity nella tabella NEFidelity della sede
    /// </summary>
    public async Task<SedeUserInfo?> GetUserByCdFidelityAsync(string cdFidelity)
    {
        try
        {
            var endpointSede = _config.GetValue<string>("SedeSettings:EndpointSede");
            var dbNameSede = _config.GetValue<string>("SedeSettings:DbNameSede");

            if (string.IsNullOrEmpty(endpointSede) || string.IsNullOrEmpty(dbNameSede))
            {
                _logger.LogWarning("SedeApiService: EndpointSede o DbNameSede non configurati");
                return null;
            }

            var request = new RequestSede
            {
                Request = new Request
                {
                    DbName = dbNameSede,
                    SpName = "xTSP_API_Get_Fidelity_ByCodice",
                    CalledFrom = "APP FIDELITY",
                    CalledOperator = ""
                },
                Parameters = new[]
                {
                    new ParamElement { Name = "codice_fidelity", Value = cdFidelity }
                }
            };

            _logger.LogInformation("SedeApiService: Chiamata API sede per CdFidelity={CdFidelity}", cdFidelity);

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede. StatusCode={StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (json == null)
            {
                _logger.LogWarning("SedeApiService: Risposta JSON nulla dalla sede");
                return null;
            }

            return ParseUserFromResponse(json, null, cdFidelity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante la chiamata API sede per CdFidelity={CdFidelity}", cdFidelity);
            return null;
        }
    }

    /// <summary>
    /// Parsa la risposta JSON della sede e estrae le informazioni utente
    /// Formato risposta atteso:
    /// {
    ///   "response": [
    ///     {
    ///       "dataset": [
    ///         {
    ///           "codice_fidelity": "...",
    ///           "nome": "...",
    ///           "cognome": "...",
    ///           ...
    ///         }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// </summary>
    private SedeUserInfo? ParseUserFromResponse(JsonDocument json, string? email = null, string? cdFidelity = null)
    {
        try
        {
            var jsonRoot = json.RootElement;

            if (!jsonRoot.TryGetProperty("response", out var responseArray))
            {
                _logger.LogWarning("SedeApiService: Proprietà 'response' non trovata nella risposta");
                return null;
            }

            var responseElements = responseArray.EnumerateArray().ToList();
            if (responseElements.Count == 0)
            {
                _logger.LogInformation("SedeApiService: Nessun elemento in 'response'");
                return null;
            }

            var responseElement = responseElements.First();

            if (!responseElement.TryGetProperty("dataset", out var datasetArray))
            {
                _logger.LogWarning("SedeApiService: Proprietà 'dataset' non trovata nella risposta");
                return null;
            }

            var datasetElements = datasetArray.EnumerateArray().ToList();
            if (datasetElements.Count == 0)
            {
                _logger.LogInformation("SedeApiService: Utente non trovato nella sede (dataset vuoto)");
                return null;
            }

            var datasetElement = datasetElements.First();

            var userInfo = new SedeUserInfo
            {
                Found = true,
                CdFidelity = GetStringProperty(datasetElement, "codice_fidelity"),
                Nome = GetStringProperty(datasetElement, "nome"),
                Cognome = GetStringProperty(datasetElement, "cognome"),
                Email = GetStringProperty(datasetElement, "email") ?? email,
                Cellulare = GetStringProperty(datasetElement, "cellulare"),
                Indirizzo = GetStringProperty(datasetElement, "indirizzo"),
                Localita = GetStringProperty(datasetElement, "localita"),
                Cap = GetStringProperty(datasetElement, "cap"),
                Provincia = GetStringProperty(datasetElement, "provincia"),
                Nazione = GetStringProperty(datasetElement, "nazione"),
                Sesso = GetStringProperty(datasetElement, "sesso"),
                Store = GetStringProperty(datasetElement, "store") ?? GetStringProperty(datasetElement, "cd_ne")
            };

            // Parse data nascita se presente
            var dataNascitaStr = GetStringProperty(datasetElement, "data_nascita");
            if (!string.IsNullOrEmpty(dataNascitaStr))
            {
                if (DateTime.TryParse(dataNascitaStr, out var dataNascita))
                {
                    userInfo.DataNascita = dataNascita;
                }
            }

            _logger.LogInformation("SedeApiService: Utente trovato - CdFidelity={CdFidelity}, Nome={Nome} {Cognome}", 
                userInfo.CdFidelity, userInfo.Nome, userInfo.Cognome);

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante il parsing della risposta JSON");
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return prop.ValueKind == JsonValueKind.Null ? null : prop.GetString();
        }
        return null;
    }
}
