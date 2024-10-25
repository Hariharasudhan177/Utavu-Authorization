using Google.Apis.Auth;

public interface IUserService
{
    Task<User> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(GoogleJsonWebSignature.Payload payload, string jwtToken);
}