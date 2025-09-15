namespace AgendaSalud.Postino.EmailService.Persistence.Interface
{
    public interface IEmailLogRepository
    {
        Task LogAsync(string entityId, string eventType,string to, object payload);
    }
}
