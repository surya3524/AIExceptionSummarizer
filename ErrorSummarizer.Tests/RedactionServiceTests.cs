using ErrorSummarizer.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

public class RedactionServiceTests
{
    private readonly BasicRedactionService _svc = new();

    [Fact]
    public void RedactsEmailAndJwt()
    {
        var input = "Contact me a@b.com token eyJabc.def.ghi";
        var redacted = _svc.RedactValue(input);
        redacted.Should().NotContain("a@b.com").And.Contain("<redacted:jwt>");
    }

    [Fact]
    public void RedactsSensitiveHeader()
    {
        var headers = new HeaderDictionary
        {
            { "Authorization", "Bearer xyz" },
            { "User-Agent", "UA" }
        };
        var result = _svc.RedactHeaders(headers, new[] { "Authorization", "User-Agent" });
        result["Authorization"].Should().Be("<redacted>");
        result["User-Agent"].Should().NotBe("<redacted>");
    }
}
