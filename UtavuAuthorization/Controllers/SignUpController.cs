using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

[ApiController]
[Route("auth/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;

    public SignUpController(IGoogleAuthService googleAuthService, IJwtService jwtService, IUserService userService)
    {
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _userService = userService;
    }

    [HttpPost(Name = "SignUp")]
    public async Task<ActionResult<bool>> Post([FromBody, BindRequired] SignUpRequest request)
    {
        try
        {
            var payload = await _googleAuthService.VerifyIdTokenAsync(request.idToken);

            var existingUser = await _userService.GetUserByEmailAsync(payload.Email);
            if (existingUser == null)
            {
                var jwtToken = _jwtService.GenerateJwtToken(payload.Email);
                await _userService.CreateUserAsync(payload, jwtToken);
            }

            var response = new
            {
                Email = payload.Email,
                Token = _jwtService.GenerateJwtToken(payload.Email)
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

    public class SignUpRequest
    {
        [Required]
        public string idToken { get; set; }
    }
}
