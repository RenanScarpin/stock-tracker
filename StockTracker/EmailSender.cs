using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

public class EmailSettings
{
    public string To { get; set; } = "";
    public string From { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string Username { get; set; } = "";
}

public class EmailSender
{
    private readonly EmailSettings _settings;
    private readonly string _password;

    public EmailSender(EmailSettings settings, string password)
    {
        _settings = settings;
        _password = password;
    }

    public async Task SendAsync(string subject, string body)
    {
        var message = new MimeMessage();

        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(_settings.To));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var smtp = new SmtpClient();

        await smtp.ConnectAsync(
            _settings.Host,
            _settings.Port,
            SecureSocketOptions.StartTls
        );

        await smtp.AuthenticateAsync(_settings.Username, _password);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
