using Microsoft.AspNetCore.Routing;

namespace GovUk.Questions.AspNetCore.Tests;

public class JourneyInstanceIdTests
{
    [Fact]
    public void Ctor_WithMissingKeyRouteValue_ThrowsArgumentException()
    {
        // Arrange
        var journeyName = "test-journey";
        var routeValues = new RouteValueDictionary { { "some-other-id", "42" } };

        // Act
        var ex = Record.Exception(() => new JourneyInstanceId(journeyName, routeValues));

        // Assert
        var argumentException = Assert.IsType<ArgumentException>(ex);
        Assert.Equal("routeValues", argumentException.ParamName);
    }

    [Fact]
    public void GetKey_ReturnsExpectedResult()
    {
        // Arrange
        var journeyName = "test-journey";
        var guid = Guid.NewGuid().ToString();
        var routeValues = new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, guid } };
        var journeyInstanceId = new JourneyInstanceId(journeyName, routeValues);

        // Act
        var key = journeyInstanceId.Key;

        // Assert
        Assert.Equal(guid, key);
    }

    [Fact]
    public void ToString_ReturnsExpectedResult()
    {
        // Arrange
        var journeyName = "test-journey";
        var key = UUID.New().ToUrlSafeString();
        var routeValues = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" },
            { "bar", "b&az" }
        };

        var journeyInstanceId = new JourneyInstanceId(journeyName, routeValues);

        // Act
        var str = journeyInstanceId.ToString();

        // Assert
        Assert.Equal($"fdc:x-govuk.org:questions/{journeyName}?bar=b%26az&foo=42&_jid={key.ToLowerInvariant()}", str);
    }

    [Fact]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var journeyName = "test-journey";
        var key = UUID.New().ToUrlSafeString();
        var routeValues1 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" }
        };
        var routeValues2 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" }
        };

        var journeyInstanceId1 = new JourneyInstanceId(journeyName, routeValues1);
        var journeyInstanceId2 = new JourneyInstanceId(journeyName, routeValues2);

        // Act
        var areEqual = journeyInstanceId1.Equals(journeyInstanceId2);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_WithDifferentCasedJourneyNamesAndKeys_ReturnsTrue()
    {
        // Arrange
        var journeyName1 = "test-journey";
        var journeyName2 = "TEST-JOURNEY";
        var key = UUID.New().ToUrlSafeString();
        var routeValues1 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" }
        };
        var routeValues2 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName.ToUpper(), key },
            { "FOO", "42" }
        };

        var journeyInstanceId1 = new JourneyInstanceId(journeyName1, routeValues1);
        var journeyInstanceId2 = new JourneyInstanceId(journeyName2, routeValues2);

        // Act
        var areEqual = journeyInstanceId1.Equals(journeyInstanceId2);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var journeyName = "test-journey";
        var key1 = UUID.New().ToUrlSafeString();
        var key2 = UUID.New().ToUrlSafeString();
        var routeValues1 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key1 },
            { "foo", "42" }
        };
        var routeValues2 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key2 },
            { "foo", "42" }
        };

        var journeyInstanceId1 = new JourneyInstanceId(journeyName, routeValues1);
        var journeyInstanceId2 = new JourneyInstanceId(journeyName, routeValues2);

        // Act
        var areEqual = journeyInstanceId1.Equals(journeyInstanceId2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void GetHashCode_WithSameRouteValues_ReturnsSameHashCode()
    {
        // Arrange
        var journeyName = "test-journey";
        var key = UUID.New().ToUrlSafeString();
        var routeValues1 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" }
        };
        var routeValues2 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" }
        };

        var journeyInstanceId1 = new JourneyInstanceId(journeyName, routeValues1);
        var journeyInstanceId2 = new JourneyInstanceId(journeyName, routeValues2);

        // Act
        var hashCode1 = journeyInstanceId1.GetHashCode();
        var hashCode2 = journeyInstanceId2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentCasedRouteValueKeys_ReturnsSameHashCode()
    {
        // Arrange
        var journeyName1 = "test-journey";
        var journeyName2 = "TEST-JOURNEY";
        var key = UUID.New().ToUrlSafeString();
        var routeValues1 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key },
            { "foo", "42" }
        };
        var routeValues2 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName.ToUpper(), key },
            { "FOO", "42" }
        };

        var journeyInstanceId1 = new JourneyInstanceId(journeyName1, routeValues1);
        var journeyInstanceId2 = new JourneyInstanceId(journeyName2, routeValues2);

        // Act
        var hashCode1 = journeyInstanceId1.GetHashCode();
        var hashCode2 = journeyInstanceId2.GetHashCode();

        // Assert - These instances are equal per Equals_WithDifferentCasedJourneyNamesAndKeys_ReturnsTrue test
        // so their hash codes must also be equal
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_WithDifferentRouteValues_ReturnsDifferentHashCode()
    {
        // Arrange
        var journeyName = "test-journey";
        var key1 = UUID.New().ToUrlSafeString();
        var key2 = UUID.New().ToUrlSafeString();
        var routeValues1 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key1 },
            { "foo", "42" }
        };
        var routeValues2 = new RouteValueDictionary
        {
            { JourneyInstanceId.KeyRouteValueName, key2 },
            { "foo", "42" }
        };

        var journeyInstanceId1 = new JourneyInstanceId(journeyName, routeValues1);
        var journeyInstanceId2 = new JourneyInstanceId(journeyName, routeValues2);

        // Act
        var hashCode1 = journeyInstanceId1.GetHashCode();
        var hashCode2 = journeyInstanceId2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void TryParse_ValidString_ReturnsExpectedJourneyInstanceId()
    {
        // Arrange
        var journeyName = "test-journey";
        var key = UUID.New().ToUrlSafeString();
        var input = $"fdc:x-govuk.org:questions/{journeyName}?foo=42&bar=b%26az&_jid={key}";

        // Act
        var success = JourneyInstanceId.TryParse(input, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(journeyName, result.JourneyName);
        Assert.Equal(key, result.Key);
        Assert.Equal("42", result.RouteValues["foo"].ToString());
        Assert.Equal("b&az", result.RouteValues["bar"].ToString());
    }

    [Fact]
    public void AppendKeyToUrl_WithExistingQueryParameters_AppendsKeyCorrectly()
    {
        // Arrange
        var journeyName = "test-journey";
        var key = UUID.New().ToUrlSafeString();
        var routeValues = new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, key } };
        var journeyInstanceId = new JourneyInstanceId(journeyName, routeValues);
        var url = "/some/path?foo=bar";

        // Act
        var result = journeyInstanceId.AppendKeyToUrl(url);

        // Assert
        Assert.Equal($"/some/path?foo=bar&_jid={key}", result);
    }

    [Fact]
    public void AppendKeyToUrl_WithoutExistingQueryParameters_AppendsKeyCorrectly()
    {
        // Arrange
        var journeyName = "test-journey";
        var key = UUID.New().ToUrlSafeString();
        var routeValues = new RouteValueDictionary { { JourneyInstanceId.KeyRouteValueName, key } };
        var journeyInstanceId = new JourneyInstanceId(journeyName, routeValues);
        var url = "/some/path";

        // Act
        var result = journeyInstanceId.AppendKeyToUrl(url);

        // Assert
        Assert.Equal($"/some/path?_jid={key}", result);
    }
}
