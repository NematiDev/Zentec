using Zentec.OrderService.Models.DTOs;

namespace Zentec.OrderService.Services
{
    public interface IUserClient
    {
        /// <summary>
        /// Get user profile from User Service
        /// </summary>
        Task<UserApiResponse<UserProfileResponse>> GetUserProfileAsync(string bearerToken, CancellationToken ct);
    }
}