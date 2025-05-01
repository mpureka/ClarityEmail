using MimeKit;

public interface IEmailHandler
{
    MimeMessage BuildEmail(
        string _recipientName,
        string _recipientAddress,
        string _subject,
        string _messageBody,
        bool _isPlainText
    );

    public Task<string> SendEmail(MimeMessage _message);

    public Task<string> SendEmail(MimeMessage _message, string _authUser, string _authPassword);

    public bool ValidateEmailAddress(string _address);
}
