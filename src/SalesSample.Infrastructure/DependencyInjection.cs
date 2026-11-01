using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SalesSample.Domain.Repositories;
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

        return services;
    }
}