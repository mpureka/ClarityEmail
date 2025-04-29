using System.Diagnostics.CodeAnalysis;
using MimeKit;
using MailKit.Net.Smtp;

class EmailHandler
{
public required MailboxAddress fromAddress {get; set;}

#region Constructor
[SetsRequiredMembers]
public EmailHandler(string _fromAddress, string _fromName)
{
    this.fromAddress=new MailboxAddress(_fromName,_fromAddress);
}
#endregion

#region Public Methods
public MimeMessage BuildEmail(string _recipientName, string _recipientAddress, string _subject, string _messageBody)
{
    var message= new MimeMessage();
    message.From.Add(fromAddress);
    message.To.Add(new MailboxAddress(_recipientName,_recipientAddress));
    message.Subject=_subject;
    //If time allows, add HTML toggle
    message.Body = new TextPart("plain") 
    {
        Text=_messageBody
    };
    return message;
}

public string SendEmail (MimeMessage _message, string _server, ushort _serverPort)
{
    try{
    var _client=new SmtpClient();
    _client.Connect(_server,_serverPort,false);
    
    //If we need Authentication, authenticate

    var response = _client.Send(_message);
    _client.Disconnect(true);
    return response;
    }
    catch
    {
        return "Email sending failed!";
    }
}

public bool ValidateEmailAddress(string _address)
{
   var trimmedEmail = _address.Trim();

    if (trimmedEmail.EndsWith(".")) {
        return false; 
    }
    try {
        var addr = new System.Net.Mail.MailAddress(_address);
        return addr.Address == trimmedEmail;
    }
    catch {
        return false;
    }
}
#endregion
}
