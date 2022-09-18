namespace Grocy.RestAPI;

public record QueryFilter(string Field, QueryCondition Condition, string Value);