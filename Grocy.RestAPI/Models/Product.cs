namespace Grocy.RestAPI.Models;

public record Product(int Id, string Name, int QuIdStock, int QuIdPurchase, IDictionary<string, string?> Userfields): Entity(Id);