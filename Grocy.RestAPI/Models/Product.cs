namespace Grocy.RestAPI.Models;

public record Product(int Id, string Name, int Qu_id_stock, int Qu_Id_Purchase, IDictionary<string, string> UserFields): Entity(Id);