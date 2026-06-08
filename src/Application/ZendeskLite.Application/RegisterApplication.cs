using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ZendeskLite.Application.Common.Behaviors;

namespace ZendeskLite.Application
{
    public static class RegisterApplication
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(typeof(RegisterApplication).Assembly);

                // Register the Validation Behavior 
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(typeof(RegisterApplication).Assembly);

            return services;
        }
    }
}
