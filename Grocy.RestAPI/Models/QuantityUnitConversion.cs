namespace Grocy.RestAPI.Models;

public record QuantityUnitConversion(string Id, string From_qu_id, string To_qu_id, string Factor, string Product_id, string Row_created_timestamp);