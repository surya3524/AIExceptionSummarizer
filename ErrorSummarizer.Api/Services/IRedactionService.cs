using System.Security.Claims;

namespace ErrorSummarizer.Api.Services;

public interface IRedactionService
{
    string RedactValue(string? value);
    string RedactKeyValue(string key, string? value);
    IDictionary<string,string> RedactHeaders(IHeaderDictionary headers, IEnumerable<string> allowList);
    IDictionary<string,string> RedactClaims(IEnumerable<Claim> claims, IEnumerable<string> allowList);
    string RedactBody(string body, int maxLength = 2048);
}
