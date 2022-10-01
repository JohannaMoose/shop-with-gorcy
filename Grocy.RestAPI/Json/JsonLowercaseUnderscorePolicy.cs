using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grocy.RestAPI.Json;

public class JsonLowercaseUnderscorePolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

        var result = r.Replace(name, "_").ToLower();
        return result;
    }
}