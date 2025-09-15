using AgendaSalud.Postino.EmailService.Models;

namespace AgendaSalud.Postino.EmailService.Queue
{
        public interface IEmailQueueProducer
        {
             Task EnqueueAsync(EmailRequestDto request);
        }
    
}
