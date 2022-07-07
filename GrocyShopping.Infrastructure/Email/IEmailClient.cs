namespace GrocyShopping.Infrastructure.Email;

public interface IEmailClient
{
    Task<IReadOnlyList<Email>> GetAllEmailsFrom(string sender);
}