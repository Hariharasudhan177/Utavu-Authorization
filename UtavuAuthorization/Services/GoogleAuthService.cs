using Google.Apis.Auth;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _configuration;

    public GoogleAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleJsonWebSignature.Payload> VerifyIdTokenAsync(string idToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = new[] { _configuration["Google:ClientId"] }
        };

        return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }
}