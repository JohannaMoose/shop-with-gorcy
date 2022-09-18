namespace Grocy.RestAPI;

public enum QueryCondition
{
    Equals
}

public static class QueryConditionsToString
{
    public static string AsQueryStringPart(this QueryCondition condition)
    {
        return condition switch
        {
            QueryCondition.Equals => "=",
            _ => throw new NotSupportedException("That query condition is not yet supported")
        };
    }
}