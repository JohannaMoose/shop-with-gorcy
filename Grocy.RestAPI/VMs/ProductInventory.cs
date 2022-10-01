namespace Grocy.RestAPI.VMs;

internal record ProductInventory(double NewAmount, DateTime BestBeforeDate = default, int ShoppingLocationId = default, int LocationId = default, double Price = default, int StockLabelType = default, string? Note = default);