using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.Persistence.Interface;
using AgendaSalud.Postino.EmailService.Queue;
using AgendaSalud.Postino.EmailService.Service;
using AgendaSalud.Postino.EmailService.Service.Interface;
using AgendaSalud.Postino.EmailService.Workers;
using Postino.EmailService.Persistence;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);



builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddControllers();

builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory() { HostName = "localhost" };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult(); 

});

var connString = Environment.GetEnvironmentVariable("POSTINO_AUDIT_URL");

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<EmailSender>();
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();
builder.Services.AddScoped<IEmailSender, EmailSender>();

builder.Services.AddSingleton<IEmailQueueProducer, RabbitMqEmailProducer>();



builder.Services.AddHostedService<EmailWorker>(); //todo el rato escucha la cola y manda los mensajes que le van llegando

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();