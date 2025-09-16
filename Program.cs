using AgendaSalud.Postino.EmailService.Config;
using AgendaSalud.Postino.EmailService.HealthChecks;
using AgendaSalud.Postino.EmailService.Persistence.Interface;
using AgendaSalud.Postino.EmailService.Queue;
using AgendaSalud.Postino.EmailService.Service;
using AgendaSalud.Postino.EmailService.Service.Interface;
using AgendaSalud.Postino.EmailService.Workers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Postino.EmailService.Persistence;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);



builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMqSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));


// HEALTH CHECKS CONFIGURATION (sin DbContext)
builder.Services.AddHealthChecks()
    // Check básico de la aplicación
    .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))

    // Check personalizado para configuración de email
    .AddCheck<EmailConfigurationHealthCheck>("email-configuration")

    // Check de memoria
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(false);
        var data = new Dictionary<string, object>
        {
            ["allocated"] = allocated,
            ["gen0"] = GC.CollectionCount(0),
            ["gen1"] = GC.CollectionCount(1),
            ["gen2"] = GC.CollectionCount(2)
        };

        // Alertar si usa más de 100MB
        var status = allocated < 100_000_000 ? HealthStatus.Healthy : HealthStatus.Degraded;
        var message = $"Memory usage: {allocated / 1024 / 1024}MB";

        return new HealthCheckResult(status, message, data: data);
    });

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
// HEALTH CHECK ENDPOINTS
app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    timestamp = DateTime.UtcNow,
                    duration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            status = kvp.Value.Status.ToString(),
                            duration = kvp.Value.Duration.TotalMilliseconds,
                            description = kvp.Value.Description
                        }
                    )
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });

// Endpoint simple para Railway
app.MapGet("/health/live", () => Results.Ok(new { status = "alive", timestamp = DateTime.UtcNow }));

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();