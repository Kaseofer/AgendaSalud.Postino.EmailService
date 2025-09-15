using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AgendaSalud.Postino.EmailService.Queue
{
    public class RabbitMqEmailProducer : IEmailQueueProducer
    {
        private readonly IChannel _channel;
        private readonly string _queueName;
        protected readonly RabbitMqSettings _rabbitMqSettings;

        public RabbitMqEmailProducer(IConnection connection, IOptions<RabbitMqSettings> options)
        {
            _rabbitMqSettings = options.Value;

            _queueName = _rabbitMqSettings.QueueName;

            // Crear canal de forma asíncrona
            _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declarar la cola
            _channel.QueueDeclareAsync(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            ).GetAwaiter().GetResult();
        }

        public async Task EnqueueAsync(EmailRequestDto request)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString()
            };

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: _queueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: CancellationToken.None
            );

            Console.WriteLine($"📤 Enqueued email to {request.To} with MessageId: {props.MessageId}");
        }
    }
}
