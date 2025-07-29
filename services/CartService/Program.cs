using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using CartService.Services;
using Prometheus;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "CartService_";
});

builder.Services.AddScoped<ICartService, CartService.Services.CartService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis");

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Configure health checks
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
