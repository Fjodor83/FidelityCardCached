using FidelityCard.Lib.Models;
using FidelityCard.Srv.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelityCard.Srv.Controllers;

[ApiController]
[Route("api/[controller]")]
//  [Route("api/Fidelity")]
public class FidelityCardController(FidelityCardDbContext context) : ControllerBase
{
    private readonly FidelityCardDbContext _context = context;

    // GET: api/FidelityCard
    [HttpGet]
    public async Task<Fidelity> Get(string email)
    {
        return await _context.Fidelity.FirstOrDefaultAsync(f => f.Email == email) ?? new Fidelity();
    }


    // POST: api/FidelityCard
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Fidelity fidelity)
    {

        if (fidelity == null)
        {
            Console.WriteLine("Modello nullo");
            return BadRequest();
        }

        _context.Fidelity.Add(fidelity);
        try
        {
            await _context.SaveChangesAsync();
            return Ok(fidelity);
        }
        catch (Exception ex)
        {
            if (ex.InnerException != null)
            {
                return StatusCode(500, ex.InnerException.Message);
            }
            else 
            {
                return StatusCode(500, ex.Message);
            }

        }
    }
}
