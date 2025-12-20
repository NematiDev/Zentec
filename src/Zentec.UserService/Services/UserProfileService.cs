using Microsoft.AspNetCore.Identity;
using Zentec.UserService.Models.DTOs;
using Zentec.UserService.Models.Entities;

namespace Zentec.UserService.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(UserManager<ApplicationUser> userManager, ILogger<UserProfileService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<ApiResponse<UserProfileResponse>> GetProfileAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Retrieving profile for user id: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Profile not found for user id: {UserId}", userId);

                    return new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "The specified user does not exist." }
                    };
                }

                var profile = new UserProfileResponse
                {
                    Id = user.Id.ToString(),
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Province = user.Province,
                    City = user.City,
                    Address = user.Address,
                    PostalCode = user.PostalCode,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt ?? user.CreatedAt
                };

                _logger.LogInformation("Profile retrieved successfully for user id: {UserId}", userId);

                return new ApiResponse<UserProfileResponse>
                {
                    Success = true,
                    Message = "Profile retrieved successfully",
                    Data = profile
                };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user id: {UserId}", userId);

                return new ApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving profile",
                    Errors = new List<string> { "Please try again later or contact support" }
                };
            }
        }

        public async Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
        {
            try
            {
                _logger.LogInformation("Updating profile for user id: {UserId}", userId);

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Profile not found for user id: {UserId}", userId);

                    return new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "User not found.",
                        Errors = new List<string> { "The specified user does not exist." }
                    };
                }

                // Track what changed for logging.
                var changes = new List<string>();

                if (!string.IsNullOrEmpty(request.FirstName) && user.FirstName != request.FirstName)
                {
                    user.FirstName = request.FirstName;
                    changes.Add("FirstName");
                }

                if (!string.IsNullOrEmpty(request.LastName) && user.LastName != request.LastName)
                {
                    user.LastName = request.LastName;
                    changes.Add("LastName");
                }

                if (!string.IsNullOrEmpty(request.Province) && user.Province != request.Province)
                {
                    user.Province = request.Province;
                    changes.Add("Province");
                }

                if (!string.IsNullOrEmpty(request.City) && user.City != request.City)
                {
                    user.City = request.City;
                    changes.Add("City");
                }

                if (!string.IsNullOrEmpty(request.PhoneNumber) && user.PhoneNumber != request.PhoneNumber)
                {
                    user.PhoneNumber = request.PhoneNumber;
                    changes.Add("PhoneNumber");
                }

                if (!string.IsNullOrEmpty(request.Address) && user.Address != request.Address)
                {
                    user.Address = request.Address;
                    changes.Add("Address");
                }

                if (!string.IsNullOrEmpty(request.PostalCode) && user.PostalCode != request.PostalCode)
                {
                    user.PostalCode = request.PostalCode;
                    changes.Add("PostalCode");
                }

                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Profile update failed for user {UserId}: {Errors}",
                        userId,
                        string.Join(", ", result.Errors.Select(e => e.Description)));

                    return new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "Profile update failed",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }

                _logger.LogInformation("Profile updated successfully for user {UserId}. Changed fields: {Changes}",
                userId,
                string.Join(", ", changes));

                var roles = await _userManager.GetRolesAsync(user);

                var profile = new UserProfileResponse
                {
                    Id = user.Id.ToString(),
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Province = user.Province,
                    City = user.City,
                    Address = user.Address,
                    PostalCode = user.PostalCode,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt ?? user.CreatedAt
                };

                return new ApiResponse<UserProfileResponse>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = profile
                };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", userId);

                return new ApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred while updating profile",
                    Errors = new List<string> { "Please try again later or contact support" }
                };
            }
        }
    }
}
