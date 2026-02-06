using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using Zentec.NotificationService.Messaging;
using Zentec.NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Zentec Notification Service API",
        Version = "v1",
        Description = "Consumes order events from RabbitMQ and sends email notifications via Gmail SMTP."
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// Configure RabbitMQ options
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));

// Register email service
builder.Services.AddScoped<IEmailService, GmailEmailService>();

// Register background service for consuming RabbitMQ messages
builder.Services.AddHostedService<OrderEventsConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();