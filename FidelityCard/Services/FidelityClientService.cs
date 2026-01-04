using FidelityCard.Lib.Models;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using System.Text.Json;

namespace FidelityCard.Services
{
    public class FidelityClientService(HttpClient httpClient, IJSRuntime jsRuntime) : IFidelityClientService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly IJSRuntime _jsRuntime = jsRuntime;
        private CustomSettings? _customSettings;

        private async Task<CustomSettings> GetSettingsAsync()
        {
            if (_customSettings != null) return _customSettings;

            var manifestData = await _jsRuntime.InvokeAsync<Dictionary<string, JsonElement>>("manifestInterop.getManifest");
            _customSettings = JsonSerializer.Deserialize<CustomSettings>(manifestData["custom_settings"]);
            
            if (_customSettings == null) throw new Exception("Custom settings not found in manifest");
            return _customSettings;
        }

        public async Task RegisterAsync(Fidelity fidelity)
        {
            var settings = await GetSettingsAsync();
            var baseUrl = settings.Endpoint;
            
            var url = $"{baseUrl.TrimEnd('/')}/api/FidelityCard";
            var response = await _httpClient.PostAsJsonAsync(url, fidelity);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Errore durante la registrazione: {error}");
            }
        }

        public async Task<Fidelity> GetRegistrationDataAsync(string token)
        {
            var settings = await GetSettingsAsync();
            var endpoint = settings.Endpoint;

             var actionurl = $"{endpoint.TrimEnd('/')}/api/FidelityCard/EmailConfirmation?token={token}";
             var fileContent = await _httpClient.GetStringAsync(actionurl);

             if (string.IsNullOrEmpty(fileContent))
             {
                 throw new Exception("Token non valido o scaduto");
             }

             string[] param = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
             if (param.Length < 2)
             {
                 throw new Exception("Dati token non validi");
             }

             return new Fidelity
             {
                 Store = param[0],
                 Email = param[1]
             };
        }
    }
}
