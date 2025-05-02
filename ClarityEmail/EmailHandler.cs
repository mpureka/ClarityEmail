using System.Diagnostics.CodeAnalysis;
using MailKit.Net.Smtp;
using MimeKit;

class EmailHandler : IEmailHandler
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
        string _messageBody,
        bool _isPlainText
    )
    {
        var message = new MimeMessage();
        message.From.Add(fromAddress);

        message.To.Add(new MailboxAddress(_recipientName, _recipientAddress));
        message.Subject = _subject;
        if (_isPlainText)
        {
            message.Body = new TextPart("plain") { Text = _messageBody };
        }
        else
        {
            message.Body = new TextPart("HTML") { Text = _messageBody };
        }
        return message;
    }

    public async Task<string> SendEmail(MimeMessage _message)
    {
        var RetryTimes = 3;
        var WaitTime = 10000;
        var client = new SmtpClient();
        string response = "";
        LogEmailEvent("Attempting to send.");
        LogEmailEvent("Using Server: " + server + " port: " + serverPort);
        for (int i = 0; i < RetryTimes; i++)
        {
            LogEmailEvent(_message, "Attempt " + (i + 1));
            try
            {
                if (!client.IsConnected)
                {
                    client.Connect(server, serverPort, false);
                }
                response = await client.SendAsync(_message);
                client.DisconnectAsync(true);
                LogEmailEvent(response);
                LogEmailEvent(_message, "Successfully sent!");
                break;
            }
            catch (Exception e)
            {
                LogEmailEvent("Message Failed to Send.");
                LogEmailEvent(_message, "Error Message: " + e.Message);
                await Task.Delay(WaitTime);
                response = "Email Failed to Send.";
            }
        }
        return response;
    }

    public async Task<string> SendEmail(
        MimeMessage _message,
        string _authUser,
        string _authPassword
    )
    {
        var RetryTimes = 3;
        var WaitTime = 500;
        var client = new SmtpClient();
        string response = "";
        LogEmailEvent("Attempting to send.");
        for (int i = 0; i < RetryTimes; i++)
        {
            LogEmailEvent(_message, "Attempt " + (i + 1));
            try
            {
                if (!client.IsConnected)
                {
                    client.Connect(server, serverPort, false);
                }
                if (!client.IsAuthenticated)
                {
                    client.Authenticate(_authUser, _authPassword);
                }
                response = await client.SendAsync(_message);
                client.DisconnectAsync(true);
                LogEmailEvent(response);
                LogEmailEvent(_message, "Successfully sent!");
                break;
            }
            catch (Exception e)
            {
                LogEmailEvent("Message Failed to Send.");
                LogEmailEvent(_message, "Error Message: " + e.Message);
                await Task.Delay(WaitTime);
                response = "Email Failed to Send.";
            }
        }
        return response;
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

    private async Task<string> MailSender(MimeMessage _message)
    {
        var client = new SmtpClient();
        try
        {
            client.Connect(server, serverPort, false);
            client.Connect(server, serverPort, false);
            var response = await client.SendAsync(_message);
            client.Disconnect(true);
            LogEmailEvent(_message, "Successfully sent!");
            return response;
        }
        catch (Exception e)
        {
            LogEmailEvent(_message, "Error Code: " + e.Message);
            return "Email Failed to Send.";
        }
    }

    private void LogEmailEvent(string _data)
    {
        var DestPath = logFilePath;
        string LogEntry = DateTime.Now + ": " + _data + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
    }

    private void LogEmailEvent(MimeMessage _message, string _data)
    {
        //var DestPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var DestPath = logFilePath;
        string LogEntry = DateTime.Now + ": " + _data + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": From: " + _message.From + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": To: " + _message.To + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": Subject: " + _message.Subject + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": " + _message.Body + " " + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + ": ---------End of Message-----------" + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
        LogEntry = DateTime.Now + Environment.NewLine;
        File.AppendAllText(DestPath, LogEntry);
    }
}
