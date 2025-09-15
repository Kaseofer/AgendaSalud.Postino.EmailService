namespace AgendaSalud.Postino.EmailService.Infrastructure.Logger
{
    public interface IAppLoggerFactory
    {
        IAppLogger<T> CreateLogger<T>();
    }
}
