using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;

namespace GrocyShopping.Infrastructure.Email;

public class MailkitImapEmailClient : IEmailClient
{
    private readonly string _server;
    private readonly int _port;
    private readonly bool _useSsl;
    private readonly string _username;
    private readonly string _password;

    public MailkitImapEmailClient(string server, int port, bool useSsl, string username, string password)
    {
        _server = server;
        _port = port;
        _useSsl = useSsl;
        _username = username;
        _password = password;
    }

    public async Task<IReadOnlyList<Email>> GetAllEmailsFrom(string sender)
    {
        using var client = new ImapClient();
        var socketOptions = SecureSocketOptions.None;
        if (_useSsl)
            socketOptions = SecureSocketOptions.SslOnConnect;

        client.Connect(_server, _port, socketOptions);
        client.Authenticate(_username, _password);
        client.Inbox.Open(FolderAccess.ReadOnly);

        var foundUids = await client.Inbox.SearchAsync(SearchQuery.FromContains(sender));

        var foundEmails = new List<Email>();
        foreach (var uid in foundUids)
        {
            var message = await client.Inbox.GetMessageAsync(uid);
            var email = new Email(message.From[0].Name, message.Subject, message.HtmlBody,
                message.Date.DateTime);
            foundEmails.Add(email);
        }

        return foundEmails; 
    }
}