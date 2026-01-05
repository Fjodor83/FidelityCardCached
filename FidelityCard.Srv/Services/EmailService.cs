using System;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using FidelityCard.Lib.Services;

namespace FidelityCard.Srv.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;

    // Costanti per design consistente
    private const string PrimaryGradient = "linear-gradient(135deg, #105a12ff 0%, #053e30ff 100%)";
    private const string PrimaryColor = "#105a12ff";
    private const string SecondaryColor = "#053e30ff";
    private const string TextColor = "#666";
    private const string DarkColor = "#333";

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task<bool> InviaEmailVerificaAsync(string email, string nome, string token, string linkRegistrazione, string puntoVenditaNome)
    {
        var subject = "🎁 Completa la tua registrazione Fidelity Card";
        var htmlBody = GeneraTemplateVerifica(nome, linkRegistrazione);

        return await InviaEmailAsync(email, nome, subject, htmlBody);
    }

    public async Task<bool> InviaEmailBenvenutoAsync(string email, string nome, string codiceFidelity, byte[]? cardDigitale = null)
    {
        var subject = $"🎉 Benvenuto {nome}! La tua Fidelity Card è pronta";
        var htmlBody = GeneraTemplateBenvenuto(nome, codiceFidelity);

        return await InviaEmailAsync(email, nome, subject, htmlBody, cardDigitale, codiceFidelity);
    }

    public async Task<bool> InviaEmailAccessoProfiloAsync(string email, string nome, string linkAccesso)
    {
        var subject = "🔑 Accedi alla tua area personale Fidelity Card";
        var htmlBody = GeneraTemplateAccesso(nome, linkAccesso);

        return await InviaEmailAsync(email, nome, subject, htmlBody);
    }

    // Metodo comune per l'invio email
    private async Task<bool> InviaEmailAsync(string email, string nome, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName ?? "Fidelity Card", _emailSettings.Sender));
            message.To.Add(new MailboxAddress(nome, email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            // Gestione allegato
            if (attachment != null && attachment.Length > 0 && !string.IsNullOrEmpty(attachmentName))
            {
                bodyBuilder.Attachments.Add($"SunsFidelityCard_{attachmentName}.png", attachment, ContentType.Parse("image/png"));
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService] Errore invio email a {email}: {ex.Message}");
            return false;
        }
    }

    // Template base HTML
    private static string GeneraTemplateBase(string contenuto)
    {
        return $@"
<!DOCTYPE html>
<html lang='it'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5; 
            margin: 0; 
            padding: 20px 0;
            line-height: 1.6;
        }}
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            background-color: #ffffff; 
            border-radius: 16px; 
            overflow: hidden; 
            box-shadow: 0 4px 24px rgba(0,0,0,0.08);
        }}
        .header {{ 
            background: {PrimaryGradient}; 
            color: white; 
            padding: 48px 32px; 
            text-align: center; 
        }}
        .header h1 {{ 
            margin: 0 0 8px 0; 
            font-size: 32px; 
            font-weight: 700; 
            letter-spacing: -0.5px;
        }}
        .header p {{ 
            margin: 0; 
            opacity: 0.95; 
            font-size: 16px; 
        }}
        .content {{ 
            padding: 48px 32px; 
        }}
        .content h2 {{ 
            color: {DarkColor}; 
            margin: 0 0 16px 0; 
            font-size: 24px;
            font-weight: 600;
        }}
        .content p {{ 
            color: {TextColor}; 
            margin: 0 0 16px 0; 
            font-size: 16px; 
        }}
        .button-container {{ 
            text-align: center; 
            margin: 32px 0; 
        }}
        .button {{ 
            display: inline-block;
            background: {PrimaryGradient};
            color: white !important;
            padding: 16px 48px;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 16px;
            box-shadow: 0 4px 12px rgba(16, 90, 18, 0.3);
            transition: all 0.3s;
        }}
        .info-box {{ 
            background-color: #f8f9fa; 
            border-left: 4px solid {PrimaryColor}; 
            padding: 20px; 
            margin: 24px 0; 
            border-radius: 8px;
        }}
        .info-box p {{ 
            margin: 0 0 8px 0; 
        }}
        .info-box p:last-child {{ 
            margin: 0; 
        }}
        .code-box {{ 
            background: {PrimaryGradient};
            color: white; 
            padding: 32px; 
            text-align: center; 
            border-radius: 12px; 
            margin: 32px 0; 
        }}
        .code-box h3 {{ 
            margin: 0 0 12px 0; 
            font-size: 16px; 
            font-weight: 500;
            opacity: 0.95;
        }}
        .code {{ 
            font-size: 40px; 
            font-weight: 700; 
            letter-spacing: 4px; 
            font-family: 'Courier New', monospace;
        }}
        .benefits {{ 
            background-color: #f8f9fa; 
            padding: 24px; 
            border-radius: 12px; 
            margin: 24px 0; 
        }}
        .benefits h3 {{ 
            color: {DarkColor}; 
            margin: 0 0 16px 0; 
            font-size: 18px;
        }}
        .benefits ul {{ 
            margin: 0; 
            padding-left: 20px; 
        }}
        .benefits li {{ 
            margin: 8px 0; 
            color: {TextColor}; 
        }}
        .footer {{ 
            background-color: #f8f9fa; 
            padding: 32px; 
            text-align: center; 
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{ 
            color: #999; 
            font-size: 14px; 
            margin: 8px 0; 
        }}
        .link-text {{
            word-break: break-all;
            color: {PrimaryColor};
            font-size: 13px;
            line-height: 1.4;
        }}
        @media only screen and (max-width: 600px) {{
            .container {{ 
                border-radius: 0; 
                margin: 0;
            }}
            .header, .content {{ 
                padding: 32px 24px; 
            }}
            .header h1 {{ 
                font-size: 26px; 
            }}
            .code {{ 
                font-size: 32px; 
            }}
        }}
    </style>
</head>
<body>
    {contenuto}
</body>
</html>";
    }

    // Template email verifica
    private string GeneraTemplateVerifica(string nome, string linkRegistrazione)
    {
        var contenuto = $@"
<div class='container'>
    <div class='header'>
        <h1>☀️ Suns</h1>
        <p>Zero & Company</p>
    </div>
    <div class='content'>
        <h2>Ciao! 👋</h2>
        <p>Grazie per il tuo interesse nel programma fedeltà Suns!</p>
        <p>Per completare la tua registrazione e attivare la tua Fidelity Card digitale, clicca sul pulsante qui sotto:</p>
        
        <div class='button-container'>
            <a href='{linkRegistrazione}' class='button'>COMPLETA REGISTRAZIONE</a>
        </div>

        <div class='info-box'>
            <p><strong>⏰ Link valido per 15 minuti</strong></p>
            <p>Se non riesci a cliccare il pulsante, copia e incolla questo link nel tuo browser:</p>
            <p class='link-text'>{linkRegistrazione}</p>
        </div>

        <p><strong>Cosa riceverai:</strong></p>
        <ul style='color: {TextColor}; padding-left: 20px; margin: 16px 0;'>
            <li>Il tuo codice Fidelity personale</li>
            <li>Card digitale con QR code</li>
            <li>Accesso immediato ai vantaggi</li>
        </ul>
    </div>
    <div class='footer'>
        <p>© 2025 Suns - Zero & Company</p>
        <p>Email inviata perché ti sei registrato presso uno dei nostri punti vendita</p>
    </div>
</div>";

        return GeneraTemplateBase(contenuto);
    }

    // Template email benvenuto
    private string GeneraTemplateBenvenuto(string nome, string codiceFidelity)
    {
        var contenuto = $@"
<div class='container'>
    <div class='header'>
        <h1>☀️ Benvenuto in Suns!</h1>
        <p>La tua Fidelity Card è attiva</p>
    </div>
    <div class='content'>
        <h2>Ciao {nome}! 🎉</h2>
        <p>Congratulazioni! La tua registrazione è stata completata con successo.</p>
        
        <div class='code-box'>
            <h3>Il tuo Codice Fidelity</h3>
            <div class='code'>{codiceFidelity}</div>
        </div>

        <p><strong>📱 La tua card digitale è allegata a questa email.</strong></p>
        <p>Salvala sul tuo telefono e mostrala ad ogni acquisto per accumulare punti!</p>

        <div class='benefits'>
            <h3>✨ I tuoi vantaggi esclusivi:</h3>
            <ul>
                <li><strong>Punti ad ogni acquisto</strong> - Accumula e risparmia</li>
                <li><strong>Sconti dedicati</strong> - Offerte riservate ai membri</li>
                <li><strong>Promozioni VIP</strong> - Anteprime esclusive</li>
                <li><strong>Premi fedeltà</strong> - Raggiungi soglie e sblocca bonus</li>
            </ul>
        </div>

        <p style='text-align: center; margin-top: 32px; font-size: 18px; color: {DarkColor};'>
            <strong>Grazie per essere parte della famiglia Suns! 🌟</strong>
        </p>
    </div>
    <div class='footer'>
        <p>© 2025 Suns - Zero & Company</p>
        <p>Per assistenza: info@sunscompany.com</p>
    </div>
</div>";

        return GeneraTemplateBase(contenuto);
    }

    // Template email accesso
    private string GeneraTemplateAccesso(string nome, string linkAccesso)
    {
        var contenuto = $@"
<div class='container'>
    <div class='header'>
        <h1>☀️ Fidelity Card</h1>
        <p>Bentornato!</p>
    </div>
    <div class='content'>
        <h2>Ciao {nome}! 👋</h2>
        <p>Abbiamo ricevuto una richiesta di accesso alla tua area personale.</p>
        <p>Clicca sul pulsante qui sotto per visualizzare la tua Fidelity Card, i tuoi punti e il QR code:</p>
        
        <div class='button-container'>
            <a href='{linkAccesso}' class='button'>ACCEDI AL PROFILO</a>
        </div>

        <div class='info-box'>
            <p><strong>⏰ Link valido per 15 minuti</strong></p>
            <p>Se non hai richiesto tu l'accesso, ignora questa email. Il tuo account rimane sicuro.</p>
            <p style='margin-top: 12px;'>Link diretto:</p>
            <p class='link-text'>{linkAccesso}</p>
        </div>

        <p style='text-align: center; color: {TextColor}; margin-top: 32px;'>
            Non vediamo l'ora di rivederti! 😊
        </p>
    </div>
    <div class='footer'>
        <p>© 2025 Suns - Zero & Company</p>
        <p>Per domande o supporto, contattaci</p>
    </div>
</div>";

        return GeneraTemplateBase(contenuto);
    }
}