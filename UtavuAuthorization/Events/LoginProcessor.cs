using Microsoft.Extensions.Configuration;
using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid; 
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;

public class LoginProcessor: ILoginProcessor
{
    private readonly EventGridPublisherClient _client;

    public LoginProcessor(IConfiguration configuration)
    {
        var eventGridTopicEndpoint = "https://utavuloginevent.uksouth-1.eventgrid.azure.net/api/events";
        var secretClient = new SecretClient(new Uri("https://utavukv.vault.azure.net/"), new DefaultAzureCredential());
        KeyVaultSecret eventGridAccessSecret = secretClient.GetSecret("EventGridAccessKey");
        _client = new EventGridPublisherClient(new Uri(eventGridTopicEndpoint), new AzureKeyCredential(eventGridAccessSecret.Value));
    }

    public void ProcessLogin(string email)
    {
        // Create an event
        var loginEvent = new EventGridEvent(
            eventType: "User.Login",
            subject: $"User {email} logged in",
            dataVersion: "1.0",
            data: new { Email = email });

        _ = _client.SendEventAsync(loginEvent); 
    }
}
