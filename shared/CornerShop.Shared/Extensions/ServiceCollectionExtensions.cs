using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using Prometheus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CornerShop.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds common Redis configuration for all services
        /// </summary>
        public static IServiceCollection AddCornerShopRedis(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

            // Configure Redis caching
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = $"{serviceName}_";
            });

            // Configure Redis Connection for Streams
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                return ConnectionMultiplexer.Connect(redisConnectionString);
            });

            return services;
        }

        /// <summary>
        /// Adds common health checks for all services
        /// </summary>
        public static IServiceCollection AddCornerShopHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            var mongoConnectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";

            var healthChecks = services.AddHealthChecks()
                .AddRedis(redisConnectionString, name: "redis");

            // Add MongoDB health check if MongoDB connection string is available
            if (configuration.GetConnectionString("MongoDB") != null)
            {
                healthChecks.AddMongoDb(mongoConnectionString, name: "mongodb");
            }

            return services;
        }

        /// <summary>
        /// Adds common HTTP client configuration
        /// </summary>
        public static IServiceCollection AddCornerShopHttpClient(
            this IServiceCollection services,
            int timeoutSeconds = 30)
        {
            services.AddHttpClient("Microservices", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            });

            return services;
        }

        /// <summary>
        /// Adds common Prometheus metrics
        /// </summary>
        public static IApplicationBuilder UseCornerShopMetrics(this IApplicationBuilder app)
        {
            app.UseMetricServer();
            app.UseHttpMetrics();
            return app;
        }

        /// <summary>
        /// Adds common middleware pipeline
        /// </summary>
        public static IApplicationBuilder UseCornerShopPipeline(
            this IApplicationBuilder app,
            IWebHostEnvironment environment)
        {
            // Configure the HTTP request pipeline
            if (environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // Configure Prometheus metrics
            app.UseCornerShopMetrics();

            // Configure health checks
            app.MapHealthChecks("/health");

            return app;
        }
    }
}
