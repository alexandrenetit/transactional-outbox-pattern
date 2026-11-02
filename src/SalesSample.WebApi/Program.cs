using SalesSample.Application;
using SalesSample.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddApplication();            // MediatR, validadores, domain services
builder.Services.AddInfrastructure(builder.Configuration);  // DbContext, repos, Kafka, workers

var app = builder.Build();

app.MapControllers();

app.Run();