using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System.Reflection;
using System.Text;
using Zentec.OrderService.Data;
using Zentec.OrderService.Messaging;
using Zentec.OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Zentec Order Service API",
        Version = "v1",
        Description = "Shopping cart, order creation, tracking, and payments. Publishes order events to RabbitMQ."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY the JWT here. Swagger will add `Bearer ` automatically."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var signingKey = jwtSettings["SigningKey"] ?? throw new Exception("JwtSettings:SigningKey missing.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// Http clients to other services
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)));

builder.Services.AddHttpClient<IProductClient, ProductClient>(client =>
{
    var baseUrl = builder.Configuration["Services:ProductServiceBaseUrl"]
                 ?? throw new Exception("Services:ProductServiceBaseUrl missing.");
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IPaymentClient, PaymentClient>(client =>
{
    var baseUrl = builder.Configuration["Services:PaymentServiceBaseUrl"]
                 ?? throw new Exception("Services:PaymentServiceBaseUrl missing.");
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy);

builder.Services.AddHttpClient<IUserClient, UserClient>(client =>
{
    var baseUrl = builder.Configuration["Services:UserServiceBaseUrl"]
                 ?? throw new Exception("Services:UserServiceBaseUrl missing.");
    client.BaseAddress = new Uri(baseUrl);
})
.AddPolicyHandler(retryPolicy);

// RabbitMQ publisher
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<PaymentEventsConsumer>();

// Business services
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.UseAuthentication();
app.UseAuthorization();

// Ensure DB exists (simple code-first bootstrap)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();

app.Run();