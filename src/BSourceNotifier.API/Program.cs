using BSourceNotifier.Application.Consumers;
using BSourceNotifier.Application.Interfaces;
using BSourceNotifier.Application.UseCases;
using BSourceNotifier.Infrastructure.Channels;
using BSourceNotifier.Infrastructure.Options;
using BSourceNotifier.Infrastructure.SignalR;
using MassTransit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("serilog.json", optional: false, reloadOnChange: true);
builder.Host.UseSerilog((context, services, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration).ReadFrom.Services(services));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notification"));

builder.Services.AddScoped<SendNotificationUseCase>();
builder.Services.AddScoped<INotificationChannel, WebSocketNotificationChannel>();
builder.Services.AddScoped<INotificationChannel, EmailNotificationChannel>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendNotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitSection = builder.Configuration.GetSection("MassTransit:RabbitMq");
        var host = rabbitSection.GetValue<string>("Host") ?? "localhost";
        var virtualHost = rabbitSection.GetValue<string>("VirtualHost") ?? "/";
        var username = rabbitSection.GetValue<string>("Username") ?? "guest";
        var password = rabbitSection.GetValue<string>("Password") ?? "guest";
        var queue = rabbitSection.GetValue<string>("Queue") ?? "notification.send";

        var retrySection = builder.Configuration.GetSection("MassTransit:Retry");
        var retryCount = retrySection.GetValue<int>("RetryCount", 3);
        var intervalSeconds = retrySection.GetValue<int>("IntervalSeconds", 10);

        cfg.Host(host, virtualHost, h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ReceiveEndpoint(queue, e =>
        {
            e.ConfigureConsumer<SendNotificationConsumer>(context);
            e.UseMessageRetry(r => r.Interval(retryCount, TimeSpan.FromSeconds(intervalSeconds)));
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
