using MongoDB.Driver;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase("cornerShop");

builder.Services.AddSingleton<IMongoDatabase>(database);

// Add health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString, name: "mongodb");

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
