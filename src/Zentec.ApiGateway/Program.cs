using Serilog;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console();
});

// Add CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Forward the Authorization header from client to backend services
        builderContext.AddRequestTransform(async transformContext =>
        {
            var authHeader = transformContext.HttpContext.Request.Headers.Authorization;
            if (!string.IsNullOrEmpty(authHeader))
            {
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
            }

            await Task.CompletedTask;
        });

        // Add custom headers for tracing
        builderContext.AddRequestTransform(transformContext =>
        {
            transformContext.ProxyRequest.Headers.Add("X-Forwarded-For",
                transformContext.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            transformContext.ProxyRequest.Headers.Add("X-Gateway-Version", "1.0");

            return ValueTask.CompletedTask;
        });
    });

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Use Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
    };
});

// Enable CORS
app.UseCors();

// Map health check endpoint
app.MapHealthChecks("/health");

// Simple root endpoint
app.MapGet("/", () => Results.Ok(new
{
    service = "Zentec API Gateway",
    version = "1.0",
    status = "running",
    timestamp = DateTime.UtcNow
}));

// Map reverse proxy
app.MapReverseProxy();

app.Run();