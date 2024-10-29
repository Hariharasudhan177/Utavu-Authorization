using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UserManagement;
using Google.Apis.Auth;

namespace UserManagement.Tests.UnitTests.Controllers
{
    public class SignUpControllerTests
    {
        private readonly Mock<IGoogleAuthService> _mockGoogleAuthService;
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ILoginProcessor> _mockLoginProcessor;
        private readonly SignUpController _controller;

        public SignUpControllerTests()
        {
            _mockGoogleAuthService = new Mock<IGoogleAuthService>();
            _mockJwtService = new Mock<IJwtService>();
            _mockUserService = new Mock<IUserService>();
            _mockLoginProcessor = new Mock<ILoginProcessor>();

            // Initialize the controller with mocked dependencies
            _controller = new SignUpController(_mockGoogleAuthService.Object, _mockJwtService.Object, _mockUserService.Object, _mockLoginProcessor.Object);
        }

        [Fact]
        public async Task Post_ValidRequest_ReturnsOkResult()
        {
            // Arrange
            var idToken = "valid_id_token";
            var request = new SignUpController.SignUpRequest { idToken = idToken };
            
            var mockPayload = new GoogleJsonWebSignature.Payload
            {
                Email = "testuser@example.com",
                Name = "Test User",
                Subject = "google_subject_id"
            };

            _mockGoogleAuthService
                .Setup(service => service.VerifyIdTokenAsync(idToken))
                .ReturnsAsync(mockPayload);

            _mockUserService
                .Setup(service => service.GetUserByEmailAsync(mockPayload.Email))
                .ReturnsAsync((User)null);  // No user exists

            _mockJwtService
                .Setup(service => service.GenerateJwtToken(mockPayload.Email))
                .Returns("mock_jwt_token");

            _mockUserService
                .Setup(service => service.CreateUserAsync(mockPayload, "mock_jwt_token"))
                .ReturnsAsync(new User { Email = mockPayload.Email, GoogleId = mockPayload.Subject });

            // Act
            var result = await _controller.Post(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as dynamic;
            
            Assert.Equal("testuser@example.com", (string)response.Email);
            Assert.Equal("mock_jwt_token", (string)response.Token);
        }

        [Fact]
        public async Task Post_UserAlreadyExists_ReturnsOkResultWithToken()
        {
            // Arrange
            var idToken = "valid_id_token";
            var request = new SignUpController.SignUpRequest { idToken = idToken };
            
            var mockPayload = new GoogleJsonWebSignature.Payload
            {
                Email = "existinguser@example.com",
                Name = "Existing User",
                Subject = "existing_google_id"
            };

            var existingUser = new User
            {
                Email = mockPayload.Email,
                GoogleId = mockPayload.Subject
            };

            _mockGoogleAuthService
                .Setup(service => service.VerifyIdTokenAsync(idToken))
                .ReturnsAsync(mockPayload);

            _mockUserService
                .Setup(service => service.GetUserByEmailAsync(mockPayload.Email))
                .ReturnsAsync(existingUser);  // User exists

            _mockJwtService
                .Setup(service => service.GenerateJwtToken(mockPayload.Email))
                .Returns("existing_user_jwt_token");

            // Act
            var result = await _controller.Post(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as dynamic;
            
            Assert.Equal("existinguser@example.com", (string)response.Email);
            Assert.Equal("existing_user_jwt_token", (string)response.Token);
        }

        [Fact]
        public async Task Post_InvalidIdToken_ReturnsBadRequest()
        {
            // Arrange
            var idToken = "invalid_id_token";
            var request = new SignUpController.SignUpRequest { idToken = idToken };

            _mockGoogleAuthService
                .Setup(service => service.VerifyIdTokenAsync(idToken))
                .ThrowsAsync(new Exception("Invalid ID token"));

            // Act
            var result = await _controller.Post(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errorResponse = badRequestResult.Value as dynamic;
            
            Assert.Equal(400, (int)errorResponse.Status);
            Assert.Equal("Invalid ID token", (string)errorResponse.Errors);
        }
    }
}
