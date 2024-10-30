using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("auth/[controller]")]
public class SignUpController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly ILoginProcessor _loginProcessor;
    private readonly ILogger<SignUpController> _logger;

    public SignUpController(ILogger<SignUpController> logger, IGoogleAuthService googleAuthService, IJwtService jwtService, IUserService userService, ILoginProcessor loginProcessor)
    {
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _userService = userService;
        _loginProcessor = loginProcessor;
        _logger = logger;
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

            _loginProcessor.ProcessLogin(payload.Email);
            _logger.LogInformation($"login processor");

            var response = new
            {
                Email = "test@gmail.com",
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
