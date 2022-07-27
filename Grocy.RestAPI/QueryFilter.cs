namespace Grocy.RestAPI;

public record QueryFilter(string Field, QueryConditions Condition, string Value);