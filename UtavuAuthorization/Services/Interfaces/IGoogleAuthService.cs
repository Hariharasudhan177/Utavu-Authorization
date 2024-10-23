using Google.Apis.Auth;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload> VerifyIdTokenAsync(string idToken);
}