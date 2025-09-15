namespace AgendaSalud.Postino.EmailService.Infrastructure.Logger
{
    public class FileLoggerFactory : IAppLoggerFactory
    {
        public IAppLogger<T> CreateLogger<T>()
        {
            return new FileLogger<T>();
        }
    }
}