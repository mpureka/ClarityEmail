using System.Diagnostics.CodeAnalysis;
using MailKit.Net.Smtp;
using MimeKit;

class EmailHandler
{
    public required MailboxAddress fromAddress { get; set; }
    public required string logFilePath { get; set; }
    public required string server { get; set; }
    public required ushort serverPort { get; set; }

    #region Constructor
    [SetsRequiredMembers]
    public EmailHandler(
        string _fromAddress,
        string _fromName,
        string _logpath,
        string _server,
        ushort _serverPort
    )
    {
        this.fromAddress = new MailboxAddress(_fromName, _fromAddress);
        this.logFilePath = _logpath;
        this.server = _server;
        this.serverPort = _serverPort;
    }
    #endregion

    #region Public Methods
    public MimeMessage BuildEmail(
        string _recipientName,
        string _recipientAddress,
        string _subject,
        string _messageBody
    )
    {
        var message = new MimeMessage();
        message.From.Add(fromAddress);
        message.To.Add(new MailboxAddress(_recipientName, _recipientAddress));
        message.Subject = _subject;
        //If time allows, add HTML toggle
        message.Body = new TextPart("plain") { Text = _messageBody };
        return message;
    }

    public async Task<string> SendEmail(MimeMessage _message)
    {
        LogEmailEvent(_message, "Attempting send");
        try
        {
            var client = new SmtpClient();
            client.Connect(server, serverPort, false);
            var response = await client.SendAsync(_message);
            client.Disconnect(true);
            LogEmailEvent(_message, "Successfully sent!");
            return response;
        }
        catch (Exception e)
        {
            LogEmailEvent(_message, "Error Code: " + e.Message);
            return "Email sending failed!";
        }
    }

    public async Task<string> SendEmail(
        MimeMessage _message,
        string _authUser,
        string _authPassword
    )
    {
        LogEmailEvent(_message, "Attempting send");
        try
        {
            var client = new SmtpClient();
            client.Connect(server, serverPort, false);

            client.Authenticate(_authUser, _authPassword);

            var response = await client.SendAsync(_message);
            client.Disconnect(true);
            LogEmailEvent(_message, "Successfully sent!");
            return response;
        }
        catch (Exception e)
        {
            LogEmailEvent(_message, "Error Code: " + e.Message);
            return "Email sending failed!";
        }
    }

    public bool ValidateEmailAddress(string _address)
    {
        var trimmedEmail = _address.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false;
        }
        try
        {
            var addr = new System.Net.Mail.MailAddress(_address);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    public void LogEmailEvent()
    {
        var DestPath = logFilePath;
        string LogEntry = DateTime.Now + ": Values and stuff go here." + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
    }

    public void LogEmailEvent(MimeMessage _message, string _status)
    {
        //var DestPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var DestPath = logFilePath;
        string LogEntry = DateTime.Now + ": Status: " + _status + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": From: " + _message.From + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": To: " + _message.To + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": Subject: " + _message.Subject + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": " + _message.Body + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": --------------------" + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
    }
}
