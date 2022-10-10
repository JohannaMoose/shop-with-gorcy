namespace Grocy.RestAPI.Models;

public record QuantityUnitConversion(int Id, int From_qu_id, int To_qu_id, double Factor, int? Product_id, DateTime Row_created_timestamp);