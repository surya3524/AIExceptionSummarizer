using System.Security.Claims;
using System.Text.RegularExpressions;

namespace ErrorSummarizer.Api.Services;

public class BasicRedactionService : IRedactionService
{
    private static readonly Regex EmailRegex = new("[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}", RegexOptions.Compiled);
    private static readonly Regex GuidRegex = new("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new("\\b(?:\\d[ -]*?){13,16}\\b", RegexOptions.Compiled);
    private static readonly Regex JwtRegex = new("eyJ[0-9A-Za-z_-]+\\.[0-9A-Za-z_-]+\\.[0-9A-Za-z_-]+", RegexOptions.Compiled);

    public string RedactValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var v = value;
        v = EmailRegex.Replace(v, m => Hash(m.Value));
        v = GuidRegex.Replace(v, m => Hash(m.Value));
        v = CreditCardRegex.Replace(v, _ => "<redacted:card>");
        v = JwtRegex.Replace(v, _ => "<redacted:jwt>");
        if (v.Length > 256) v = v.Substring(0, 256) + "…";
        return v;
    }

    public string RedactKeyValue(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (IsSensitiveKey(key)) return "<redacted>";
        return RedactValue(value);
    }

    public IDictionary<string, string> RedactHeaders(IHeaderDictionary headers, IEnumerable<string> allowList)
    {
        var dict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in allowList)
        {
            if (headers.TryGetValue(k, out var v))
            {
                dict[k] = RedactKeyValue(k, v.ToString());
            }
        }
        return dict;
    }

    public IDictionary<string, string> RedactClaims(IEnumerable<Claim> claims, IEnumerable<string> allowList)
    {
        var dict = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in claims)
        {
            if (allowList.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
            {
                dict[c.Type] = RedactValue(c.Value);
            }
        }
        return dict;
    }

    public string RedactBody(string body, int maxLength = 2048)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        var redacted = RedactValue(body);
        if (redacted.Length > maxLength)
            redacted = redacted.Substring(0, maxLength) + "…";
        return redacted;
    }

    private static bool IsSensitiveKey(string key)
    {
        key = key.ToLowerInvariant();
        return key.Contains("auth") || key.Contains("token") || key.Contains("cookie") || key.Contains("secret") || key.Contains("key");
    }

    private static string Hash(string input)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes.AsSpan(0, 6)); // short hash
    }
}
