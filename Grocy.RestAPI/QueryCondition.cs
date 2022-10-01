namespace Grocy.RestAPI;

public enum QueryCondition
{
    Equals,
    Like
}

public static class QueryConditionsToString
{
    public static string AsQueryStringPart(this QueryCondition condition)
    {
        return condition switch
        {
            QueryCondition.Equals => "=",
            QueryCondition.Like => "~",
            _ => throw new NotSupportedException("That query condition is not yet supported")
        };
    }
}