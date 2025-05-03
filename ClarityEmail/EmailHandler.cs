using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

[method: SetsRequiredMembers]
class EmailHandler(
    string _fromAddress,
    string _fromName,
    string _logpath,
    string _server,
    ushort _serverPort
) : IEmailHandler
{
    public required MailboxAddress FromAddress { get; set; } =
        new MailboxAddress(_fromName, _fromAddress);
    public required string LogFilePath { get; set; } = _logpath;
    public required string Server { get; set; } = _server;
    public required ushort ServerPort { get; set; } = _serverPort;

    #region Public Methods
    /// <summary>
    /// Takes a collection of necessary info and builds a Mime Message for sending
    /// </summary>
    /// <param name="_recipientName"></param>
    /// <param name="_recipientAddress"></param>
    /// <param name="_subject"></param>
    /// <param name="_messageBody"></param>
    /// <param name="_isPlainText"></param>
    /// <param name="_attachments">Can be an empty list</param>
    /// <param name="_CC">Can be an empty list</param>
    /// <param name="_BCC">Can be an empty list</param>
    /// <returns>A populated MimeMessage</returns>
    public MimeMessage BuildEmail(
        string _recipientName,
        string _recipientAddress,
        string _subject,
        string _messageBody,
        bool _isPlainText,
        EmailAttachment[] _attachments,
        EmailAddress[] _CC,
        EmailAddress[] _BCC
    )
    {
        //Create new Mimemessage, populate with basic stuff
        var message = new MimeMessage();
        message.From.Add(FromAddress);
        message.To.Add(new MailboxAddress(_recipientName, _recipientAddress));
        message.Subject = _subject;
        //If the plaintext flag is set, build the body as plain text
        if (_isPlainText)
        {
            var messageBuilder = new BodyBuilder { TextBody = _messageBody };
            if (_attachments.Length != 0)
            {
                AddEmailAttachments(messageBuilder, _attachments);
            }
            message.Body = messageBuilder.ToMessageBody();
        }
        //Else, build it as HTML
        else
        {
            var messageBuilder = new BodyBuilder { HtmlBody = _messageBody };
            if (_attachments.Length != 0)
            {
                AddEmailAttachments(messageBuilder, _attachments);
            }
            message.Body = messageBuilder.ToMessageBody();
        }

        //Add CC and BCC if there are any.
        if (_CC.Length != 0)
        {
            message = AddCC(message, _CC);
        }
        if (_BCC.Length != 0)
        {
            message = AddBCC(message, _BCC);
        }
        return message;
    }

    /// <summary>
    /// Sends an already built email without authenticating to the SMTP server
    /// </summary>
    /// <param name="_message"></param>
    /// <returns>The server response</returns>
    public async Task<string> SendEmail(MimeMessage _message)
    {
        var RetryTimes = 3;
        var WaitTime = 10000;
        var client = new SmtpClient();
        string response = "";
        LogEmailEvent("Attempting to send.");
        LogEmailEvent("Using Server: " + Server + " port: " + ServerPort);

        //Three retries on sending
        for (int i = 0; i < RetryTimes; i++)
        {
            LogEmailEvent(_message, "Attempt " + (i + 1));
            //Try sending the mail. IF we succeed, break out of the loop.
            try
            {
                //Need to make sure we don't already have a client connection from a previous attempt
                if (!client.IsConnected)
                {
                    client.Connect(Server, ServerPort, false);
                }
                response = await client.SendAsync(_message);
                client.DisconnectAsync(true);
                LogEmailEvent(response);
                LogEmailEvent(_message, "Successfully sent!");
                break;
            }
            //If we catch an exception, we log some info, and the loop continues.
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

    /// <summary>
    /// Sends an already build Email message using the supplied user and password to authenticate to the server.
    /// </summary>
    /// <param name="_message"></param>
    /// <param name="_authUser"></param>
    /// <param name="_authPassword"></param>
    /// <returns>Server response</returns>
    public async Task<string> SendEmail(
        MimeMessage _message,
        string _authUser,
        string _authPassword
    )
    {
        var RetryTimes = 3;
        var WaitTime = 10000;
        var client = new SmtpClient();
        string response = "";
        LogEmailEvent("Attempting to send.");
        LogEmailEvent("Using Server: " + Server + " port: " + ServerPort);
        //Three retries on sending
        for (int i = 0; i < RetryTimes; i++)
        {
            LogEmailEvent(_message, "Attempt " + (i + 1));
            //Try sending the mail. IF we succeed, break out of the loop.
            try
            {
                //Need to make sure we don't already have a client connection or authentication from a previous attempt
                if (!client.IsConnected)
                {
                    client.Connect(Server, ServerPort, false);
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
            //If we catch an exception, we log some info, and the loop continues.
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

    /// <summary>
    /// Takes an email address and determines if it's a valid email address format
    /// </summary>
    /// <param name="_address"></param>
    /// <returns>boolean</returns>
    public bool ValidateEmailAddress(string _address)
    {
        var trimmedEmail = _address.Trim();

        //Domains can't end in .
        if (trimmedEmail.EndsWith("."))
        {
            return false;
        }
        //Try converting it to System.Net.Mail.MailAddres
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

    #region Private Methods

    /// <summary>
    /// Logs a message with a datestamp and nothing else
    /// </summary>
    /// <param name="_data"></param>
    private void LogEmailEvent(string _data)
    {
        string LogEntry = DateTime.Now + ": " + _data + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
    }

    /// <summary>
    /// Logs a message, but also everything about the Email
    /// </summary>
    /// <param name="_message"></param>
    /// <param name="_data"></param>
    private void LogEmailEvent(MimeMessage _message, string _data)
    {
        string LogEntry = DateTime.Now + ": " + _data + " " + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
        LogEntry = DateTime.Now + ": From: " + _message.From + " " + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
        LogEntry = DateTime.Now + ": To: " + _message.To + " " + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
        LogEntry = DateTime.Now + ": Subject: " + _message.Subject + " " + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
        //Need to make sure we use the correct message body
        //We can't log the entire body or we get the attachments
        if (_message.TextBody == null)
        {
            LogEntry =
                DateTime.Now + ": " + _message.HtmlBody.ToString() + " " + Environment.NewLine;
        }
        else
        {
            LogEntry = DateTime.Now + ": " + _message.TextBody + " " + Environment.NewLine;
        }
        File.AppendAllText(LogFilePath, LogEntry);
        LogEntry = DateTime.Now + ": ---------End of Message-----------" + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
        LogEntry = DateTime.Now + Environment.NewLine;
        File.AppendAllText(LogFilePath, LogEntry);
    }

    /// <summary>
    /// Loops through a list of attachments and adds them to the Bodybuilder.
    /// </summary>
    /// <param name="_messageBuilder"></param>
    /// <param name="_attachments"></param>
    /// <returns>Bodybuilder with added attachments</returns>
    private static BodyBuilder AddEmailAttachments(
        BodyBuilder _messageBuilder,
        EmailAttachment[] _attachments
    )
    {
        foreach (EmailAttachment attachment in _attachments)
        {
            _messageBuilder.Attachments.Add(attachment.Filename, attachment.Body);
        }
        return _messageBuilder;
    }

    /// <summary>
    /// Loops through a list of CC addresses and adds them to the message
    /// </summary>
    /// <param name="_message"></param>
    /// <param name="_CC"></param>
    /// <returns>Message with CCs added</returns>
    private static MimeMessage AddCC(MimeMessage _message, EmailAddress[] _CC)
    {
        foreach (EmailAddress address in _CC)
        {
            _message.Cc.Add(new MailboxAddress(address.Name, address.Address));
        }
        return _message;
    }

    /// <summary>
    /// Loops through a list of BCC addresses and adds them to the message
    /// </summary>
    /// <param name="_message"></param>
    /// <param name="_BCC"></param>
    /// <returns>Message with BCCs added</returns>
    private static MimeMessage AddBCC(MimeMessage _message, EmailAddress[] _BCC)
    {
        foreach (EmailAddress address in _BCC)
        {
            _message.Bcc.Add(new MailboxAddress(address.Name, address.Address));
        }
        return _message;
    }
}
    #endregion
