using Microsoft.Extensions.Hosting;
using AgendaSalud.Postino.EmailService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgendaSalud.Postino.EmailService.Service.Interface;

namespace AgendaSalud.Postino.EmailService.Workers
{
    public class EmailWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IEmailSender _emailSender;
        private IChannel _channel;

        public EmailWorker(IConnection connection, IEmailSender emailSender)
        {
            _connection = connection;
            _emailSender = emailSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: "postino_email_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var request = JsonSerializer.Deserialize<EmailRequestDto>(message);
                    var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();

                    if (request != null)
                    {
                        Console.WriteLine($"📥 Processing email to {request.To} with MessageId: {messageId}");
                        await _emailSender.SendAsync(request);
                    }

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    var isRecoverable = !(ex is JsonException);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: isRecoverable);
                }
            };


            await _channel.BasicConsumeAsync(
                queue: "postino_email_queue",
                autoAck: false,
                consumer: consumer
            );

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // Mantiene el worker vivo
            }
        }
    }
}
