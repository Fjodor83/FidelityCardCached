using FidelityCard.Lib.Models;

namespace FidelityCard.Services
{
    public interface IFidelityClientService
    {
        Task RegisterAsync(Fidelity fidelity);
        Task<Fidelity> GetRegistrationDataAsync(string token);
    }
}
