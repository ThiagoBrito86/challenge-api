using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using ServiceControl.Application.DTOs;
using ServiceControl.Application.UseCases.ProcessWorkRecord;
using ServiceControl.Application.Validators;
using System.Reflection;


namespace ServiceControl.DependencyInjection;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Controllers
        services.AddControllers();

        // Application Services
        services.AddScoped<IProcessWorkRecordUseCase, ProcessWorkRecordUseCase>();
        services.AddScoped<IBatchProcessWorkRecordUseCase, BatchProcessWorkRecordUseCase>();

        // Validators
        services.AddScoped<IValidator<WorkRecordRequestDto>, WorkRecordRequestValidator>();

        // MediatR for Domain Events
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = " Service Control API",
                Version = "v1",
                Description = "API para controle de obras rodoviárias com integração meteorológica",
                Contact = new OpenApiContact
                {
                    Name = "Construtora Team",
                    Email = "dev@construtora.com"
                }
            });

            // Incluir comentários XML
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Configurar Bearer Token
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
        });

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Health Checks
        services.AddHealthChecks()
        .AddSqlServer(
        connectionString: "DefaultConnection",
        name: "sqlserver",
        healthQuery: "SELECT 1;",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" })
        .AddCheck<WeatherServiceHealthCheck>("weather_service");

        return services;
    }
}

public class WeatherServiceHealthCheck : IHealthCheck
{
    private readonly Domain.Intefaces.Services.IWeatherService _weatherService;

    public WeatherServiceHealthCheck(Domain.Intefaces.Services.IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _weatherService.IsServiceHealthyAsync(cancellationToken);
            return isHealthy
                ? HealthCheckResult.Healthy("Weather service está ativo")
                : HealthCheckResult.Unhealthy("Weather service esta inativo");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Weather service check falhou", ex);
        }
    }
}