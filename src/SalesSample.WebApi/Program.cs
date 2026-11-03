using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesSample.Application;
using SalesSample.Infrastructure;
using SalesSample.WebApi.Middleware;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---- Observabilidade: OpenTelemetry (traces + métricas) ----
var otlp = new Uri(builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4320");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "SalesSample", serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()   // trace das requests HTTP
        .AddHttpClientInstrumentation()   // trace das chamadas HTTP de saída
        .AddOtlpExporter(o => o.Endpoint = otlp))   // OTLP -> Tempo
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()   // métricas HTTP
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()      // GC, CPU, memória
        .AddPrometheusExporter());        // /metrics -> Prometheus

// ---- Logs: Serilog -> console + Loki ----
builder.Host.UseSerilog((context, cfg) => cfg
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        context.Configuration["Loki:Url"] ?? "http://localhost:3101",
        new[] { new LokiLabel { Key = "app", Value = "salessample" } }));

var app = builder.Build();

app.UseSwagger();       // /swagger/v1/swagger.json
app.UseSwaggerUI();     // UI em /swagger

app.UseExceptionHandler();   // exceção -> ProblemDetails

app.MapControllers();
app.MapPrometheusScrapingEndpoint();   // /metrics

app.Run();