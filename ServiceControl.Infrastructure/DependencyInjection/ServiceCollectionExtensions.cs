using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceControl.Application.DTOs;
using ServiceControl.Domain.Intefaces.MessageBrokers;
using ServiceControl.Domain.Intefaces.Repositories;
using ServiceControl.Domain.Intefaces.Services;
using ServiceControl.Infrastructure.MessageBrokers.Http;
using ServiceControl.Infrastructure.Persistence.Contexts;
using ServiceControl.Infrastructure.Persistence.Repositories;
using ServiceControl.Infrastructure.Services.Health;
using ServiceControl.Infrastructure.Services.Metrics;
using ServiceControl.Infrastructure.Services.Resilience;
using ServiceControl.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ServiceControl.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<WorkRecordContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // Usar InMemory para desenvolvimento/testes
                options.UseInMemoryDatabase("WorkRecordDb");
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        // Repositories
        services.AddScoped<IWorkRecordRepository, WorkRecordRepository>();

        // Resilience
        services.AddScoped<IRetryPolicy, ExponentialBackoffRetryPolicy>();

        // External Services
        services.AddHttpClient<IWeatherService, OpenWeatherMapService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Message Brokers
        services.AddHttpClient<IMessageBroker<WorkRecordResponseDto>, HttpMessageBroker<WorkRecordResponseDto>>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Health and Metrics
        services.AddSingleton<IMetricsCollector, MetricsCollector>();
        services.AddScoped<IHealthChecker, HealthChecker>();

        return services;
    }
}