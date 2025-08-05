using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using CornerShop.Shared.Extensions;
using ChoreographedSagaCoordinator.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure shared services
builder.Services.AddCornerShopRedis(builder.Configuration, "ChoreographedSagaCoordinator");
builder.Services.AddCornerShopHealthChecks(builder.Configuration);
builder.Services.AddCornerShopHttpClient();

// Register service-specific dependencies
builder.Services.AddScoped<IChoreographedSagaCoordinator, ChoreographedSagaCoordinatorService>();
builder.Services.AddScoped<IChoreographedSagaStateManager, ChoreographedSagaStateManager>();
builder.Services.AddSingleton<IEventProducer, EventProducer>();
builder.Services.AddSingleton<IEventConsumer, EventConsumer>();

var app = builder.Build();

// Configure shared middleware pipeline
app.UseCornerShopPipeline(app.Environment);

app.MapControllers();

app.Run();
