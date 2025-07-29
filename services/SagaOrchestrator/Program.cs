using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using SagaOrchestrator.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "SagaOrchestrator_";
});

// Configure Redis Connection for Streams
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

// Register saga services
builder.Services.AddScoped<ISagaOrchestrator, SagaOrchestratorService>();
builder.Services.AddScoped<ISagaStateManager, SagaStateManager>();
builder.Services.AddSingleton<IEventProducer, EventProducer>();

// Configure HTTP client for microservice communication
builder.Services.AddHttpClient("Microservices", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Configure Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Map controllers
app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

app.Run();
