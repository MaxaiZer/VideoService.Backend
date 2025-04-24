using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CoreService.Application.Dto;
using CoreService.IntegrationTests.Tools;
using FluentAssertions;

namespace CoreService.IntegrationTests.Tests;

[Collection("Environment collection")]
public class AuthControllerTests: IClassFixture<TestingWebAppFactory>
{
        private readonly HttpClient _client;
        private const string _baseUrl = "api/auth";

        private static int _currectTestUser = 0;

        public AuthControllerTests(TestingWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        public TestUserData GetNextUserWithActiveTokens()
        {
            return DatabaseSeeder.usersWithActiveTokens[_currectTestUser++];
        }

        [Fact]
        public async Task RegisterUser_WhenValidUser_ShouldReturn201()
        {
            var userForRegistration = new UserForRegistrationDto
            {
                UserName = "test",
                Password = "Test@1234"
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/register", userForRegistration);
            
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        [Fact]
        public async Task RegisterUser_WhenInvalidData_ShouldReturn400()
        {
            var userForRegistration = new UserForRegistrationDto
            {
                UserName = "what",
                Password = "s",
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/register", userForRegistration);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task RegisterUser_WhenUserAlreadyExists_ShouldReturn400()
        {
            var user = GetNextUserWithActiveTokens();
            var userForRegistration = new UserForRegistrationDto
            {
                UserName = user.Name,
                Password = user.Password,
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/register", userForRegistration);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Authenticate_WhenValidCredentials_ShouldReturn200AndTokens()
        {
            var user = GetNextUserWithActiveTokens();
            var userForAuthentication = new UserForAuthenticationDto
            {
                UserName = user.Name,
                Password = user.Password
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/login", userForAuthentication);
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var tokenDto = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
            tokenDto.Should().NotBeNull();
            tokenDto.AccessToken.Should().NotBeNullOrEmpty();

            GetRefreshTokenFromCookie(response);
        }

        [Fact]
        public async Task Authenticate_WhenInvalidCredentials_ShouldReturn401()
        {
            var user = GetNextUserWithActiveTokens();
            var userForAuthentication = new UserForAuthenticationDto
            {
                UserName = user.Name,
                Password = user.Password.Insert(0, "r")
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/login", userForAuthentication);
            
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenValidTokens_ShouldReturn200AndNewTokens()
        {
            var user = GetNextUserWithActiveTokens();
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={user.RefreshToken}");
            
            var response = await _client.PostAsync(_baseUrl + "/refresh", null);
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var newTokenDto = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
            newTokenDto.Should().NotBeNull();
            newTokenDto.AccessToken.Should().NotBeNullOrEmpty();
            
            GetRefreshTokenFromCookie(response);
        }

        [Fact]
        public async Task Refresh_WhenNotUsedRefreshToken_ShouldReturn200()
        {
            var user = GetNextUserWithActiveTokens();
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={user.RefreshToken}");
            
            var response = await _client.PostAsync(_baseUrl + "/refresh", null);
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            _client.DefaultRequestHeaders.Remove("Cookie");
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={GetRefreshTokenFromCookie(response)}");

            response = await _client.PostAsync(_baseUrl + "/refresh", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        
        [Fact]
        public async Task Refresh_WhenUsedRefreshToken_ShouldReturn400()
        {
            var user = GetNextUserWithActiveTokens();
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={user.RefreshToken}");
            
            var response = await _client.PostAsync(_baseUrl + "/refresh", null);
            
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            response = await _client.PostAsync(_baseUrl + "/refresh", null);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Refresh_WhenInvalidTokens_ShouldReturn400()
        {
            _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=somerandomrefreshToken");
            
            var response = await _client.PostAsync(_baseUrl + "/refresh", null);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Refresh_WhenExpiredToken_ShouldReturn400()
        {
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={DatabaseSeeder.userWithExpiredToken.RefreshToken}");
            
            var response = await _client.PostAsync(_baseUrl + "/refresh", null);
            
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Refresh_WhenMissingRefreshToken_ShouldReturn400()
        {
            var response = await _client.PostAsync(_baseUrl + "/refresh", null);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Revoke_WhenValidUser_ShouldRevokeToken()
        {
            var user = GetNextUserWithActiveTokens();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
            
            var tokenDto = new AccessTokenDto(AccessToken: user.AccessToken);
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={user.RefreshToken}");
            
            var response1 = await _client.PostAsync(_baseUrl + "/logout", null);
            var response2 = await _client.PostAsJsonAsync(_baseUrl + "/refresh", tokenDto);
            
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private string GetRefreshTokenFromCookie(HttpResponseMessage response)
        {
            response.Headers.TryGetValues("Set-Cookie", out var cookies);
            cookies.Should().ContainSingle();
            var refreshTokenCookie = cookies.First().Split(';').First();
            refreshTokenCookie.Should().StartWith("refreshToken=");

            return refreshTokenCookie.Replace("refreshToken=", "");
        }
}