using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SharedService.Core.Handlers;

namespace FileService.Core;

public static class Registration
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddHandlers(typeof(Registration).Assembly);

        services.AddValidatorsFromAssembly(typeof(Registration).Assembly);

        return services;
    }
}