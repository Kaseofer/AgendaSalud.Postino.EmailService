using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Models;
using AgendaSalud.Postino.EmailService.Persistence.Interface;
using AgendaSalud.Postino.EmailService.Service.Interface;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AgendaSalud.Postino.EmailService.Service
{
    public class EmailSender : IEmailSender
    {
        protected readonly EmailSettings _settings;
        protected readonly IEmailLogRepository _emailRepository;
        private readonly HttpClient _httpClient;

        public EmailSender(IOptions<EmailSettings> options, IEmailLogRepository repo, HttpClient httpClient)
        {
            _settings = options.Value;
            _emailRepository = repo;
            _httpClient = httpClient;
        }

        // VERSIÓN CON MAILEROO API (sin SMTP)
        public async Task<bool> SendAsync(EmailRequestDto request)
        {
            try
            {
                // Payload para la API de Maileroo
                var payload = new
                {
                    to = new[] { request.To },
                    from = new
                    {
                        email = _settings.SenderEmail,
                        name = "AgendaSalud Notificaciones"
                    },
                    subject = request.Subject,
                    html = _settings.IsBodyHtml ? request.HtmlBody : request.TextBody,
                    text = request.TextBody ?? request.HtmlBody
                };

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Limpiar headers previos y agregar autenticación
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.SenderPassword);

                // LOGGING DETALLADO
                Console.WriteLine($"🔄 Enviando email via API...");
                Console.WriteLine($"📧 Endpoint: https://smtp.maileroo.com/send");
                Console.WriteLine($"🔐 De: {_settings.SenderEmail}");
                Console.WriteLine($"📬 Para: {request.To}");
                Console.WriteLine($"📄 Payload: {json}");

                // Enviar via API
                var response = await _httpClient.PostAsync("https://smtp.maileroo.com/send", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"📋 Status: {response.StatusCode}");
                Console.WriteLine($"📄 Response: {responseBody}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Correo enviado correctamente via API.");

                    // Log exitoso con información de la respuesta
                    var successInfo = new
                    {
                        Method = "API",
                        StatusCode = response.StatusCode.ToString(),
                        Response = responseBody,
                        Timestamp = DateTime.UtcNow
                    };

                    await _emailRepository.LogAsync(request.MessageId, "Envio Exitoso (API)", request.To, successInfo);
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ Error API: {response.StatusCode}");

                    var errorInfo = new
                    {
                        Method = "API",
                        StatusCode = response.StatusCode.ToString(),
                        Response = responseBody,
                        ReasonPhrase = response.ReasonPhrase,
                        Timestamp = DateTime.UtcNow
                    };

                    await _emailRepository.LogAsync(request.MessageId, $"Error API: {response.StatusCode} - {response.ReasonPhrase}", request.To, errorInfo);
                    return false;
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"❌ Error HTTP: {httpEx.Message}");
                Console.WriteLine($"🔍 Inner Exception: {httpEx.InnerException?.Message}");

                var errorInfo = new
                {
                    Type = "HttpRequestException",
                    Message = httpEx.Message,
                    InnerMessage = httpEx.InnerException?.Message ?? "N/A",
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Error HTTP", request.To, errorInfo);
                return false;
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                Console.WriteLine($"❌ Timeout al enviar email: {tcEx.Message}");

                var errorInfo = new
                {
                    Type = "TimeoutException",
                    Message = "La solicitud tardó demasiado tiempo",
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Timeout", request.To, errorInfo);
                return false;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"❌ Error de serialización JSON: {jsonEx.Message}");

                var errorInfo = new
                {
                    Type = "JsonException",
                    Message = jsonEx.Message,
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Error JSON", request.To, errorInfo);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error general: {ex.Message}");
                Console.WriteLine($"🔍 Tipo: {ex.GetType().Name}");
                Console.WriteLine($"🔍 Stack Trace: {ex.StackTrace}");

                var errorInfo = new
                {
                    Type = ex.GetType().Name,
                    Message = ex.Message,
                    Source = ex.Source ?? "N/A",
                    Timestamp = DateTime.UtcNow
                };

                await _emailRepository.LogAsync(request.MessageId, "Error General", request.To, errorInfo);
                return false;
            }
        }
    }
}