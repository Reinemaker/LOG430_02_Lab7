using CornerShop.Shared.Interfaces;
using CornerShop.Shared.Models;
using CornerShop.Shared.Extensions;
using CustomerService.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure shared services
builder.Services.AddCornerShopRedis(builder.Configuration, "CustomerService");
builder.Services.AddCornerShopHealthChecks(builder.Configuration);
builder.Services.AddCornerShopHttpClient();

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase("cornerShop");

builder.Services.AddSingleton<IMongoDatabase>(database);

var app = builder.Build();

// Configure shared middleware pipeline
app.UseCornerShopPipeline(app.Environment);

app.MapControllers();

app.Run();
