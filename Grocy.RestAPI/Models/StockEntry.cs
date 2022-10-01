namespace Grocy.RestAPI.Models;

public record StockEntry(int Id, int ProductId, int LocationId, int ShoppingLocationId, double Amount, DateTime BestBeforeDate, DateTime PurchasedDate, string StockId, double Price, int Open, DateTime OpenedDate, string Note, DateTime RowCreatedTimestamp);