namespace GrocyShopping.Console;

public class ConsoleProgramHelper
{
    public static bool AcceptChoice(string info)
    {
        System.Console.Write(info);
        var userAnser = System.Console.ReadLine()?.ToLower().Trim();

        var accept = string.IsNullOrWhiteSpace(userAnser) || userAnser == "y";
        return accept;
    }

}