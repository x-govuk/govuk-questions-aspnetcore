using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using NUlid;

namespace GovUk.Questions.Mvc;

/// <summary>
/// The unique ID of a journey instance.
/// </summary>
public sealed class JourneyInstanceId : IEquatable<JourneyInstanceId>, IParsable<JourneyInstanceId>
{
    private const string UriScheme = "fdc:x-govuk.org:questions";

    /// <summary>
    /// The route values key for an instance's key.
    /// </summary>
    public const string KeyRouteValueName = "_jid";

    /// <summary>
    /// Creates a new <see cref="JourneyInstanceId"/>.
    /// </summary>
    /// <param name="journeyName">The name of the journey.</param>
    /// <param name="routeValues">The route values for this instance.</param>
    public JourneyInstanceId(string journeyName, RouteValueDictionary routeValues)
    {
        ArgumentNullException.ThrowIfNull(journeyName);
        ArgumentNullException.ThrowIfNull(routeValues);

        if (!routeValues.ContainsKey(KeyRouteValueName))
        {
            throw new ArgumentException($"Route values is missing an entry for '{KeyRouteValueName}'.", nameof(routeValues));
        }

        // Copy routeValues into a new dictionary to ensure they cannot be modified.
        RouteValues = new ReadOnlyDictionary<string, object?>(routeValues);

        JourneyName = journeyName;
    }

    /// <summary>
    /// Gets the name of the journey.
    /// </summary>
    public string JourneyName { get; }

    /// <summary>
    /// Gets the route values for this instance.
    /// </summary>
    public IReadOnlyDictionary<string, object?> RouteValues { get; }

    /// <summary>
    /// Gets the key for this instance.
    /// </summary>
    public Ulid Key => Ulid.Parse(RouteValues[KeyRouteValueName]!.ToString()!);

    internal static StringComparer JourneyNameComparer { get; } = StringComparer.Ordinal;

    /// <summary>
    /// Parses a string into a <see cref="JourneyInstanceId"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <returns>The <see cref="JourneyInstanceId"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="s"/> is not in the correct format.</exception>
    public static JourneyInstanceId Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        if (!TryParse(s, out var result))
        {
            throw new FormatException($"The input string '{s}' was not in the correct format.");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a string into a <see cref="JourneyInstanceId"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">On return, contains the result of successfully parsing <paramref name="s"/> or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> was successfully parsed; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        [MaybeNullWhen(false)] out JourneyInstanceId result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        if (!Uri.TryCreate(s, UriKind.Absolute, out var uri))
        {
            result = null;
            return false;
        }

        if (!string.Equals(uri.Scheme, UriScheme, StringComparison.Ordinal))
        {
            result = null;
            return false;
        }

        var journeyName = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        var queryString = QueryHelpers.ParseQuery(uri.Query);
        var routeValues = new RouteValueDictionary();
        foreach (var (key, value) in queryString)
        {
            routeValues.Add(key, value);
        }

        if (!routeValues.ContainsKey(KeyRouteValueName))
        {
            result = null;
            return false;
        }

        result = new JourneyInstanceId(journeyName, routeValues);
        return true;
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(JourneyInstanceId? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return JourneyNameComparer.Equals(JourneyName, other.JourneyName) && RouteValues.Equals(other.RouteValues);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is JourneyInstanceId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(JourneyName, RouteValues);

    /// <inheritdoc/>
    public override string ToString()
    {
        var qs = RouteValues.Aggregate(new QueryString(), (q, kvp) => q.Add(kvp.Key, kvp.Value!.ToString()!));
        return $"{UriScheme}/{Uri.EscapeDataString(JourneyName)}{qs.ToUriComponent()}";
    }

    static JourneyInstanceId IParsable<JourneyInstanceId>.Parse(string s, IFormatProvider? provider) =>
        Parse(s);

    static bool IParsable<JourneyInstanceId>.TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out JourneyInstanceId result) =>
        TryParse(s, out result);
}
