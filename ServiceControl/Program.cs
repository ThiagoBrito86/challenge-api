using ServiceControl.DependencyInjection;
using ServiceControl.Infrastructure.Persistence.Contexts;
using ServiceControl.Middleware;
using Serilog;
using ServiceControl.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/application-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddApiServices();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<WorkRecordContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Service Control API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Custom Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<MetricsMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Endpoint adicional para métricas Prometheus (opcional)
app.MapGet("/metrics/prometheus", (ServiceControl.Infrastructure.Services.Metrics.IMetricsCollector metricsCollector) =>
{
    var metrics = metricsCollector.GetSnapshot();
    return Results.Text($"""
        # HELP total_requests Total number of requests
        # TYPE total_requests counter
        total_requests {metrics.TotalRequests}

        # HELP total_errors Total number of errors
        # TYPE total_errors counter
        total_errors {metrics.TotalErrors}

        # HELP error_rate Error rate percentage
        # TYPE error_rate gauge
        error_rate {metrics.ErrorRate}

        # HELP retry_attempts Total retry attempts
        # TYPE retry_attempts counter
        retry_attempts {metrics.RetryAttempts}

        # HELP average_response_time Average response time in milliseconds
        # TYPE average_response_time gauge
        average_response_time {metrics.AverageResponseTime}

        # HELP p95_response_time 95th percentile response time in milliseconds
        # TYPE p95_response_time gauge
        p95_response_time {metrics.P95ResponseTime}

        # HELP p99_response_time 99th percentile response time in milliseconds
        # TYPE p99_response_time gauge
        p99_response_time {metrics.P99ResponseTime}
        """, "text/plain");
});

Log.Information("Starting Road Construction Service API...");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}