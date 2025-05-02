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

//Setup Email handler and JsonSerializer Options.
var myEmailHandler = new EmailHandler(FromAddress, FromName, LogFile, MailServer, ServerPort);
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    IncludeFields = true,
    WriteIndented = true,
};

app.MapPut(
    "/SendEmail",
    async Task<Results<Ok<string>, BadRequest<string>, InternalServerError<string>>> (
        HttpContext context
    ) =>
    {
        //Read the request body
        var requestBody = await context.Request.ReadFromJsonAsync<EmailInfo.EmailParams>(options);

        //Validate the email address
        if (myEmailHandler.ValidateEmailAddress(requestBody.ToAddress))
        {
            try
            {
                //Build the Email
                var Email = myEmailHandler.BuildEmail(
                    requestBody.ToName,
                    requestBody.ToAddress,
                    requestBody.Subject,
                    requestBody.MailBody,
                    requestBody.IsPlainText
                );
                //IF we have authentication info, send the email with SMTP authentication
                if (!AuthUsername.Equals(string.Empty) & !AuthPassword.Equals(string.Empty))
                {
                    myEmailHandler.SendEmail(Email, AuthUsername, AuthPassword);
                }
                //Otherwise, just send it unsecured.
                else
                {
                    myEmailHandler.SendEmail(Email);
                }

                return TypedResults.Ok("Email Accepted, see logs for details.");
            }
            catch
            {
                return TypedResults.InternalServerError("There was a problem with the request");
            }
        }
        else
        {
            return TypedResults.BadRequest(requestBody.ToAddress + " is an invalid Email Address");
        }
    }
);

app.MapPut(
    "/TestEmail",
    async Task<Results<Ok<string>, BadRequest<string>, InternalServerError<string>>> (
        HttpContext context
    ) =>
    {
        //Get the request body
        var requestBody = await context.Request.ReadFromJsonAsync<EmailInfo.TestEmailParams>(
            options
        );
        string TestRecipientName = requestBody.ToAddress.Split('@')[0];
        //Validate the email address
        if (myEmailHandler.ValidateEmailAddress(requestBody.ToAddress))
        {
            try
            {
                //Build the Email
                var Email = myEmailHandler.BuildEmail(
                    TestRecipientName,
                    requestBody.ToAddress,
                    "This is only a test.",
                    "Please disregard this test mail.",
                    false
                );
                //IF we have authentication info, send the email with SMTP authentication
                if (!AuthUsername.Equals(string.Empty) & !AuthPassword.Equals(string.Empty))
                {
                    myEmailHandler.SendEmail(Email, AuthUsername, AuthPassword);
                }
                //Otherwise, just send it unsecured.
                else
                {
                    myEmailHandler.SendEmail(Email);
                }
                return TypedResults.Ok("Email Accepted, see logs for details.");
            }
            catch
            {
                return TypedResults.InternalServerError("There was a problem with the request");
            }
        }
        else
        {
            return TypedResults.BadRequest(requestBody.ToAddress + " is an invalid Email Address");
        }
    }
);
app.Run();
