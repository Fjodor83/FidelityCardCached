using FidelityCard.Lib.Models;
using FidelityCard.Lib.Services;
using FidelityCard.Srv.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace FidelityCard.Srv.Controllers;

[ApiController]
[Route("api/[controller]")]
//  [Route("api/Fidelity")]
public class FidelityCardController(FidelityCardDbContext context, 
        ILogger<EmailSender> logger,
        IOptions<EmailSettings> emailSettings,
        IConfiguration config,
        IWebHostEnvironment env) : ControllerBase
{
    private readonly FidelityCardDbContext _context = context;

    private readonly ILogger<EmailSender> _logger = logger;
    private readonly IOptions<EmailSettings> _emailSettings = emailSettings;
    private readonly IConfiguration _config = config;
    private readonly IWebHostEnvironment _env = env;

    // GET: api/FidelityCard
    [HttpGet]
    public async Task<Fidelity> Get(string email)
    {
        return await _context.Fidelity.FirstOrDefaultAsync(f => f.Email == email) ?? new Fidelity();
    }

    // GET: api/FidelityCard/EmailValidation
    [HttpGet("[action]")]
    public async Task EmailValidation(string email, string store)
    {
        var token = TokenManager.Generate();
        System.IO.File.WriteAllText(Path.Combine(_env.ContentRootPath, "Token", token), store);

        var emailSender = new EmailSender(_logger,_emailSettings,_config);
        await emailSender.SendEmailAsync(email, "SUNS Fidelity card - Registrazione utente", $"{token}\r\n{store}");
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
