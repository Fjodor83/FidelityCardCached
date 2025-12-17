using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace FidelityCard.Lib.Services;

public class EmailSettings
{
    public string? MailServer { get; set; } = string.Empty;
    public int MailPort { get; set; }
    public string? SenderName { get; set; } = string.Empty;
    public string? Sender { get; set; } = string.Empty;
    public string? Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
}

public sealed class EmailAttach
{
    public string? FileName { get; set; } = string.Empty;
    /// <summary>
    /// Per i valori è opportuno usare il tipo statico MediaTypeNames (es MediaTypeNames.Image.Png)
    /// </summary>
    public string? ContentType { get; set; } = string.Empty;
    public byte[]? Content { get; set; }
}

public sealed class EmailSender
{
    private readonly ILogger<EmailSender>? Logger;
    private readonly EmailSettings? EmailSettings;
    private readonly IConfiguration? Config;

    private ArrayList Ms { get; set; } = [];

 
    public EmailSender(ILogger<EmailSender> logger,
        IOptions<EmailSettings> emailSettings,
        IConfiguration config)
    {
        Logger = logger;
        EmailSettings = emailSettings.Value;
        Config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string message) =>
        await SendEmailAsync(new MailAddress(email), subject, message);

    /// <summary>
    /// Nel footer è possibile usare le macro {WebSiteUrl} e {WebSiteTitle}
    /// </summary>
    /// <param name="email"></param>
    /// <param name="subject"></param>
    /// <param name="message"></param>
    /// <param name="footer"></param>
    /// <param name="showFooter"></param>
    /// <param name="isBodyHtml"></param>
    /// <returns></returns>
    public async Task SendEmailAsync(MailAddress email, string subject, string message, string? footer = "", bool showFooter = true, bool isBodyHtml = true) =>
        await SendEmailAsync([email], subject, message, footer, showFooter, isBodyHtml);

    /// <summary>
    /// Nel footer è possibile usare le macro {WebSiteUrl} e {WebSiteTitle}
    /// </summary>
    /// <param name="emails"></param>
    /// <param name="subject"></param>
    /// <param name="message"></param>
    /// <param name="footer"></param>
    /// <param name="showFooter"></param>
    /// <param name="isBodyHtml"></param>
    /// <param name="cc"></param>
    /// <param name="bcc"></param>
    /// <param name="images"></param>
    /// <param name="attachs"></param>
    /// <param name="zipAllAttachments"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task SendEmailAsync(
        MailAddress[] emails,
        string? subject,
        string? message,
        string? footer = "",
        bool showFooter = true,
        bool isBodyHtml = true,
        MailAddress[]? cc = null,
        MailAddress[]? bcc = null,
        EmailAttach[]? images = null,
        EmailAttach[]? attachs = null,
        bool zipAllAttachments = false)
    {
        try
        {
            if (EmailSettings == null || string.IsNullOrEmpty(EmailSettings.Sender))
            {
                throw new(nameof(EmailSettings));
            }

            var smtpClient = new SmtpClient(EmailSettings.MailServer, EmailSettings.MailPort)
            {
                Credentials = new NetworkCredential(EmailSettings.Sender, EmailSettings.Password),
                EnableSsl = true,
            };

            message = $@"
                        <html style='background:#eee;'>
                            <body style='text-align:center;font-size:10pt;font-family:Tahoma,Verdana,Arial,sans-serif;'>
                                <table width='70%' style='margin-top:10px;background:#fff;' cellspacing='0' cellpadding='20'>
                                <tr>
                                    <td style='padding: 0 30px 30px 30px;text-align:left;'>
                                        {message}
                                    </td>
                                </tr>";
            if (showFooter)
            {
                if (!string.IsNullOrEmpty(footer))
                {
                    message += $@"<tr>
                                    <td style='padding: 0 30px 30px 30px;text-align:left;font-size:80%;'>
                                        {footer
                                            .Replace("{WebSiteUrl}", Config?["Company:WebSiteUrl"])
                                            .Replace("{WebSiteTitle}", Config?["Company:WebSiteTitle"])}
                                    </td>
                                </tr>
                            </body>
                        </html>";
                }
                else
                {
                    message += $@"<tr>
                                    <td style='padding: 0 30px 30px 30px;text-align:left;font-size:80%;'>
                                        ATTENZIONE! Questo &egrave; un messaggio automatico. Se vuoi inviare nuovi commenti, non rispondere 
                                        via e-mail ma usa sempre gli struementi di contatto del portale all'indirizzo 
                                        <a target='_blank' href='{Config?["Company:WebSiteUrl"]}'>{Config?["Company:WebSiteTitle"]}</a>.
                                    </td>
                                </tr>
                            </body>
                        </html>";
                }
            }
            else
            {
                message += @"</body>
                        </html>";
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(EmailSettings.Sender, EmailSettings.SenderName),
                Subject = subject,
                Body = message,
                IsBodyHtml = isBodyHtml,
            };

            foreach (var email in emails)
            {
                mailMessage.To.Add(email);
            }

            if (cc != null)
            {
                foreach (var email in cc)
                {
                    mailMessage.CC.Add(email);
                }
            }

            if (bcc != null)
            {
                foreach (var email in bcc)
                {
                    mailMessage.Bcc.Add(email);
                }
            }

            // Aggiunge gli allegati al messaggio o, se richiesto,
            // aggiunge un unico archivio compresso in formato .ZIP
            if (attachs != null)
            {
                if (zipAllAttachments)
                {
                    var fileBytes = CreateZipArchive(attachs);
                    MemoryStream ms = new(fileBytes);
                    Ms.Add(ms);
                    Attachment attachment = new(ms, "AllFiles.zip", MediaTypeNames.Application.Zip);
                    mailMessage.Attachments.Add(attachment);
                }
                else
                {
                    int i = 0;
                    foreach (var attach in attachs)
                    {
                        var content = attach.Content;
                        if (content != null)
                        {
                            byte[] fileBytes = content;
                            string? fileName = attach.FileName;

                            MemoryStream ms = new(fileBytes);
                            Ms.Add(ms);
                            Attachment attachment = new(ms, fileName, attach.ContentType);
                            mailMessage.Attachments.Add(attachment);

                            i++;
                        }
                    }
                }
            }

            // Aggiunge immagini nel corpo del messaggio
            if (images != null && images.Length > 0)
            {
                // message example: "<html><body><h1>Immagine inclusa</h1><img src='cid:image0'></body></html>";
                AlternateView avHtml = AlternateView.CreateAlternateViewFromString(message, null, MediaTypeNames.Text.Html);

                int i = 0;
                foreach (var image in images)
                {
                    var content = image.Content;
                    if (content != null)
                    {
                        // Caricare i dati binari dell'immagine
                        //byte[] imageBytes = File.ReadAllBytes("percorso/dell/immagine.jpg");
                        MemoryStream imageStream = new(content);

                        // Aggiungere l'immagine come LinkedResource
                        LinkedResource inline = new(imageStream, image.ContentType)
                        {
                            ContentId = $"image{i}"
                        };
                        avHtml.LinkedResources.Add(inline);
                    }

                    i++;
                }

                // Aggiungere l'AlternateView al messaggio
                mailMessage.AlternateViews.Add(avHtml);
            }

            // Utilizza SendAsync per inviare l'email in modo asincrono
            smtpClient.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);
            string? userState = $"SendEmailAsync: {emails.Select(s => s.Address)} | {subject}";

            smtpClient.SendAsync(mailMessage, userState);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger?.LogInformation("EmailSender error: {ErrorMessage} | subject: {Subject} | to: {Recipient}.", ex.Message, subject, emails[0].DisplayName);
        }
    }

    private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
    {
        // Recupera l'oggetto userState passato al metodo SendAsync.
        string? us = e.UserState as string ?? "[Token non disponibile.]";

        if (e.Cancelled)
        {
            if (Logger != null) Logger.LogInformation("Invio {token} annullato.", us);
            else Console.WriteLine($"Invio {us} annullato.");
        }
        if (e.Error != null)
        {
            if (Logger != null) Logger.LogInformation("Errore durante l'invio di {token}: {error}.", us, e.Error);
            else Console.WriteLine($"Errore durante l'invio di {us}: {e.Error}");
        }
        else
        {
            if (Logger != null) Logger.LogInformation("Email {token} inviata con successo.", us);
            else Console.WriteLine($"Email {us} inviata con successo.");
        }

        foreach (MemoryStream ms in Ms)
        {
            ms.Close();
            ms.Dispose();
        }
    }

    public static byte[] CreateZipArchive(EmailAttach[] attachments)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var attach in attachments)
            {
                if (attach.Content != null && !string.IsNullOrEmpty(attach.FileName))
                {
                    var entry = archive.CreateEntry(attach.FileName);
                    using var entryStream = entry.Open();
                    entryStream.Write(attach.Content, 0, attach.Content.Length);
                }
            }
        }

        return memoryStream.ToArray();
    }
}
