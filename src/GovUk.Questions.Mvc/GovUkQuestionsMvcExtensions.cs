using GovUk.Questions.Mvc.Filters;
using GovUk.Questions.Mvc.State;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GovUk.Questions.Mvc;

/// <summary>
/// Extension methods for setting up GovUk.Questions.Mvc.
/// </summary>
public static class GovUkQuestionsMvcExtensions
{
    /// <summary>
    /// Adds GovUk.Questions.Mvc services to the specified <see cref="IServiceCollection"/>.
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
    /// Adds GovUk.Questions.Mvc services to the specified <see cref="IServiceCollection"/>.
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
        services.AddTransient<JourneyInstanceProvider>();
        services.AddTransient<ValidateJourneyFilter>();

        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new ServiceFilterAttribute<ValidateJourneyFilter> { Order = ValidateJourneyFilter.Order });
        });

        services.Configure(configureOptions);

        return services;
    }
}
