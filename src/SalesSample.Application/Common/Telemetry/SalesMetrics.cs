using System.Diagnostics.Metrics;

namespace SalesSample.Application.Common.Telemetry;

/// <summary>
/// Métricas de NEGÓCIO (não confundir com as de infra: HTTP, GC...).
///
/// Meter estático: o .NET recomenda UM Meter por app.
/// O Program.cs registra com AddMeter(SalesMetrics.MeterName) e o
/// Prometheus exporter expõe em /metrics.
/// </summary>
public static class SalesMetrics
{
    public const string MeterName = "SalesSample";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    // Counter = só CRESCE (é um total acumulado).
    // No /metrics vira "sales_created_total" (o exporter acrescenta o sufixo _total).
    private static readonly Counter<long> SalesCreated =
        Meter.CreateCounter<long>(
            name: "sales_created",
            description: "Quantidade de vendas criadas");

    public static void SaleCreated() => SalesCreated.Add(1);
}