using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SalesSample.Application;
using SalesSample.Infrastructure;
using SalesSample.WebApi.Middleware;   

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen();   // gera o documento OpenAPI

builder.Services.AddProblemDetails();                                  // ← registra o serviço
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();        // ← registra o handler

// ---- Observabilidade: OpenTelemetry -> OTLP -> Datadog Agent ----
var otlp = new Uri(builder.Configuration["Telemetry:OtlpEndpoint"] ?? "http://localhost:4319");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r
        .AddService(serviceName: "SalesSample", serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()   // trace das requests HTTP
        .AddHttpClientInstrumentation()   // trace das chamadas HTTP de saída
        .AddOtlpExporter(o => o.Endpoint = otlp))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()   // métricas HTTP
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()      // GC, CPU, memória
        .AddPrometheusExporter());

// Logs também via OTLP
builder.Logging.AddOpenTelemetry(logging =>
    logging.AddOtlpExporter(o => o.Endpoint = otlp));

var app = builder.Build();

app.UseSwagger();       // expõe /swagger/v1/swagger.json
app.UseSwaggerUI();     // expõe a UI em /swagger

app.UseExceptionHandler();   // ← captura exceção e vira ProblemDetails

app.MapControllers();
app.MapPrometheusScrapingEndpoint();   // expõe /metrics

app.Run();