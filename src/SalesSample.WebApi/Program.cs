using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesSample.Application;
using SalesSample.Infrastructure;
using SalesSample.WebApi.Middleware;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using SalesSample.Application.Common.Telemetry;
using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- HealthCheck -> Liveness & Readiness 
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(3))
    .AddKafka(
        new ProducerConfig
        {
            BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
        },
        name: "kafka",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(3));

// ---- Observabilidade: OpenTelemetry (traces + métricas) ----
var otlp = new Uri(builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4320");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "SalesSample", serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation() // trace das requests HTTP
        .AddHttpClientInstrumentation() // trace das chamadas HTTP de saída
        .AddOtlpExporter(o => o.Endpoint = otlp)) // OTLP -> Tempo
    .WithMetrics(m => m
        .AddMeter(SalesMetrics.MeterName)   // ← métrica de negócio
        .AddAspNetCoreInstrumentation() // métricas HTTP
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation() // GC, CPU, memória
        .AddPrometheusExporter()); // /metrics -> Prometheus

// ---- Logs: Serilog -> console + Loki ----
builder.Host.UseSerilog((context, cfg) => cfg
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        context.Configuration["Loki:Url"] ?? "http://localhost:3101",
        new[] { new LokiLabel { Key = "app", Value = "salessample" } }));

var app = builder.Build();

app.UseSwagger(); // /swagger/v1/swagger.json
app.UseSwaggerUI(); // UI em /swagger

app.UseExceptionHandler(); // exceção -> ProblemDetails

app.MapControllers();
app.MapPrometheusScrapingEndpoint(); // /metrics

// Liveness: a API está viva? (não checa dependências)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

// Readiness: pode receber tráfego? (DB + Kafka saudáveis)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
    }
});

app.Run();