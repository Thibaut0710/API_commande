using Moq;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace API_Commande.Tests
{
    public class AuthCommandeControllerTests
    {
        private readonly AuthCommandeController _controller;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public AuthCommandeControllerTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.SetupGet(x => x["Jwt:Key"]).Returns("dzjao3019%%dza");
            _mockConfiguration.SetupGet(x => x["Jwt:Issuer"]).Returns("Issuer117");
            _mockConfiguration.SetupGet(x => x["Jwt:Audience"]).Returns("Audience117");

            _controller = new AuthCommandeController(_mockConfiguration.Object);
        }

        [Fact]
        public void Login_ValidCredentials_ReturnsOkWithToken()
        {
            var validUser = new UserLogin
            {
                Username = "testuser",
                Password = "testpassword"
            };

            var result = _controller.Login(validUser) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);

            var resultValue = result.Value as IDictionary<string, object>;
            Assert.NotNull(resultValue);
            Assert.True(resultValue.ContainsKey("Token"));
            Assert.NotNull(resultValue["Token"]);
        }

        [Fact]
        public void Login_InvalidCredentials_ReturnsUnauthorized()
        {
            var invalidUser = new UserLogin
            {
                Username = "wronguser",
                Password = "wrongpassword"
            };

            var result = _controller.Login(invalidUser) as UnauthorizedObjectResult;

            Assert.NotNull(result);
            Assert.Equal(401, result.StatusCode);

            var resultValue = result.Value as IDictionary<string, object>;
            Assert.NotNull(resultValue);
            Assert.True(resultValue.ContainsKey("message"));
            Assert.Equal("Nom d'utilisateur ou mot de passe incorrect.", resultValue["message"]);
        }
    }
}
