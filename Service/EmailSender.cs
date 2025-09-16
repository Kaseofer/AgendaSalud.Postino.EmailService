using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Models;
using AgendaSalud.Postino.EmailService.Persistence.Interface;
using AgendaSalud.Postino.EmailService.Service.Interface;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

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
        // VERSIÓN CORREGIDA para Railway
        public async Task<bool> SendAsync(EmailRequestDto request)
        {
            try
            {
                using var client = new SmtpClient(_settings.SmtpServer)
                {
                    Port = _settings.SmtpPort,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    EnableSsl = _settings.EnableSsl,

                    // CONFIGURACIONES CRÍTICAS para Railway:
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,  // MUY IMPORTANTE
                    Timeout = 30000, // 30 segundos

                    // Para debugging en Railway:
                    DeliveryFormat = SmtpDeliveryFormat.International
                };

                using var mail = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail),
                    Subject = request.Subject,
                    Body = _settings.IsBodyHtml ? request.HtmlBody : request.TextBody,
                    IsBodyHtml = _settings.IsBodyHtml
                };

                mail.To.Add(request.To);

                // LOGGING DETALLADO para identificar el problema:
                Console.WriteLine($"🔄 Enviando email...");
                Console.WriteLine($"📧 SMTP: {_settings.SmtpServer}:{_settings.SmtpPort}");
                Console.WriteLine($"🔐 De: {_settings.SenderEmail}");
                Console.WriteLine($"📬 Para: {request.To}");
                Console.WriteLine($"🔒 SSL: {_settings.EnableSsl}");

                await client.SendMailAsync(mail);

                Console.WriteLine("✅ Correo enviado correctamente.");
                await _emailRepository.LogAsync(request.MessageId, "Envio Exitoso", request.To, request);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                // MANEJO ESPECÍFICO de errores SMTP
                Console.WriteLine($"❌ Error SMTP: {smtpEx.StatusCode}");
                Console.WriteLine($"📋 Mensaje: {smtpEx.Message}");
                Console.WriteLine($"🔍 Inner Exception: {smtpEx.InnerException?.Message}");

                await _emailRepository.LogAsync(request.MessageId, $"Error SMTP: {smtpEx.StatusCode} - {smtpEx.Message}", request.To, smtpEx);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general: {ex.Message}");
                Console.WriteLine($"🔍 Stack Trace: {ex.StackTrace}");

                await _emailRepository.LogAsync(request.MessageId, "Envio Fallido", request.To, ex);
                return false;
            }
        }

    }
}
