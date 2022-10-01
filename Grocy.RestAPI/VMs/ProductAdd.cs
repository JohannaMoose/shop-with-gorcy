namespace Grocy.RestAPI.VMs;

public record ProductAdd(string Name, int QuIdStock, int QuIdPurchase, double QuFactorPurchaseToStock, int LocationId);