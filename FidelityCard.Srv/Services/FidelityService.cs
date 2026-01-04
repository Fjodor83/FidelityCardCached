using FidelityCard.Lib.Models;
using FidelityCard.Lib.Services;
using FidelityCard.Srv.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FidelityCard.Srv.Services
{
    public class FidelityService(FidelityCardDbContext context, 
        IConfiguration config, 
        IHttpClientFactory httpClientFactory,
        ICardGeneratorService cardGenerator,
        IEmailService emailService,
        ILogger<FidelityService> logger) : IFidelityService
    {
        private readonly FidelityCardDbContext _context = context;
        private readonly IConfiguration _config = config;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ICardGeneratorService _cardGenerator = cardGenerator;
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<FidelityService> _logger = logger;

        public async Task<Fidelity> RegisterAsync(Fidelity fidelity)
        {
            if (fidelity == null) throw new ArgumentNullException(nameof(fidelity));

            // 1. Call External API (Sede)
            var cdFidelity = await CreateFidelityOnSedeAsync(fidelity);
            if (string.IsNullOrEmpty(cdFidelity))
            {
                throw new Exception("Impossibile ottenere il codice fidelity dalla sede.");
            }

            fidelity.CdFidelity = cdFidelity;

            // 2. Save to Local DB
            var existing = await _context.Fidelity.FirstOrDefaultAsync(f => f.Email == fidelity.Email);
            if (existing != null)
            {
                // Update existing or throw? Logic in Controller was .Add, suggesting new.
                // If it exists, we might want to update it or return it.
                // Controller logic was .Add() which would throw if ID is set and exists, or add duplicate if not Key.
                // Email is likely unique.
                // If we are here, we are registering.
                // Let's assume we update if exists or add if not.
                 // However, the controller used .Add, let's stick to that but handle potential duplicate key if email is unique index.
                 // Actually, the original controller code just did .Add(fidelity).
                 _context.Fidelity.Add(fidelity);
            }
            else
            {
                _context.Fidelity.Add(fidelity);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                 _logger.LogError(ex, "Errore salvataggio DB locale");
                 throw;
            }

            // 3. Generate Card and Send Email
            try
            {
                var cardBytes = await _cardGenerator.GeneraCardDigitaleAsync(fidelity, fidelity.Store);
                await _emailService.InviaEmailBenvenutoAsync(fidelity.Email ?? "", fidelity.Nome ?? "Cliente", fidelity.CdFidelity ?? "", cardBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante generazione card o invio email benvenuto.");
                // We don't block success
            }

            return fidelity;
        }

        private async Task<string?> CreateFidelityOnSedeAsync(Fidelity fidelity)
        {
            var endpoint = _config["SedeSettings:Endpoint"];
            var dbName = _config["SedeSettings:DbName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(dbName))
            {
                throw new Exception("Configurazione Sede non valida (Endpoint o DbName mancanti).");
            }

            var request = new RequestSede
            {
                Request = new()
                {
                    DbName = dbName,
                    SpName = "xTSP_API_Put_Fidelity",
                    CalledFrom = "APP FIDELIT"
                },
                Parameters = [
                    new() { Name = "store", Value = fidelity.Store },
                    new() { Name = "tipo", Value = "D" },
                    new() { Name = "nome", Value = fidelity.Nome },
                    new() { Name = "cognome", Value = fidelity.Cognome },
                    new() { Name = "sesso", Value = fidelity.Sesso },
                    new() { Name = "data_nascita", Value = fidelity.DataNascita?.ToString("yyyyMMdd") },
                    // new() { Name = "codice_fiscale", Value = "" },
                    new() { Name = "indirizzo", Value = fidelity.Indirizzo },
                    new() { Name = "localita", Value = fidelity.Localita },
                    new() { Name = "cap", Value = fidelity.Cap },
                    new() { Name = "provincia", Value = fidelity.Provincia },
                    new() { Name = "nazione", Value = fidelity.Nazione },
                    new() { Name = "cellulare", Value = fidelity.Cellulare },
                    new() { Name = "email", Value = fidelity.Email }
                ]
            };

            var client = _httpClientFactory.CreateClient("SedeClient");
            var response = await client.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
                 if (json != null) 
                {
                    var responseArray = json.RootElement.GetProperty("response").EnumerateArray();
                    if (responseArray.Any())
                    {
                         var datasetArray = responseArray.First().GetProperty("dataset").EnumerateArray();
                         if (datasetArray.Any())
                         {
                             return datasetArray.First().GetProperty("codice_fidelity").GetString();
                         }
                    }
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Errore API Sede: {response.StatusCode} - {error}");
            }

            return null;
        }
    }
}
