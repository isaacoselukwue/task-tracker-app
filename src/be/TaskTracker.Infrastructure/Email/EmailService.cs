global using MailKit.Net.Smtp;
global using MailKit.Security;
global using Microsoft.Extensions.Options;
global using MimeKit;
global using TaskTracker.Application.Common.Interfaces;
global using TaskTracker.Domain.Events;

namespace TaskTracker.Infrastructure.Email;
internal class EmailService(IOptions<MailSettings> mailSettings) : IEmailService
{
    private readonly MailSettings mailSettings = mailSettings.Value;
    public async Task SendEmail(NotificationEvent notification, CancellationToken cancellationToken)
    {
        MimeMessage mail = new();
        #region Sender / Receiver
        mail.From.Add(new MailboxAddress(mailSettings?.DisplayName, mailSettings?.From));
        mail.Sender = new MailboxAddress(mailSettings?.DisplayName, mailSettings?.From);

        mail.To.Add(MailboxAddress.Parse(notification.Receiver));
        #endregion

        #region Content
        BodyBuilder body = new();
        mail.Subject = notification.Subject;
        string templateName = notification.NotificationType.ToString();
        notification.Replacements.TryAdd("{{year}}", DateTime.Now.Year.ToString());
        notification.Replacements.TryAdd("{{LinkToWebApp}}", mailSettings?.BaseUrl ?? "");
        string emailContent = LoadAndReplaceTemplate(templateName, notification.Replacements ?? []);
        body.HtmlBody = emailContent;
        mail.Body = body.ToMessageBody();

        #endregion

        #region Send Mail

        using SmtpClient smtp = new();

        smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

        if (mailSettings!.UseSSL)
        {
            await smtp.ConnectAsync(mailSettings.Host, mailSettings.Port, SecureSocketOptions.SslOnConnect, cancellationToken);
        }
        else if (mailSettings.UseStartTls)
        {
            await smtp.ConnectAsync(mailSettings.Host, mailSettings.Port, SecureSocketOptions.StartTls, cancellationToken);
        }
        else
        {
            await smtp.ConnectAsync(mailSettings?.Host, mailSettings!.Port, true, cancellationToken);
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
        }
        if (mailSettings!.UseAuthentication)
            await smtp.AuthenticateAsync(mailSettings?.UserName, mailSettings?.Password, cancellationToken);
        string resp = await smtp.SendAsync(mail, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        #endregion
    }


    private static string LoadAndReplaceTemplate(string templateName, Dictionary<string, string> replacements)
    {
        string templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", $"{templateName}.html");
        string templateContent = File.ReadAllText(templatePath);

        foreach (var replacement in replacements)
        {
            templateContent = templateContent.Replace(replacement.Key, replacement.Value);
        }

        return templateContent;
    }
}
