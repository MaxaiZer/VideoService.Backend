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

        public AuthControllerTests(TestingWebAppFactory factory)
        {
            _client = factory.CreateClient();
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
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
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
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task RegisterUser_WhenUserAlreadyExists_ShouldReturn400()
        {
            var userForRegistration = new UserForRegistrationDto
            {
                UserName = DatabaseSeeder.existingUsersWithActiveTokens[0].Name,
                Password = DatabaseSeeder.existingUsersWithActiveTokens[0].Password,
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/register", userForRegistration);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Authenticate_WhenValidCredentials_ShouldReturn200AndTokens()
        {
            var userForAuthentication = new UserForAuthenticationDto
            {
                UserName = DatabaseSeeder.existingUsersWithActiveTokens[0].Name,
                Password = DatabaseSeeder.existingUsersWithActiveTokens[0].Password
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/login", userForAuthentication);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            
            var tokenDto = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
            tokenDto.Should().NotBeNull();
            tokenDto.AccessToken.Should().NotBeNullOrEmpty();

            GetRefreshTokenFromCookie(response);
        }

        [Fact]
        public async Task Authenticate_WhenInvalidCredentials_ShouldReturn401()
        {
            var userForAuthentication = new UserForAuthenticationDto
            {
                UserName = DatabaseSeeder.existingUsersWithActiveTokens[0].Name,
                Password = DatabaseSeeder.existingUsersWithActiveTokens[0].Password.Insert(0, "r")
            };
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/login", userForAuthentication);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_WhenValidTokens_ShouldReturn200AndNewTokens()
        {
            var tokenDto = new AccessTokenDto(AccessToken: DatabaseSeeder.existingUsersWithActiveTokens[1].AccessToken);
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={DatabaseSeeder.existingUsersWithActiveTokens[1].RefreshToken}");
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/refresh", tokenDto);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            
            var newTokenDto = await response.Content.ReadFromJsonAsync<AccessTokenDto>();
            newTokenDto.Should().NotBeNull();
            newTokenDto.AccessToken.Should().NotBeNullOrEmpty();
            
            GetRefreshTokenFromCookie(response);
        }

        [Fact]
        public async Task Refresh_WhenInvalidTokens_ShouldReturn400()
        {
            var tokenDto = new AccessTokenDto(AccessToken: "invalidToken");
            _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=refreshToken");
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/refresh", tokenDto);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Refresh_WhenExpiredToken_ShouldReturn400()
        {
            var tokenDto = new AccessTokenDto(AccessToken: DatabaseSeeder.existingUserWithExpiredToken.AccessToken);
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={DatabaseSeeder.existingUserWithExpiredToken.RefreshToken}");
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/refresh", tokenDto);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Refresh_WhenMissingRefreshToken_ShouldReturn400()
        {
            var tokenDto = new AccessTokenDto(AccessToken: DatabaseSeeder.existingUserWithExpiredToken.AccessToken);
            
            var response = await _client.PostAsJsonAsync(_baseUrl + "/refresh", tokenDto);
            
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
        
        [Fact]
        public async Task Revoke_WhenValidUser_ShouldRevokeToken()
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", DatabaseSeeder.existingUserForTokenRevoke.AccessToken);
            
            var tokenDto = new AccessTokenDto(AccessToken: DatabaseSeeder.existingUserForTokenRevoke.AccessToken);
            _client.DefaultRequestHeaders.Add("Cookie", $"refreshToken={DatabaseSeeder.existingUserForTokenRevoke.RefreshToken}");
            
            var response1 = await _client.PostAsync(_baseUrl + "/revoke", null);
            var response2 = await _client.PostAsJsonAsync(_baseUrl + "/refresh", tokenDto);
            
            response1.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            response2.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        private string GetRefreshTokenFromCookie(HttpResponseMessage response)
        {
            response.Headers.TryGetValues("Set-Cookie", out var cookies);
            cookies.Should().ContainSingle();
            var refreshTokenCookie = cookies.First().Split(';').First();
            refreshTokenCookie.Should().StartWith("refreshToken=");

            return refreshTokenCookie;
        }
}