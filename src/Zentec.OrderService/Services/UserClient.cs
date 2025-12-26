using System.Net.Http.Headers;
using System.Net.Http.Json;
using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public class UserClient : IUserClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<UserClient> _logger;

        public UserClient(HttpClient http, ILogger<UserClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<UserApiResponse<UserProfileResponse>> GetUserProfileAsync(string bearerToken, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, "api/User/profile");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                var res = await _http.SendAsync(req, ct);
                var payload = await res.Content.ReadFromJsonAsync<UserApiResponse<UserProfileResponse>>(cancellationToken: ct);

                if (payload == null)
                {
                    return new UserApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "Invalid response from user service"
                    };
                }

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling UserService profile endpoint");
                return new UserApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "User service unavailable",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}