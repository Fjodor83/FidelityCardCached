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
    private const string PrimaryGradient = "linear-gradient(135deg, #105a12 0%, #053e30 100%)";
    private const string PrimaryColor = "#105a12";
    private const string SecondaryColor = "#053e30";
    private const string AccentGreen = "#0d8540";
    private const string LightGreen = "#e8f5e9";
    private const string TextColor = "#4a5568";
    private const string DarkColor = "#1a202c";
    private const string BorderColor = "#e2e8f0";

    public EmailService(IOptions<EmailSettings> emailSettings)
    {
        _emailSettings = emailSettings.Value;
    }

    public async Task<bool> InviaEmailVerificaAsync(string email, string nome, string token, string linkRegistrazione, string puntoVenditaNome)
    {
        var subject = "Completa la tua registrazione - Suns Fidelity Card";
        var htmlBody = GeneraTemplateVerifica(nome, linkRegistrazione);

        return await InviaEmailAsync(email, nome, subject, htmlBody);
    }

    public async Task<bool> InviaEmailBenvenutoAsync(string email, string nome, string codiceFidelity, byte[]? cardDigitale = null)
    {
        var subject = $"Benvenuto in Suns, {nome} - La tua Fidelity Card è attiva";
        var htmlBody = GeneraTemplateBenvenuto(nome, codiceFidelity);

        return await InviaEmailAsync(email, nome, subject, htmlBody, cardDigitale, codiceFidelity);
    }

    public async Task<bool> InviaEmailAccessoProfiloAsync(string email, string nome, string linkAccesso)
    {
        var subject = "Accesso alla tua area personale - Suns Fidelity Card";
        var htmlBody = GeneraTemplateAccesso(nome, linkAccesso);

        return await InviaEmailAsync(email, nome, subject, htmlBody);
    }

    // Metodo comune per l'invio email
    private async Task<bool> InviaEmailAsync(string email, string nome, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName ?? "Suns Fidelity Card", _emailSettings.Sender));
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
    <meta http-equiv='X-UA-Compatible' content='IE=edge'>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f7fafc; 
            margin: 0; 
            padding: 0;
            line-height: 1.7;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }}
        .email-wrapper {{
            width: 100%;
            background-color: #f7fafc;
            padding: 40px 20px;
        }}
        .container {{ 
            max-width: 600px; 
            margin: 0 auto; 
            background-color: #ffffff; 
            border-radius: 12px; 
            overflow: hidden; 
            box-shadow: 0 10px 40px rgba(0,0,0,0.06);
        }}
        .header {{ 
            background-color: {PrimaryColor};
            background-image: {PrimaryGradient}; 
            color: white; 
            padding: 50px 40px; 
            text-align: center; 
            position: relative;
        }}
        .header::after {{
            content: '';
            position: absolute;
            bottom: 0;
            left: 0;
            right: 0;
            height: 4px;
            background: rgba(255,255,255,0.2);
        }}
        .logo {{ 
            font-size: 42px; 
            margin-bottom: 8px;
            line-height: 1;
        }}
        .header h1 {{ 
            margin: 0 0 6px 0; 
            font-size: 28px; 
            font-weight: 700; 
            letter-spacing: -0.5px;
            color: white;
        }}
        .header p {{ 
            margin: 0; 
            opacity: 0.92; 
            font-size: 15px; 
            color: white;
            font-weight: 400;
        }}
        .content {{ 
            padding: 50px 40px; 
        }}
        .greeting {{ 
            color: {DarkColor}; 
            margin: 0 0 20px 0; 
            font-size: 26px;
            font-weight: 700;
            letter-spacing: -0.3px;
        }}
        .text-block {{ 
            color: {TextColor}; 
            margin: 0 0 18px 0; 
            font-size: 16px;
            line-height: 1.7;
        }}
        .text-block:last-of-type {{
            margin-bottom: 0;
        }}
        .button-container {{ 
            text-align: center; 
            margin: 36px 0; 
        }}
        .button {{ 
            display: inline-block;
            background-color: {PrimaryColor};
            background-image: {PrimaryGradient};
            color: white !important;
            padding: 18px 50px;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            letter-spacing: 0.3px;
            box-shadow: 0 4px 14px rgba(16, 90, 18, 0.25);
            transition: all 0.3s;
            text-transform: uppercase;
        }}
        .info-card {{ 
            background-color: {LightGreen}; 
            border-left: 4px solid {PrimaryColor}; 
            padding: 24px; 
            margin: 30px 0; 
            border-radius: 8px;
        }}
        .info-card-title {{
            color: {DarkColor};
            font-weight: 700;
            font-size: 15px;
            margin: 0 0 12px 0;
            display: flex;
            align-items: center;
        }}
        .info-card p {{ 
            margin: 0 0 10px 0; 
            color: {TextColor};
            font-size: 14px;
            line-height: 1.6;
        }}
        .info-card p:last-child {{ 
            margin: 0; 
        }}
        .code-container {{ 
            background-color: {PrimaryColor};
            background-image: {PrimaryGradient};
            color: white; 
            padding: 40px; 
            text-align: center; 
            border-radius: 12px; 
            margin: 36px 0;
            box-shadow: 0 6px 20px rgba(16, 90, 18, 0.2);
        }}
        .code-label {{ 
            margin: 0 0 14px 0; 
            font-size: 14px; 
            font-weight: 600;
            opacity: 0.9;
            color: white;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        .code {{ 
            font-size: 44px; 
            font-weight: 800; 
            letter-spacing: 6px; 
            font-family: 'Courier New', monospace;
            color: white;
            text-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .highlight-box {{ 
            background: linear-gradient(135deg, #fff8e1 0%, #fff3cd 100%);
            border: 2px solid #ffd54f;
            border-radius: 10px; 
            padding: 26px; 
            margin: 30px 0;
            box-shadow: 0 3px 10px rgba(255, 193, 7, 0.1);
        }}
        .highlight-title {{
            margin: 0 0 12px 0; 
            color: #f57c00; 
            font-size: 19px; 
            font-weight: 700;
            display: flex;
            align-items: center;
        }}
        .highlight-text {{
            margin: 0; 
            color: #e65100; 
            font-weight: 600; 
            font-size: 16px;
            line-height: 1.6;
        }}
        .benefits-card {{
            background-color: #ffffff; 
            border: 2px solid {BorderColor};
            padding: 30px; 
            border-radius: 12px; 
            margin: 30px 0;
        }}
        .benefits-title {{ 
            color: {DarkColor}; 
            margin: 0 0 20px 0; 
            font-size: 20px;
            font-weight: 700;
            letter-spacing: -0.3px;
        }}
        .benefits-list {{ 
            margin: 0; 
            padding: 0;
            list-style: none;
        }}
        .benefits-list li {{ 
            margin: 0 0 16px 0; 
            color: {TextColor};
            font-size: 15px;
            padding-left: 32px;
            position: relative;
            line-height: 1.6;
        }}
        .benefits-list li:last-child {{
            margin-bottom: 0;
        }}
        .benefits-list li::before {{
            content: '✓';
            position: absolute;
            left: 0;
            color: {PrimaryColor};
            font-weight: 700;
            font-size: 18px;
        }}
        .benefits-list strong {{
            color: {DarkColor};
            font-weight: 700;
        }}
        .feature-list {{
            margin: 20px 0;
            padding: 0;
            list-style: none;
        }}
        .feature-list li {{
            color: {TextColor};
            padding: 12px 0 12px 32px;
            margin: 0;
            position: relative;
            font-size: 15px;
            line-height: 1.6;
            border-bottom: 1px solid {BorderColor};
        }}
        .feature-list li:last-child {{
            border-bottom: none;
        }}
        .feature-list li::before {{
            content: '→';
            position: absolute;
            left: 0;
            color: {AccentGreen};
            font-weight: 700;
            font-size: 16px;
        }}
        .closing-message {{
            text-align: center; 
            margin-top: 40px; 
            padding-top: 30px;
            border-top: 2px solid {BorderColor};
        }}
        .closing-text {{
            font-size: 18px; 
            color: {DarkColor};
            font-weight: 600;
            margin: 0;
            line-height: 1.5;
        }}
        .footer {{ 
            background-color: #f7fafc; 
            padding: 40px; 
            text-align: center; 
            border-top: 1px solid {BorderColor};
        }}
        .footer-text {{ 
            color: #718096; 
            font-size: 13px; 
            margin: 0 0 8px 0;
            line-height: 1.6;
        }}
        .footer-text:last-child {{
            margin-bottom: 0;
        }}
        .footer-link {{
            color: {AccentGreen};
            text-decoration: none;
            font-weight: 600;
        }}
        .link-display {{
            word-break: break-all;
            color: {AccentGreen};
            font-size: 13px;
            line-height: 1.5;
            font-family: 'Courier New', monospace;
            background-color: #f7fafc;
            padding: 12px;
            border-radius: 6px;
            margin-top: 12px;
        }}
        .divider {{
            height: 1px;
            background-color: {BorderColor};
            margin: 30px 0;
        }}
        @media only screen and (max-width: 600px) {{
            .email-wrapper {{
                padding: 20px 10px;
            }}
            .container {{ 
                border-radius: 8px; 
            }}
            .header {{
                padding: 40px 30px;
            }}
            .content {{ 
                padding: 40px 30px; 
            }}
            .greeting {{ 
                font-size: 24px; 
            }}
            .code {{ 
                font-size: 36px;
                letter-spacing: 4px; 
            }}
            .button {{
                padding: 16px 40px;
                font-size: 14px;
            }}
            .benefits-card, .info-card {{
                padding: 24px;
            }}
        }}
    </style>
    <!--[if mso]>
    <style>
        .button {{ background-color: {PrimaryColor} !important; }}
        table {{ border-collapse: collapse; }}
    </style>
    <![endif]-->
</head>
<body>
    <div class='email-wrapper'>
        {contenuto}
    </div>
</body>
</html>";
    }

    // Template email verifica
    private string GeneraTemplateVerifica(string nome, string linkRegistrazione)
    {
        var contenuto = $@"
<div class='container'>
    <div class='header'>
        <div class='logo'>☀️</div>
        <h1>Suns</h1>
        <p>Zero & Company</p>
    </div>
    <div class='content'>
        <h2 class='greeting'>Ciao,</h2>
        <p class='text-block'>Grazie per il tuo interesse nel programma fedeltà Suns. Siamo felici di averti con noi.</p>
        <p class='text-block'>Per completare la tua registrazione e attivare la tua Fidelity Card digitale, clicca sul pulsante qui sotto.</p>
        
        <div class='button-container'>
            <a href='{linkRegistrazione}' class='button'>Completa Registrazione</a>
        </div>

        <div class='info-card'>
            <p class='info-card-title'>⏰ Importante</p>
            <p>Questo link è valido per 15 minuti per garantire la sicurezza del tuo account.</p>
            <p style='margin-top: 14px;'>Se non riesci a cliccare il pulsante, copia e incolla questo indirizzo nel tuo browser:</p>
            <div class='link-display'>{linkRegistrazione}</div>
        </div>

        <div class='divider'></div>

        <p class='text-block' style='font-weight: 600; color: {DarkColor}; margin-bottom: 14px;'>Cosa riceverai dopo la registrazione:</p>
        <ul class='feature-list'>
            <li>Il tuo codice Fidelity personale e univoco</li>
            <li>Card digitale con QR code per accumulo punti</li>
            <li>Accesso immediato a vantaggi esclusivi</li>
        </ul>
    </div>
    <div class='footer'>
        <p class='footer-text'>© 2025 Suns – Zero & Company. Tutti i diritti riservati.</p>
        <p class='footer-text'>Hai ricevuto questa email perché ti sei registrato presso uno dei nostri punti vendita.</p>
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
        <div class='logo'>☀️</div>
        <h1>Benvenuto in Suns</h1>
        <p>La tua Fidelity Card è attiva</p>
    </div>
    <div class='content'>
        <h2 class='greeting'>Ciao {nome},</h2>
        <p class='text-block'>Congratulazioni! La tua registrazione è stata completata con successo e sei ora parte della famiglia Suns.</p>
        
        <div class='code-container'>
            <h3 class='code-label'>Il tuo Codice Fidelity</h3>
            <div class='code'>{codiceFidelity}</div>
        </div>

        <p class='text-block' style='font-weight: 600; color: {DarkColor};'>📱 La tua card digitale è allegata a questa email</p>
        <p class='text-block'>Ti consigliamo di salvarla sul tuo smartphone e mostrarla ad ogni acquisto per accumulare punti e ottenere vantaggi esclusivi.</p>

        <div class='highlight-box'>
             <p class='highlight-title'>🎁 Offerta di Benvenuto</p>
             <p class='highlight-text'>
              Hai ottenuto uno sconto del 10% sul tuo primo acquisto nel nostro E‑Commerce. Non perdere questa opportunità!
            </p>
        </div>  

        <div class='benefits-card'>
            <h3 class='benefits-title'>I tuoi vantaggi esclusivi</h3>
            <ul class='benefits-list'>
                <li><strong>Punti ad ogni acquisto</strong> – Accumula punti e trasformali in sconti e premi</li>
                <li><strong>Sconti dedicati</strong> – Offerte speciali riservate esclusivamente ai nostri membri</li>
                <li><strong>Promozioni VIP</strong> – Accesso anticipato alle anteprime e agli eventi esclusivi</li>
                <li><strong>Premi fedeltà</strong> – Raggiungi nuove soglie e sblocca bonus straordinari</li>
            </ul>
        </div>

        <div class='closing-message'>
            <p class='closing-text'>Grazie per essere parte della famiglia Suns</p>
        </div>
    </div>
    <div class='footer'>
        <p class='footer-text'>© 2025 Suns – Zero & Company. Tutti i diritti riservati.</p>
        <p class='footer-text'>Per assistenza: <a href='mailto:info@sunscompany.com' class='footer-link'>info@sunscompany.com</a></p>
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
        <div class='logo'>☀️</div>
        <h1>Fidelity Card</h1>
        <p>Accesso alla tua area personale</p>
    </div>
    <div class='content'>
        <h2 class='greeting'>Ciao {nome},</h2>
        <p class='text-block'>Abbiamo ricevuto una richiesta di accesso alla tua area personale Suns Fidelity Card.</p>
        <p class='text-block'>Clicca sul pulsante qui sotto per visualizzare la tua card digitale, i tuoi punti accumulati e il QR code personale.</p>
        
        <div class='button-container'>
            <a href='{linkAccesso}' class='button'>Accedi al Profilo</a>
        </div>

        <div class='info-card'>
            <p class='info-card-title'>🔒 Sicurezza</p>
            <p>Questo link è valido per 15 minuti per garantire la protezione del tuo account.</p>
            <p style='margin-top: 14px; font-weight: 600;'>Non hai richiesto l'accesso?</p>
            <p>Puoi tranquillamente ignorare questa email. Il tuo account rimane sicuro e protetto.</p>
            <div class='link-display'>{linkAccesso}</div>
        </div>

        <div class='closing-message'>
            <p class='closing-text'>Non vediamo l'ora di rivederti</p>
        </div>
    </div>
    <div class='footer'>
        <p class='footer-text'>© 2025 Suns – Zero & Company. Tutti i diritti riservati.</p>
        <p class='footer-text'>Per domande o supporto, non esitare a contattarci.</p>
    </div>
</div>";

        return GeneraTemplateBase(contenuto);
    }
}