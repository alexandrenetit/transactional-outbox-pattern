using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SalesSample.Domain.Entities;
using SalesSample.Domain.Services;
using SalesSample.Domain.Specifications;
using SalesSample.Application.Common.Behaviors;

namespace SalesSample.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<ISaleService,SaleService>();
        services.AddScoped<ISpecification<Branch>, ActiveBranchSpecification>();
        
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        return services;
    }
}