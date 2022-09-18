namespace Grocy.RestAPI.Models;

public record Product(string Id, string Name, string Qa_Id_Stock, string Qu_Id_Purchase, IDictionary<string, string> UserFields): Entity(Id);