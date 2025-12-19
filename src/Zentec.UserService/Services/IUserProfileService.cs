using Zentec.UserService.Models.DTOs;

namespace Zentec.UserService.Services
{
    public interface IUserProfileService
    {
        Task<ApiResponse<UserProfileResponse>> GetProfileAsync(string userId);
        Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    }
}
