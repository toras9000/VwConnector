namespace VwConnector.Tests;

[TestClass]
public class InternalExtensionsTests
{
    [TestMethod]
    public void TakeSkipToken()
    {
        var token = "abc@def@ghi".AsSpan().TakeSkipToken('@', out var next);
        token.ToString().Should().Be("abc");
        next.ToString().Should().Be("def@ghi");
    }

    [TestMethod]
    public void DecodeBase64()
    {
        var source = Random.Shared.GetBytes(32);

        Convert.ToBase64String(source).AsSpan().DecodeBase64().Should().Equal(source);
    }

    [TestMethod]
    public void EncodeBase64()
    {
        var source = Random.Shared.GetBytes(32);

        source.EncodeBase64().Should().Be(Convert.ToBase64String(source));
    }

    [TestMethod]
    public void TryParseNumber()
    {
        "123".AsSpan().TryParseNumber<int>().Should().Be(123);
        "abc".AsSpan().TryParseNumber<int>().Should().BeNull();
    }
}
