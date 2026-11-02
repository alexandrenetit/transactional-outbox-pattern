using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesSample.Domain.Repositories;
using SalesSample.Infrastructure.Messaging;
using SalesSample.Infrastructure.Persistence;
using SalesSample.Infrastructure.Persistence.Repositories;

namespace SalesSample.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SalesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SalesDbContext>());
        
        // kafka
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };

            return new ProducerBuilder<string, string>(config).Build();
        });
        
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        services.AddHostedService<OutboxPublisherWorker>();

        return services;
    }
}