namespace GrocyShopping.Infrastructure.Email;

public record Email(string Sender, string Title, string Body, DateTime Date)
{
}