using System.Text;
using System.Text.Json;
using FidelityCard.Lib.Models;

namespace FidelityCard.Srv.Services;

/// <summary>
/// Implementazione del servizio per comunicare con l'API della sede centrale (NEFidelity)
/// Con gestione migliorata degli errori e logging delle risposte
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
                    new ParamElement { Name = "Email", Value = email.Trim().ToLowerInvariant() }
                }
            };

            _logger.LogInformation("SedeApiService: Chiamata API sede per email={Email}", email);

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede. StatusCode={StatusCode}, Content={Content}",
                    response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("SedeApiService: Risposta vuota dalla sede per email={Email}", email);
                return null;
            }

            _logger.LogDebug("SedeApiService: Risposta ricevuta: {Response}", responseContent);

            var json = JsonDocument.Parse(responseContent);
            return ParseUserFromResponse(json, email);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "SedeApiService: Errore parsing JSON per email={Email}", email);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante la chiamata API sede per email={Email}", email);
            return null;
        }
    }

    /// <summary>
    /// Registra o aggiorna un utente nella tabella NEFidelity della sede
    /// IMPORTANTE: La stored procedure xTSP_API_Put_Fidelity si aspetta un array di parametri
    /// con struttura: {name, type, value, sequence}
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

            // Crea l'array di parametri nel formato richiesto dalla stored procedure
            var parameters = new List<ParamElement>
            {
                new ParamElement { Name = "store", Type = "string", Value = fidelity.Store ?? "NE001", Sequence = "1" },
                new ParamElement { Name = "tipo", Type = "string", Value = "D", Sequence = "2" },
                new ParamElement { Name = "nome", Type = "string", Value = fidelity.Nome ?? "", Sequence = "3" },
                new ParamElement { Name = "cognome", Type = "string", Value = fidelity.Cognome ?? "", Sequence = "4" },
                new ParamElement { Name = "sesso", Type = "string", Value = fidelity.Sesso ?? "", Sequence = "5" },
                new ParamElement { Name = "data_nascita", Type = "string", Value = fidelity.DataNascita?.ToString("yyyyMMdd") ?? "", Sequence = "6" },
                new ParamElement { Name = "codice_fiscale", Type = "string", Value = "", Sequence = "7" },
                new ParamElement { Name = "indirizzo", Type = "string", Value = fidelity.Indirizzo ?? "", Sequence = "8" },
                new ParamElement { Name = "localita", Type = "string", Value = fidelity.Localita ?? "", Sequence = "9" },
                new ParamElement { Name = "cap", Type = "string", Value = fidelity.Cap ?? "", Sequence = "10" },
                new ParamElement { Name = "provincia", Type = "string", Value = fidelity.Provincia ?? "", Sequence = "11" },
                new ParamElement { Name = "nazione", Type = "string", Value = fidelity.Nazione ?? "", Sequence = "12" },
                new ParamElement { Name = "cellulare", Type = "string", Value = fidelity.Cellulare ?? "", Sequence = "13" },
                new ParamElement { Name = "email", Type = "string", Value = fidelity.Email ?? "", Sequence = "14" },
                new ParamElement { Name = "sconto", Type = "string", Value = "10", Sequence = "15" }
            };

            // Crea la request con i parametri nel formato corretto
            var request = new RequestSede
            {
                Request = new Request
                {
                    DbName = dbNameSede,
                    SpName = "xTSP_API_Put_Fidelity",
                    CalledFrom = "APP FIDELITY",
                    CalledOperator = ""
                },
                Parameters = parameters.ToArray()
            };

            _logger.LogInformation("SedeApiService: Registrazione utente {Email} store {Store}, DataNascita={DataNascita}",
                fidelity.Email, fidelity.Store, fidelity.DataNascita?.ToString("yyyyMMdd") ?? "NULL");

            // Log della request completa per debug
            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogInformation("SedeApiService: Request JSON formato RequestSede:\n{Request}", requestJson);

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            // Leggi sempre la risposta come stringa per debugging
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SedeApiService: HTTP Status={Status}, ContentLength={Length}",
                response.StatusCode, responseContent?.Length ?? 0);

            _logger.LogInformation("SedeApiService: Risposta completa: {Content}",
                string.IsNullOrWhiteSpace(responseContent) ? "[VUOTO]" : responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede (Register). StatusCode={StatusCode}, Content={Content}",
                    response.StatusCode, responseContent);
                return null;
            }

            // Controlla se la risposta è vuota
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("SedeApiService: Risposta vuota dalla sede per registrazione {Email}", fidelity.Email);
                return null;
            }

            // Tentativo di parsing del codice fidelity
            var cdFidelity = ExtractCdFidelityFromResponse(responseContent);

            if (!string.IsNullOrEmpty(cdFidelity))
            {
                _logger.LogInformation("SedeApiService: Codice fidelity estratto con successo: {Code}", cdFidelity);
                return cdFidelity;
            }

            _logger.LogWarning("SedeApiService: Impossibile estrarre codice_fidelity dalla risposta");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante RegisterUserAsync per {Email}", fidelity.Email);
            return null;
        }
    }

    /// <summary>
    /// Estrae il codice fidelity dalla risposta, provando diversi metodi
    /// </summary>
    private string? ExtractCdFidelityFromResponse(string responseContent)
    {
        try
        {
            var trimmedContent = responseContent.Trim();

            // CASO 1: Risposta è solo il codice (es: "FID20250113001")
            if (trimmedContent.StartsWith("FID") && trimmedContent.Length >= 12 && trimmedContent.Length <= 25 && !trimmedContent.Contains("{"))
            {
                _logger.LogInformation("SedeApiService: Risposta è codice diretto: {Code}", trimmedContent);
                return trimmedContent;
            }

            // CASO 2: Prova a parsare come JSON
            JsonDocument? json;
            try
            {
                json = JsonDocument.Parse(responseContent);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "SedeApiService: Risposta non è JSON valido");

                // CASO 3: Cerca pattern "FID" nella stringa con regex
                var fidMatch = System.Text.RegularExpressions.Regex.Match(responseContent, @"FID\d{11}");
                if (fidMatch.Success)
                {
                    _logger.LogInformation("SedeApiService: Estratto codice con regex: {Code}", fidMatch.Value);
                    return fidMatch.Value;
                }

                return null;
            }

            var jsonRoot = json.RootElement;

            // CASO 4: Verifica se c'è un campo "status" con valore "ERROR"
            if (jsonRoot.TryGetProperty("status", out var statusProp))
            {
                var status = statusProp.GetString();
                if (status == "ERROR")
                {
                    var errorMsg = GetStringProperty(jsonRoot, "codice_fidelity") ?? "Errore sconosciuto";
                    _logger.LogError("SedeApiService: Errore dalla sede: {Error}", errorMsg);
                    return null;
                }
            }

            // CASO 5: Struttura complessa {"response": [{"dataset": [...]}]}
            if (jsonRoot.TryGetProperty("response", out var responseArray))
            {
                foreach (var respElement in responseArray.EnumerateArray())
                {
                    if (respElement.TryGetProperty("dataset", out var datasetArray))
                    {
                        foreach (var dataElement in datasetArray.EnumerateArray())
                        {
                            var cdFidelity = GetStringProperty(dataElement, "codice_fidelity");
                            if (!string.IsNullOrEmpty(cdFidelity) && !cdFidelity.StartsWith("ERRORE"))
                            {
                                _logger.LogInformation("SedeApiService: Codice trovato in JSON (response/dataset): {Code}", cdFidelity);
                                return cdFidelity;
                            }
                        }
                    }
                }
            }

            // CASO 6: Struttura standard {"dataset": [...]}
            if (jsonRoot.TryGetProperty("dataset", out var rootDatasetArray))
            {
                foreach (var dataElement in rootDatasetArray.EnumerateArray())
                {
                    var cdFidelity = GetStringProperty(dataElement, "codice_fidelity");
                    if (!string.IsNullOrEmpty(cdFidelity) && !cdFidelity.StartsWith("ERRORE"))
                    {
                        _logger.LogInformation("SedeApiService: Codice trovato in JSON (root dataset): {Code}", cdFidelity);
                        return cdFidelity;
                    }
                }
            }

            // CASO 7: Proprietà diretta {"codice_fidelity": "..."}
            if (jsonRoot.TryGetProperty("codice_fidelity", out var codiceProp))
            {
                var codice = codiceProp.GetString();
                if (!string.IsNullOrEmpty(codice) && !codice.StartsWith("ERRORE"))
                {
                    _logger.LogInformation("SedeApiService: Codice trovato diretto: {Code}", codice);
                    return codice;
                }
            }

            _logger.LogWarning("SedeApiService: codice_fidelity non trovato in nessuna struttura JSON");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante estrazione codice fidelity");
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
                    new ParamElement { Name = "@Codice_Fidelity", Value = cdFidelity }
                }
            };

            _logger.LogInformation("SedeApiService: Chiamata API sede per CdFidelity={CdFidelity}", cdFidelity);

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede. StatusCode={StatusCode}, Content={Content}",
                    response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("SedeApiService: Risposta vuota dalla sede per CdFidelity={CdFidelity}", cdFidelity);
                return null;
            }

            _logger.LogDebug("SedeApiService: Risposta ricevuta: {Response}", responseContent);

            var json = JsonDocument.Parse(responseContent);
            return ParseUserFromResponse(json, null, cdFidelity);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "SedeApiService: Errore parsing JSON per CdFidelity={CdFidelity}", cdFidelity);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante la chiamata API sede per CdFidelity={CdFidelity}", cdFidelity);
            return null;
        }
    }

    /// <summary>
    /// Recupera tutti gli utenti attivi dalla tabella NEFidelity della sede
    /// Utilizzato per la sincronizzazione della cache all'avvio
    /// </summary>
    public async Task<List<SedeUserInfo>> GetAllUsersAsync()
    {
        var users = new List<SedeUserInfo>();
        
        try
        {
            var endpointSede = _config.GetValue<string>("SedeSettings:EndpointSede");
            var dbNameSede = _config.GetValue<string>("SedeSettings:DbNameSede");

            if (string.IsNullOrEmpty(endpointSede) || string.IsNullOrEmpty(dbNameSede))
            {
                _logger.LogWarning("SedeApiService: EndpointSede o DbNameSede non configurati");
                return users;
            }

            var request = new RequestSede
            {
                Request = new Request
                {
                    DbName = dbNameSede,
                    SpName = "xTSP_API_Get_All_Fidelity",
                    CalledFrom = "APP FIDELITY - CACHE SYNC",
                    CalledOperator = ""
                },
                Parameters = Array.Empty<ParamElement>()
            };

            _logger.LogInformation("SedeApiService: Chiamata API sede per recupero di tutti gli utenti attivi");

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("SedeApiService: Risposta non OK dalla sede (GetAllUsers). StatusCode={StatusCode}, Content={Content}",
                    response.StatusCode, errorContent);
                return users;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                _logger.LogWarning("SedeApiService: Risposta vuota dalla sede per GetAllUsers");
                return users;
            }

            _logger.LogDebug("SedeApiService: Risposta ricevuta per GetAllUsers");

            var json = JsonDocument.Parse(responseContent);
            var jsonRoot = json.RootElement;
            JsonElement datasetArray = default;
            bool datasetFound = false;

            // Cerca structure {"response": [{"dataset": ...}]}
            if (jsonRoot.TryGetProperty("response", out var responseArray))
            {
                var responseElements = responseArray.EnumerateArray().ToList();
                if (responseElements.Count > 0)
                {
                    var responseElement = responseElements.First();
                    if (responseElement.TryGetProperty("dataset", out datasetArray))
                    {
                        datasetFound = true;
                    }
                }
            }

            // Cerca direttamente {"dataset": ...}
            if (!datasetFound)
            {
                if (jsonRoot.TryGetProperty("dataset", out datasetArray))
                {
                    datasetFound = true;
                }
            }

            if (!datasetFound)
            {
                _logger.LogWarning("SedeApiService: Nessuna proprietà 'dataset' trovata nella risposta GetAllUsers");
                return users;
            }

            foreach (var dataElement in datasetArray.EnumerateArray())
            {
                var userInfo = new SedeUserInfo
                {
                    Found = true,
                    CdFidelity = GetStringProperty(dataElement, "codice_fidelity"),
                    Nome = GetStringProperty(dataElement, "nome"),
                    Cognome = GetStringProperty(dataElement, "cognome"),
                    Email = GetStringProperty(dataElement, "email"),
                    Cellulare = GetStringProperty(dataElement, "cellulare"),
                    Indirizzo = GetStringProperty(dataElement, "indirizzo"),
                    Localita = GetStringProperty(dataElement, "localita"),
                    Cap = GetStringProperty(dataElement, "cap"),
                    Provincia = GetStringProperty(dataElement, "provincia"),
                    Nazione = GetStringProperty(dataElement, "nazione"),
                    Sesso = GetStringProperty(dataElement, "sesso"),
                    Store = GetStringProperty(dataElement, "store") ?? GetStringProperty(dataElement, "cd_ne")
                };

                // Parse data nascita se presente
                var dataNascitaStr = GetStringProperty(dataElement, "data_nascita");
                if (!string.IsNullOrEmpty(dataNascitaStr))
                {
                    if (DateTime.TryParse(dataNascitaStr, out var dataNascita))
                    {
                        userInfo.DataNascita = dataNascita;
                    }
                }

                users.Add(userInfo);
            }

            _logger.LogInformation("SedeApiService: Recuperati {Count} utenti dalla sede", users.Count);
            return users;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "SedeApiService: Errore parsing JSON per GetAllUsers");
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SedeApiService: Errore durante GetAllUsers");
            return users;
        }
    }

    /// <summary>
    /// Parsa la risposta JSON della sede e estrae le informazioni utente
    /// </summary>
    private SedeUserInfo? ParseUserFromResponse(JsonDocument json, string? email = null, string? cdFidelity = null)
    {
        try
        {
            var jsonRoot = json.RootElement;
            JsonElement datasetArray = default;
            bool datasetFound = false;

            // Tentativo 1: Cerca structure {"response": [{"dataset": ...}]}
            if (jsonRoot.TryGetProperty("response", out var responseArray))
            {
                var responseElements = responseArray.EnumerateArray().ToList();
                if (responseElements.Count > 0)
                {
                    var responseElement = responseElements.First();
                    if (responseElement.TryGetProperty("dataset", out datasetArray))
                    {
                        datasetFound = true;
                    }
                }
            }

            // Tentativo 2: Cerca direttamente {"dataset": ...}
            if (!datasetFound)
            {
                if (jsonRoot.TryGetProperty("dataset", out datasetArray))
                {
                    datasetFound = true;
                }
            }

            if (!datasetFound)
            {
                _logger.LogWarning("SedeApiService: Nessuna proprietà 'dataset' trovata nella risposta");
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