using GovUk.Questions.Mvc.State;
using GovUk.Questions.Mvc.Testing.State;
using Microsoft.Extensions.DependencyInjection;

namespace GovUk.Questions.Mvc.Testing;

/// <summary>
/// Extension methods for setting up test helpers for GovUk.Questions.Mvc.
/// </summary>
public static class GovUkQuestionsMvcTestingExtensions
{
    /// <summary>
    /// Adds an in-memory implementation of <see cref="IJourneyStateStorage"/> to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddInMemoryJourneyStateStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<IJourneyStateStorage, InMemoryJourneyStateStorage>();
    }

    /// <summary>
    /// Adds the <see cref="JourneyHelper"/> to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddJourneyHelper(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton<JourneyHelper>();
    }

    /// <summary>
    /// Adds testing services for GovUk.Questions.Mvc to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddJourneyTestingServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddInMemoryJourneyStateStorage();
        services.AddJourneyHelper();

        return services;
    }
}
