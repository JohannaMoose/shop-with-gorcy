namespace Grocy.ManageAPI;

public class AppSettings
{
    public static string GrocyInstanceUrl => Environment.GetEnvironmentVariable("GROCY_INSTANCE") ??
                                             throw new ApplicationException("No Grocy instance proviced, it is requrired");

    public static string GrocyApiKey => Environment.GetEnvironmentVariable("GROCY-API-KEY") ??
                                 throw new ApplicationException("No Grocy API key provided, it is required");
}