using Microsoft.AspNetCore.Cors.Infrastructure;

namespace CornerShop.Services;

public static class CorsService
{
    public static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            // Development policy - allows all origins
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // Production policy - allows specific origins
            options.AddPolicy("AllowSpecificOrigins", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                                   new[] { "http://localhost:3000", "http://localhost:4200", "http://localhost:8080" };

                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();

                if (configuration.GetValue<bool>("Cors:AllowCredentials", false))
                {
                    policy.AllowCredentials();
                }
            });

            // API policy - for API endpoints only
            options.AddPolicy("ApiPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:4200", "http://localhost:8080")
                      .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                      .AllowCredentials();
            });
        });
    }

    public static void UseCorsPolicy(IApplicationBuilder app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            // Use AllowAll policy in development
            app.UseCors("AllowAll");
        }
        else
        {
            // Use specific origins policy in production
            app.UseCors("AllowSpecificOrigins");
        }
    }
}
