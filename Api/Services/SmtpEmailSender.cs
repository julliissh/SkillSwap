using System.Net;
using System.Net.Mail;

namespace Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private const string DefaultFromName = "SkillSwap";

    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendVerificationCodeAsync(string email, string code)
    {
        var host = _configuration["Smtp:Host"]
            ?? throw new InvalidOperationException("SMTP host is not configured.");
        var port = _configuration.GetValue<int>("Smtp:Port");
        var fromEmail = _configuration["Smtp:FromEmail"]
            ?? throw new InvalidOperationException("SMTP sender email is not configured.");
        var fromName = _configuration["Smtp:FromName"] ?? DefaultFromName;
        var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl");

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = "SkillSwap - Email verification",
            Body = $"Your SkillSwap verification code is: {code}\n\n" +
                   $"This code expires in {EmailCodeService.CodeExpirationMinutes} minutes.",
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var smtpClient = new SmtpClient(host, port) { EnableSsl = enableSsl };

        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            smtpClient.Credentials = new NetworkCredential(username, password);
        }

        await smtpClient.SendMailAsync(message);
    }
}
