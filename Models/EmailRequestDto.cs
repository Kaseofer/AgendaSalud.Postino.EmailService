namespace AgendaSalud.Postino.EmailService.Models
{
    public class EmailRequestDto
    {
        public string To { get; set; } = null!;
        public string Subject { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string? Template { get; set; }
        public string Source { get; set; } = null!;
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    }
}
