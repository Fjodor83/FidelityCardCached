using FidelityCard.Lib.Models;

namespace FidelityCard.Srv.Services
{
    public interface IFidelityService
    {
        Task<Fidelity> RegisterAsync(Fidelity fidelity);
    }
}
