using AgendaSalud.Postino.EmailService.Models;

namespace AgendaSalud.Postino.EmailService.Service.Interface
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(EmailRequestDto request);

      
    }
}
