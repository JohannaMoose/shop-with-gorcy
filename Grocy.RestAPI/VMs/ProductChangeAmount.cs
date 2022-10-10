namespace Grocy.RestAPI.VMs;

internal record ProductChangeAmount(double Amount, string TransactionType, double Price, DateTime? BestBeforeDate, int? LocationId, int? ShoppingLocationId, int? StockLabelType, string? Note);

public enum StockTransactionType
{
    Purchase,
    Consume,
    InventoryCorrection,
    ProductOpened
}

public static class StockTransactionTypeHelp {
    public static string ToApiString(this StockTransactionType type)
    {
        return type switch
        {
            StockTransactionType.Purchase => "purchase",
            StockTransactionType.Consume => "consume",
            StockTransactionType.InventoryCorrection => "inventory-correction",
            StockTransactionType.ProductOpened => "product-opened",
            _ => throw new NotImplementedException()
        };
    }
}