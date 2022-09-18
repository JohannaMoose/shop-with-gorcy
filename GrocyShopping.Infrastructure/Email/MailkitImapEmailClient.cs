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
    private ImapClient _client;

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
         SetupClient();
         return await GetAllEmailsFromFolder(_client.Inbox, sender);
    }

    public Task<IReadOnlyList<Email>> GetAllEmailsFromArchiveSentBy(string sender)
    {
        SetupClient();
        var archiveFolder = _client.GetFolder(SpecialFolder.Archive);
        return GetAllEmailsFromFolder(archiveFolder, sender);
    }

    private async Task<IReadOnlyList<Email>> GetAllEmailsFromFolder(IMailFolder folder, string sender)
    {
        _client.Inbox.Open(FolderAccess.ReadOnly);
        var foundUids = await folder.SearchAsync(SearchQuery.FromContains(sender));
        var foundEmails = new List<Email>();
        foreach (var uid in foundUids)
        {
            var message = await _client.Inbox.GetMessageAsync(uid);
            var email = new Email(message.From[0].Name, message.Subject, message.HtmlBody,
                message.Date.DateTime);
            foundEmails.Add(email);
        }

        return foundEmails;
    }

    private void SetupClient()
    {
        _client = new ImapClient();
        var socketOptions = SecureSocketOptions.None;
        if (_useSsl)
            socketOptions = SecureSocketOptions.SslOnConnect;

        _client.Connect(_server, _port, socketOptions);
        _client.Authenticate(_username, _password);
    }

  
}