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
                // OPCIÓN 1: Probar puerto 465 (SSL directo)
                using var client = new SmtpClient(_settings.SmtpServer)
                {
                    Port = 465, // Cambiar a puerto SSL
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    EnableSsl = true,

                    // CONFIGURACIONES CRÍTICAS para Railway:
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 60000, // 60 segundos más tiempo

                    // Configuraciones adicionales para problemas de red:
                    TargetName = "STARTTLS/smtp.maileroo.com"
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

                // Crear un objeto simple para evitar problemas de serialización
                var errorInfo = new
                {
                    StatusCode = smtpEx.StatusCode.ToString(),
                    Message = smtpEx.Message,
                    InnerMessage = smtpEx.InnerException?.Message ?? "N/A"
                };

                await _emailRepository.LogAsync(request.MessageId, $"Error SMTP: {smtpEx.StatusCode} - {smtpEx.Message}", request.To, errorInfo);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general: {ex.Message}");
                Console.WriteLine($"🔍 Tipo: {ex.GetType().Name}");

                // Objeto simple sin referencias complejas
                var errorInfo = new
                {
                    Type = ex.GetType().Name,
                    Message = ex.Message,
                    Source = ex.Source ?? "N/A"
                };

                await _emailRepository.LogAsync(request.MessageId, "Envio Fallido", request.To, errorInfo);
                return false;
            }
        }
    }
}
