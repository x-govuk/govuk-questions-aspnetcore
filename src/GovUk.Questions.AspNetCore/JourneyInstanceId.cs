using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GovUk.Questions.AspNetCore.Description;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace GovUk.Questions.AspNetCore;

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
        RouteValues = new ReadOnlyDictionary<string, object>(
            routeValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase));

        JourneyName = journeyName;
    }

    /// <summary>
    /// Gets the name of the journey.
    /// </summary>
    public string JourneyName { get; }

    /// <summary>
    /// Gets the route values for this instance.
    /// </summary>
    public IReadOnlyDictionary<string, object> RouteValues { get; }

    /// <summary>
    /// Gets the key for this instance.
    /// </summary>
    public string Key => RouteValues[KeyRouteValueName].ToString()!;

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

        if (!routeValues.TryGetValue(KeyRouteValueName, out var keyRouteValue) || !UUID.TryFromUrlSafeString(keyRouteValue?.ToString()!, out _))
        {
            result = null;
            return false;
        }

        result = new JourneyInstanceId(journeyName, routeValues);
        return true;
    }

    /// <summary>
    /// Appends the instance key query parameter to the specified URL, if it doesn't already contain it.
    /// </summary>
#pragma warning disable CA1055
    public string EnsureUrlHasKey(string url)
#pragma warning restore CA1055
    {
        ArgumentNullException.ThrowIfNull(url);

        return url.Contains($"{KeyRouteValueName}=", StringComparison.Ordinal) ? url : QueryHelpers.AddQueryString(url, KeyRouteValueName, Key);
    }

    internal static bool TryCreate(JourneyDescriptor journey, RouteValueDictionary routeValues, [NotNullWhen(true)] out JourneyInstanceId? result)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(routeValues);

        if (!routeValues.TryGetValue(KeyRouteValueName, out var keyValue) || !UUID.TryFromUrlSafeString(keyValue?.ToString()!, out _))
        {
            result = null;
            return false;
        }

        var sanitizedRouteValues = new RouteValueDictionary();
        sanitizedRouteValues.Add(KeyRouteValueName, keyValue!.ToString());

        foreach (var key in journey.RouteValueKeys)
        {
            if (!routeValues.TryGetValue(key, out var value))
            {
                result = null;
                return false;
            }

            sanitizedRouteValues.Add(key, value);
        }

        result = new JourneyInstanceId(journey.JourneyName, sanitizedRouteValues);
        return true;
    }

    internal static bool TryCreateNew(JourneyDescriptor journey, RouteValueDictionary routeValues, [NotNullWhen(true)] out JourneyInstanceId? result)
    {
        ArgumentNullException.ThrowIfNull(journey);
        ArgumentNullException.ThrowIfNull(routeValues);

        var instanceKey = UUID.New();

        var sanitizedRouteValues = new RouteValueDictionary();
        sanitizedRouteValues.Add(KeyRouteValueName, instanceKey.ToUrlSafeString());

        foreach (var key in journey.RouteValueKeys)
        {
            if (!routeValues.TryGetValue(key, out var value))
            {
                result = null;
                return false;
            }

            sanitizedRouteValues.Add(key, value);
        }

        result = new JourneyInstanceId(journey.JourneyName, sanitizedRouteValues);
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
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.Ordinal);

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
            .Aggregate(new QueryString(), (q, kvp) => q.Add(kvp.Key.ToLowerInvariant(), kvp.Value.ToString() ?? string.Empty));

        // Add the key last, lower-cased
        qs = qs.Add(KeyRouteValueName, Key.ToLowerInvariant());

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
