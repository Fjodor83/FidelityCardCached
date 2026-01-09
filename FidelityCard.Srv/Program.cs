using FidelityCard.Lib.Services;
using FidelityCard.Srv.Services;
using FidelityCard.Srv.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Supporto CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7065") // URL del client Blazor WebAssembly
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var settings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new();
builder.Services.AddSingleton(Options.Create(settings));


// Add services to the container.
builder.Services.AddDbContext<FidelityCardDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddScoped<ICardGeneratorService, CardGeneratorService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Servizio cache email - Singleton per mantenere la cache in memoria durante tutto il ciclo di vita dell'app
builder.Services.AddSingleton<IEmailCacheService, EmailCacheService>();

// Servizio API Sede - HttpClient per chiamare l'API della sede centrale
builder.Services.AddHttpClient<ISedeApiService, SedeApiService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Supporto CORS
app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
