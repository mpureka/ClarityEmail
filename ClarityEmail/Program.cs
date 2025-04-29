using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var app = builder.Build();

//This doesn't currently work, but it would be better to be able to grab the whole config
var config = builder.Configuration.GetRequiredSection("EmailAppConfig").Get<EmailAppConfig>();

//This gets us our Appsettings values
var MailServer = config.Server;
var FromAddress = config.FromAddress;
var FromName = config.FromName;
var ServerPort = config.Port;

if (MailServer.Equals(null))
{
    throw new ArgumentNullException("MailServer");
}

if (FromAddress.Equals(null))
{
    throw new ArgumentNullException("FromAddress");
}

if (FromName.Equals(null))
{
    throw new ArgumentNullException("FromName");
}

if (ServerPort==0)
{
    throw new ArgumentNullException("Port");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Right now, this uses static values, but it needs to get them from Appsettings
var myEmailHandler= new EmailHandler(FromAddress,FromName);
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    IncludeFields=true,
    WriteIndented=true
};

app.MapPut("/SendEmail", async Task<Results<Ok<string>,BadRequest<string>>> (HttpContext context) => 
{
    var requestBody=await context.Request.ReadFromJsonAsync<EmailInfo.EmailParams>(options);
    
    if (myEmailHandler.ValidateEmailAddress(requestBody.ToAddress))
    {
        var Email=myEmailHandler.BuildEmail(requestBody.ToName,requestBody.ToAddress,requestBody.Subject, requestBody.MailBody);
        var Result=myEmailHandler.SendEmail(Email,MailServer, ServerPort);
        if (Result=="Email sending failed!")
        {
            return TypedResults.BadRequest(Result);
        }
        else
        {
            return TypedResults.Ok(Result);
        }
    }
    else
    {
        return TypedResults.BadRequest(requestBody.ToAddress + " is an invalid Email Address");
    }
});

app.Run();
