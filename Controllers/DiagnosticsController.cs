using AgendaSalud.Postino.EmailService.Models;
using AgendaSalud.Postino.EmailService.Service.Interface;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly IEmailSender _emailSender;

    public DiagnosticsController(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    [HttpGet("ping-smtp")]
    public async Task<IActionResult> PingSmtp()
    {
        var testEmail = new EmailRequestDto
        {
            To = "kaseofer@gmail.com", // Cambiá esto por tu correo real
            Subject = "🔧 Test SMTP desde Railway",
            TextBody = "Este es un correo de prueba para validar la conexión SMTP.",
            HtmlBody = "<p><strong>Este es un correo de prueba</strong> para validar la conexión SMTP.</p>",
            MessageId = Guid.NewGuid().ToString()
        };

        var result = await _emailSender.SendAsync(testEmail);

        if (result)
            return Ok("✅ SMTP operativo. Correo enviado correctamente.");
        else
            return StatusCode(500, "❌ Fallo en el envío. Revisá configuración SMTP o logs.");
    }
}
