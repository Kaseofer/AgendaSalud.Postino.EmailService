using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Models;
using AgendaSalud.Postino.EmailService.Persistence.Interface;
using AgendaSalud.Postino.EmailService.Service.Interface;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace AgendaSalud.Postino.EmailService.Service
{

    public class EmailSender : IEmailSender
    {
        protected readonly EmailSettings _settings;
        protected readonly IEmailLogRepository _emailRepository;

        public EmailSender(IOptions<EmailSettings> options,IEmailLogRepository repo)
        {
            _settings = options.Value;
            _emailRepository = repo;
        }
        public async Task<bool> SendAsync(EmailRequestDto request)
        {
            try

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
                    Subject = request.Subject,
                    Body = _settings.IsBodyHtml==true ? request.HtmlBody: request.TextBody,
                    IsBodyHtml = _settings.IsBodyHtml
                };

                mail.To.Add(request.To); // Cambiá esto por tu correo de prueba

                try
                {
                    await client.SendMailAsync(mail);
                    Console.WriteLine("✅ Correo enviado correctamente.");

                    await _emailRepository.LogAsync(request.MessageId, "Envio Exitoso",request.To, request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al enviar: {ex.Message}");
                    await _emailRepository.LogAsync(request.MessageId, "Envio Fallido",request.To, ex.Message);
                }

                return true;
            }
            catch (Exception)
            {

                return false;
            }
}
    }
}
