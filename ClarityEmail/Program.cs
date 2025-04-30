using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//Load the config from appsettings
var config = builder.Configuration.GetRequiredSection("EmailAppConfig").Get<EmailAppConfig>();

//Get Appsettings values
var MailServer = config.Server;
var FromAddress = config.FromAddress;
var FromName = config.FromName;
var ServerPort = config.Port;
var LogFile = config.LogFile;
var AuthUsername = string.IsNullOrEmpty(config.AuthUsername) ? string.Empty : config.AuthUsername;
var AuthPassword = string.IsNullOrEmpty(config.AuthPassword) ? string.Empty : config.AuthPassword;

//Validate Config values
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

if (ServerPort == 0)
{
    throw new ArgumentNullException("Port");
}

var myEmailHandler = new EmailHandler(FromAddress, FromName, LogFile);
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    IncludeFields = true,
    WriteIndented = true,
};

app.MapPut(
    "/SendEmail",
    async Task<Results<Ok<string>, BadRequest<string>>> (HttpContext context) =>
    {
        //Read the request body
        var requestBody = await context.Request.ReadFromJsonAsync<EmailInfo.EmailParams>(options);
        string Result;

        //Validate the email address
        if (myEmailHandler.ValidateEmailAddress(requestBody.ToAddress))
        {
            //Build the Email
            var Email = myEmailHandler.BuildEmail(
                requestBody.ToName,
                requestBody.ToAddress,
                requestBody.Subject,
                requestBody.MailBody
            );

            //IF we have authentication info, send the email with SMTP authentication
            if (!AuthUsername.Equals(string.Empty) & !AuthPassword.Equals(string.Empty))
            {
                Result = await myEmailHandler.SendEmail(
                    Email,
                    MailServer,
                    ServerPort,
                    AuthUsername,
                    AuthPassword
                );
            }
            //Otherwise, just send it unsecured.
            else
            {
                Result = await myEmailHandler.SendEmail(Email, MailServer, ServerPort);
            }

            //If sending the Email failed...
            if (Result == "Email sending failed!")
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
    }
);

app.Run();
