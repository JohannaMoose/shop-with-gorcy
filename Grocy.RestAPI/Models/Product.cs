namespace Grocy.RestAPI.Models;

public record Product(string Id, string Name, IDictionary<string, string> UserFields): Entity(Id);