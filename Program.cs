using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Queue;
using AgendaSalud.Postino.EmailService.Service;
using AgendaSalud.Postino.EmailService.Service.Interface;
using AgendaSalud.Postino.EmailService.Workers;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddControllers();
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory() { HostName = "localhost" };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult(); 

});
builder.Services.AddSingleton<IEmailQueueProducer, RabbitMqEmailProducer>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();

builder.Services.AddHostedService<EmailWorker>(); //todo el rato escucha la cola y manda los mensajes que le van llegando

var app = builder.Build();
app.MapControllers();
app.Run();