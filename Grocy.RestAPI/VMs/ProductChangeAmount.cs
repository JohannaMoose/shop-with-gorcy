namespace Grocy.RestAPI.VMs;

internal record ProductChangeAmount(double amount, string transaction_type, double price, DateTime? best_before_date, int? location_id, int? shopping_location_id, int? stock_label_type, string? note);

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