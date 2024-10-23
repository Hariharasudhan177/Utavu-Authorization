using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Google.Apis.Auth;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace UserManagement;

[ApiController]
[Route("auth/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public SignUpController(IConfiguration configuration, AppDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [HttpPost(Name = "SignUp")]
    public async Task<ActionResult<bool>> Post([FromBody, BindRequired] SignUpRequest request)
    {
        try
        {
            var payload = await VerifyIdToken(request.idToken);

            // Check if user already exists
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);
            if (existingUser == null)
            {
                // Save the new user to the database
                var user = new User
                {
                    Email = payload.Email,
                    Name = payload.Name,
                    GoogleId = payload.Subject, // Store the unique Google ID
                    JwtToken = GenerateJwtToken(payload.Email) // Store the generated JWT token
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            var jwtToken = GenerateJwtToken(payload.Email);

            var response = new
            {
                Email = payload.Email,
                Token = jwtToken
            };

            return Ok(response);
        }
        catch (Exception e)
        {

            var errorResponse = new
            {
                Status = 400,
                Errors = e.Message
            };
            return BadRequest(errorResponse);
        }
    }

    private async Task<GoogleJsonWebSignature.Payload> VerifyIdToken(string idToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = new[] { _configuration["Google:ClientId"] }
        };

        return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
    }

    // JWT generation method
    private string GenerateJwtToken(string email)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: new []{new Claim(ClaimTypes.Email, email)},
            expires: DateTime.Now.AddDays(7)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public class SignUpRequest
    {
        [Required]
        public string idToken { get; set; }
    }
}