using System.Diagnostics.CodeAnalysis;
using MimeKit;
using System.Text.Json;
using MailKit.Net.Smtp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var app = builder.Build();

//This doesn't currently work, but it would be better to be able to grab the whole config
//var config = builder.Configuration.GetValue<EmailAppConfig>("EmailAppConfig");

//This gets us our Appsettings values
var MailServer = builder.Configuration.GetValue<string>("MailServer");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Right now, this uses static values, but it needs to get them from Appsettings
var myRequestHandler= new RequestHandler("mpureka@gmail.com","Mike P.");
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    IncludeFields=true,
    WriteIndented=true
};

app.MapPut("/Test", async (HttpContext context) => 
{
    var requestBody=await context.Request.ReadFromJsonAsync<EmailInfo.EmailParams>(options);
    if (myRequestHandler.ValidateEmailAddress(requestBody.ToAddress))
    {
        var Email=myRequestHandler.BuildEmail(requestBody.ToName,requestBody.ToAddress,requestBody.Subject, requestBody.MailBody);
        var Result=myRequestHandler.SendEmail(Email);
        return Result;
    }
    else
    {
    return "Invalid Email Address";
    }
});

app.Run();

class RequestHandler
{
public required MailboxAddress fromAddress {get; set;}

#region Constructor
[SetsRequiredMembers]
public RequestHandler(string _fromAddress, string _fromName)
{
    this.fromAddress=new MailboxAddress(_fromName,_fromAddress);
}
#endregion

#region Public Methods
public MimeMessage BuildEmail(string RecipientName, string RecipientAddress, string Subject, string MessageBody)
{
    var message= new MimeMessage();
    message.From.Add(fromAddress);
    message.To.Add(new MailboxAddress(RecipientName,RecipientAddress));
    message.Subject=Subject;
    //If time allows, add HTML toggle
    message.Body = new TextPart("plain") 
    {
        Text=MessageBody
    };
    return message;
}

public string SendEmail (MimeMessage message)
{
    try{
    var _client=new SmtpClient();
    _client.Connect("server",587,false);
    
    //If we need Authentication, authenticate

    var response = _client.Send(message);
    _client.Disconnect(true);
    return response;
    }
    catch
    {
        return "Email sending failed";
    }
}

public bool ValidateEmailAddress(string Address)
{
    return true;
}
#endregion
}
