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
