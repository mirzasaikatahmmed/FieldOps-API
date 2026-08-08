using FieldOps.BLL.Services;
using FluentAssertions;
using Xunit;

namespace FieldOps.Tests.Unit;

public class AuthTokenHashTests
{
    [Fact]
    public void HashToken_IsDeterministicAndHex()
    {
        var a = AuthService.HashToken("reset-token");
        var b = AuthService.HashToken("reset-token");
        var c = AuthService.HashToken("other");

        a.Should().Be(b);
        a.Should().NotBe(c);
        a.Should().MatchRegex("^[0-9A-F]+$");
        a.Length.Should().Be(64);
    }
}
