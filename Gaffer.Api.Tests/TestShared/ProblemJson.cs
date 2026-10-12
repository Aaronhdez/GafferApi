using AwesomeAssertions;
using Newtonsoft.Json.Linq;

namespace Gaffer.Api.Tests.TestShared;

/// <summary>
/// TECHDEBT-006 step 02 (Q10 "RFC 7807"): the error body read as raw, case-sensitive JSON. Standard members in
/// camelCase (kept with <c>null</c>, Q12 a)), <c>errorTranslationKey</c> and <c>errors</c> at the root, no
/// <c>extensions</c> object, and <c>errors</c> keys in camelCase per segment (Q11 a)).
/// </summary>
public static class ProblemJson
{
    public const string ErrorTranslationKey = "errorTranslationKey";
    public const string Errors = "errors";

    private static readonly string[] StandardMembers = ["type", "title", "status", "detail", "instance"];
    private static readonly string[] AllowedMembers = [.. StandardMembers, ErrorTranslationKey, Errors];

    public static async Task<JObject> Read(HttpResponseMessage response) =>
        Parse(await response.Content.ReadAsStringAsync());

    public static JObject Parse(string raw)
    {
        var body = JObject.Parse(raw);
        var members = body.Properties().Select(property => property.Name).ToList();
        members.Should().Contain(StandardMembers, raw);
        members.Should().BeSubsetOf(AllowedMembers, raw);
        if (body[Errors] is JObject errors)
        {
            errors.Properties().Select(property => property.Name).Should()
                .OnlyContain(key => key.Split('.').All(segment => segment.Length > 0 && !char.IsUpper(segment[0])), raw);
        }

        return body;
    }

    public static string? TranslationKeyOf(JObject body) => body[ErrorTranslationKey]?.Value<string>();

    public static JObject ErrorsOf(JObject body) => (JObject)body[Errors]!;
}
