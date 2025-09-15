using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Models;
using AgendaSalud.Postino.EmailService.Service.Interface;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AgendaSalud.Postino.EmailService.Service
{

    public class EmailSender : IEmailSender
    {
        protected readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }
        public async Task SendAsync(EmailRequestDto request)
        {
            {

                var client = new SmtpClient(_settings.SmtpServer)
                {
                    Port = _settings.SmtpPort,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    EnableSsl = _settings.EnableSsl
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail),
                    Subject = "📬 Prueba desde consola",
                    Body = File.ReadAllText("plantilla.html"),
                    IsBodyHtml = _settings.IsBodyHtml
                };

                mail.To.Add("fernando.garin.tejedor@gmail.com"); // Cambiá esto por tu correo de prueba

                try
                {
                    await client.SendMailAsync(mail);
                    Console.WriteLine("✅ Correo enviado correctamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al enviar: {ex.Message}");
                }
            }
        }
    }
}
