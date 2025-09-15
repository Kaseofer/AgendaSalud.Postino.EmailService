namespace AgendaSalud.Postino.EmailService.Config
{
    public class RabbitMqSettings
    {
        public string Uri { get; set; } = string.Empty;
        public string QueueName { get; set; } = "postino_email_queue";
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
    }
}
