# ClarityEmail
Email Assignment from Clarity Ventures

# Setup Instructions
Configure settings in appsettings.json, modifying the EmailAppConfig section.
Fields are as follows:

    FromName -- The name that will be displayed on Emails sent by the application
    FromAddress -- The From address of mails sent by the application
    Server -- The hostname of the SMTP server to use when sending emails
    Port -- The port to connect to the SMTP server on
    AuthUsername and AuthPassword -- The username and password to use when connecting to the SMTP server; If the server is unsecured, omit these fields.
    LogFile -- The path and file to use for logging, e.g. C:\\Logs\\EmailLog.txt

Build and run as normal.

# Usage
The application accepts JSON messages PUT to:
/TestEmail
and
/SendEmail

## TestEmail
/TestEmail accepts simple JSON with only an Email address:
{
    "ToAddress":"mpureka@gmail.com"
}

It will send a test email to that email address.

## SendEmail
/SendEmail requires a more complex json, with the following required fields:

    ToName -- The name of the recipient
    ToAddress -- The email address of the recipient
    Subject -- The subject of the mail
    MailBody -- The mail body. Accepts HTML.

Additionally, the following values are optional:

    IsPlainText: A Boolen value. If set to True, the message will be sent as plain text rather than HTML. Defaults to false if omitted.
    CC -- A list of Name/Address field pairs to CC on the message e.g. "CC":[{"Name":"Mimi","Address":"mimi@gmail.com"}]
    BCC -- A list of Name/Address field pairs to BCC on the message e.g. "BCC":[{"Name":"James","Address":"TheJames@gmail.com"}]
    Attachments -- A list of Filename/Body pairs to add as attachments.  The Body value must be a UUEncoded string containing the data in the file to send.

