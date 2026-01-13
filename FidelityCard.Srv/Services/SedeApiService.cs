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

            // Leggi prima come stringa per fare debug
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
                    CalledFrom = "APP FIDELITY",
                    CalledOperator = ""
                },
                Parameters = new[]
                {
                    new ParamElement { Name = "CdNE", Value = fidelity.Store },
                    new ParamElement { Name = "Tipo", Value = "D" }, // D = Digitale
                    new ParamElement { Name = "Nome", Value = fidelity.Nome },
                    new ParamElement { Name = "Cognome", Value = fidelity.Cognome },
                    new ParamElement { Name = "Sesso", Value = fidelity.Sesso },
                    new ParamElement { Name = "DataNascita", Value = fidelity.DataNascita?.ToString("ddMMyyyy") },
                    new ParamElement { Name = "Indirizzo", Value = fidelity.Indirizzo },
                    new ParamElement { Name = "Localita", Value = fidelity.Localita },
                    new ParamElement { Name = "CdCap", Value = fidelity.Cap },
                    new ParamElement { Name = "CdProv", Value = fidelity.Provincia },
                    new ParamElement { Name = "CdNazioni", Value = fidelity.Nazione },
                    new ParamElement { Name = "Cellulare", Value = fidelity.Cellulare },
                    new ParamElement { Name = "Email", Value = fidelity.Email }
                }
            };

            _logger.LogInformation("SedeApiService: Registrazione utente {Email} store {Store}, DataNascita={DataNascita}",
                fidelity.Email, fidelity.Store, fidelity.DataNascita?.ToString("ddMMyyyy") ?? "NULL");

            var response = await _httpClient.PostAsJsonAsync(endpointSede, request);

            // Leggi sempre la risposta come stringa per debugging
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("SedeApiService: HTTP Status={Status}, ContentLength={Length}, Content={Content}",
                response.StatusCode, responseContent?.Length ?? 0,
                string.IsNullOrWhiteSpace(responseContent) ? "[VUOTO]" : responseContent.Substring(0, Math.Min(200, responseContent.Length)));

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

            // CASO 1: Risposta è solo il codice (es: "FID20250112001")
            var trimmedContent = responseContent.Trim();
            if (trimmedContent.StartsWith("FID") && trimmedContent.Length >= 15 && trimmedContent.Length <= 25)
            {
                // Probabilmente è solo il codice senza wrapper JSON
                _logger.LogInformation("SedeApiService: Risposta sembra essere codice diretto: {Code}", trimmedContent);
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
                _logger.LogError(jsonEx, "SedeApiService: Risposta non è JSON valido. Content={Content}", responseContent);

                // Ultimo tentativo: cerca pattern "FID" nella stringa
                var fidMatch = System.Text.RegularExpressions.Regex.Match(responseContent, @"FID\d{12,16}");
                if (fidMatch.Success)
                {
                    _logger.LogInformation("SedeApiService: Estratto codice con regex: {Code}", fidMatch.Value);
                    return fidMatch.Value;
                }

                return null;
            }

            // Parse response per trovare 'codice_fidelity'
            // Parse response per trovare 'codice_fidelity'
            var jsonRoot = json.RootElement;

            // CASO 1: Struttura complessa {"response": [{"dataset": [...]}]}
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
                                _logger.LogInformation("SedeApiService: Codice fidelity trovato in JSON (response/dataset): {Code}", cdFidelity);
                                return cdFidelity;
                            }
                        }
                    }
                }
            }
            
            // CASO 2: Struttura standard SQL {"dataset": [...]}
            if (jsonRoot.TryGetProperty("dataset", out var rootDatasetArray))
            {
                foreach (var dataElement in rootDatasetArray.EnumerateArray())
                {
                    var cdFidelity = GetStringProperty(dataElement, "codice_fidelity");
                    if (!string.IsNullOrEmpty(cdFidelity))
                    {
                        _logger.LogInformation("SedeApiService: Codice fidelity trovato in JSON (root dataset): {Code}", cdFidelity);
                        return cdFidelity;
                    }
                }
            }

            // CASO 3: Proprietà diretta {"codice_fidelity": "..."}
            if (jsonRoot.TryGetProperty("codice_fidelity", out var codiceProp))
            {
                var codice = codiceProp.GetString();
                if (!string.IsNullOrEmpty(codice))
                {
                    _logger.LogInformation("SedeApiService: Codice fidelity trovato diretto: {Code}", codice);
                    return codice;
                }
            }

            _logger.LogWarning("SedeApiService: codice_fidelity non trovato nella risposta. Response={Response}", responseContent);
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
                    new ParamElement { Name = "Codice_Fidelity", Value = cdFidelity }
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