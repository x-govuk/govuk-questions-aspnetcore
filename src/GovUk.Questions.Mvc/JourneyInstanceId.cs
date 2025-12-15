using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using NUlid;

namespace GovUk.Questions.Mvc;

/// <summary>
/// The unique ID of a journey instance.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JourneyInstanceId : IEquatable<JourneyInstanceId>, IParsable<JourneyInstanceId>
{
    private const string UriPrefix = "fdc:x-govuk.org:questions";

    private string? _asString;

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
        RouteValues = new ReadOnlyDictionary<string, object?>(routeValues.ToDictionary(StringComparer.OrdinalIgnoreCase));

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

        var schemeAndPath = uri.GetLeftPart(UriPartial.Path);
        if (!schemeAndPath.StartsWith(UriPrefix, StringComparison.Ordinal))
        {
            result = null;
            return false;
        }

        var journeyName = Uri.UnescapeDataString(schemeAndPath[UriPrefix.Length..].TrimStart('/'));

        var queryString = QueryHelpers.ParseQuery(uri.Query);
        var routeValues = new RouteValueDictionary();
        foreach (var (key, value) in queryString)
        {
            routeValues.Add(key, value);
        }

        if (!routeValues.TryGetValue(KeyRouteValueName, out var keyRouteValue) || !Ulid.TryParse(keyRouteValue?.ToString(), out _))
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

        return ToString().Equals(other.ToString(), StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is JourneyInstanceId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(JourneyName, RouteValues);

    /// <inheritdoc/>
    public override string ToString()
    {
        if (_asString is not null)
        {
            return _asString;
        }

#pragma warning disable CA1308
        // Ensure all the query parameters are in a consistent order and lower-cased for equality comparisons.
        var qs = RouteValues
            .Where(kvp => !kvp.Key.Equals(KeyRouteValueName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(kvp => kvp.Key)
            .Aggregate(new QueryString(), (q, kvp) => q.Add(kvp.Key.ToLowerInvariant(), kvp.Value!.ToString()!));

        // Add the key last, lower-cased
        qs = qs.Add(KeyRouteValueName, Key.ToString().ToLowerInvariant());

        return _asString = $"{UriPrefix}/{Uri.EscapeDataString(JourneyName.ToLowerInvariant())}{qs.ToUriComponent()}";
#pragma warning restore CA1308
    }

    static JourneyInstanceId IParsable<JourneyInstanceId>.Parse(string s, IFormatProvider? provider) =>
        Parse(s);

    static bool IParsable<JourneyInstanceId>.TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out JourneyInstanceId result) =>
        TryParse(s, out result);
}
