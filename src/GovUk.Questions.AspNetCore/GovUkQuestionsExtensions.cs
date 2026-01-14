using GovUk.Questions.AspNetCore.Description;
using GovUk.Questions.AspNetCore.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GovUk.Questions.AspNetCore;

/// <summary>
/// Extension methods for setting up GovUk.Questions.AspNetCore.
/// </summary>
public static class GovUkQuestionsExtensions
{
    /// <summary>
    /// Adds GovUk.Questions.AspNetCore services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection AddGovUkQuestions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddGovUkQuestions(services, _ => { });
    }

    /// <summary>
    /// Adds GovUk.Questions.AspNetCore services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An <see cref="Action{GovUkQuestionsOptions}"/> to configure the provided <see cref="GovUkQuestionsOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IServiceCollection AddGovUkQuestions(
        this IServiceCollection services,
        Action<GovUkQuestionsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddHttpContextAccessor();
        services.TryAddSingleton<IJourneyStateStorage, SessionJourneyStateStorage>();
        services.AddTransient<IJourneyInstanceProvider, JourneyInstanceProvider>();
        services.AddTransient<ValidateJourneyFilter>();

        services
            .AddMvcCore(options =>
            {
                options.Filters.Add(new ServiceFilterAttribute<ValidateJourneyFilter> { Order = ValidateJourneyFilter.Order });
            })
            .AddJourneys();

        services.Configure(configureOptions);

        return services;
    }

    /// <summary>
    /// Adds an implementation of <see cref="IJourneyStateStorage"/> that uses the local filesystem.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDevelopmentJourneyStateStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IJourneyStateStorage, DevelopmentJourneyStateStorage>();

        return services;
    }

    private static IMvcCoreBuilder AddJourneys(this IMvcCoreBuilder builder)
    {
        var journeyRegistryProvider = new JourneyRegistryProvider();
        var journeyRegistry = journeyRegistryProvider.CreateRegistry(builder.PartManager);

        var nonGenericCoordinatorType = typeof(JourneyCoordinator);

        foreach (var coordinatorType in journeyRegistry.GetAllCoordinatorFactoryTypes().Append(nonGenericCoordinatorType))
        {
            builder.Services.TryAddTransient(
                coordinatorType,
                sp =>
                {
                    var instanceProvider = sp.GetRequiredService<IJourneyInstanceProvider>();

                    var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext ??
                        throw new InvalidOperationException("No HttpContext is available.");

                    var coordinator = instanceProvider.GetJourneyInstance(httpContext);

                    if (coordinator is null || !coordinator.GetType().IsAssignableTo(coordinatorType))
                    {
                        throw new InvalidOperationException($"Could not resolve journey for '{coordinatorType.FullName}'.");
                    }

                    return coordinator;
                });
        }

        builder.Services.AddSingleton(journeyRegistry);

        return builder;
    }
}
