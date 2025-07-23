using CornerShop.Services;
using MongoDB.Driver;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Prometheus;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Add content negotiation
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
})
.AddRazorRuntimeCompilation()
.AddXmlDataContractSerializerFormatters() // Support XML format
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve property names
});

// Configure CORS
CorsService.ConfigureCors(builder.Services, builder.Configuration);

// Configure Redis caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "CornerShop_";
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CornerShop API",
        Version = "v1",
        Description = "REST API for CornerShop management system with full REST compliance including HATEOAS, versioning, and standardized error responses"
    });

    // Enable XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    // Add error response schema globally
    c.MapType<CornerShop.Models.ErrorResponse>(() => new Microsoft.OpenApi.Models.OpenApiSchema
    {
        Type = "object",
        Properties =
        {
            ["timestamp"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "date-time" },
            ["status"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "integer", Format = "int32" },
            ["error"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
            ["message"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
            ["path"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" }
        }
    });

    // Add request/response examples (for demonstration, can be extended)
    // c.ExampleFilters();
});

// Configure MongoDB
var mongoConnectionString = builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
var mongoClient = new MongoClient(mongoConnectionString);
var database = mongoClient.GetDatabase("cornerShop");

// Register services
builder.Services.AddSingleton<IMongoDatabase>(database);
builder.Services.AddScoped<CornerShop.Services.IDatabaseService>(sp =>
    new CornerShop.Services.MongoDatabaseService(mongoConnectionString, "cornerShop"));
builder.Services.AddSingleton<IStoreService, StoreService>();
builder.Services.AddScoped<CornerShop.Services.SyncService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISaleService, SaleService>();

// Register Saga Orchestrator
builder.Services.AddScoped<ISagaOrchestrator, SagaOrchestrator>();

// Register Saga Event Publisher and State Manager
builder.Services.AddSingleton<ISagaEventPublisher, SagaEventPublisher>();
builder.Services.AddScoped<ISagaStateManager, SagaStateManager>();

// Register Controlled Failure Service
builder.Services.AddSingleton<IControlledFailureService, ControlledFailureService>();

// Register Saga Metrics and Business Event Logging Services
builder.Services.AddSingleton<ISagaMetricsService, SagaMetricsService>();
builder.Services.AddSingleton<IBusinessEventLogger, BusinessEventLogger>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString, name: "mongodb")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis");

// Add JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");

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
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Global exception handler for API endpoints
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                timestamp = DateTime.UtcNow,
                status = 500,
                error = "Internal Server Error",
                message = app.Environment.IsDevelopment() ? ex.Message : "An unexpected error occurred",
                path = context.Request.Path
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
        }
        else
        {
            throw;
        }
    }
});

app.UseStaticFiles();

app.UseRouting();

// Use CORS based on environment
CorsService.UseCorsPolicy(app, app.Environment);

app.UseAuthentication();

app.UseAuthorization();

// Configure Prometheus metrics
app.UseMetricServer();
app.UseHttpMetrics();

// Configure health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CornerShop API V1");
    c.RoutePrefix = "api-docs";
});

// Add Redoc UI
app.MapGet("/redoc", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(@"<!DOCTYPE html><html><head><title>ReDoc</title><link rel='stylesheet' href='https://fonts.googleapis.com/css?family=Montserrat:300,400,700|Roboto:300,400,700'><style>body{margin:0;padding:0;}</style></head><body><redoc spec-url='/swagger/v1/swagger.json'></redoc><script src='https://cdn.redoc.ly/redoc/latest/bundles/redoc.standalone.js'></script></body></html>");
});

// Map API routes
app.MapControllers();

// Map MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize database only if not running in test environment
if (!app.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
        await dbService.InitializeDatabase();

        // Clear existing collections to avoid ObjectId format conflicts
        var mongoDb = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        await mongoDb.DropCollectionAsync("stores");
        await mongoDb.DropCollectionAsync("products");
        await mongoDb.DropCollectionAsync("sales");

        // Re-initialize after clearing
        await dbService.InitializeDatabase();

        var storeService = scope.ServiceProvider.GetRequiredService<IStoreService>();
        var stores = await storeService.GetAllStores();
        foreach (var store in stores)
        {
            CornerShop.Services.LocalStoreDatabaseHelper.CreateLocalDatabaseForStore(store.Id);
        }
    }
}

app.Run();

// Make Program class public for integration testing
public partial class Program { }
