using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Zentec.UserService.Models.DTOs;
using Zentec.UserService.Services;

namespace Zentec.UserService.Controllers
{
    /// <summary>
    /// Controller for user profile and account management operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly IUserProfileService _profileService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserProfileService profileService, ILogger<UserController> logger)
        {
            _profileService = profileService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's complete profile
        /// </summary>
        /// <returns>Complete user profile with all details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        /// <response code="404">User not found</response>
        [HttpGet("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetProfile called with invalid user token");
                    return Unauthorized(new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                _logger.LogInformation("Getting profile for user: {UserId}", userId);

                var result = await _profileService.GetProfileAsync(userId);

                if (!result.Success)
                {
                    _logger.LogWarning("Profile not found for user: {UserId}", userId);
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving profile"
                });
            }
        }

        /// <summary>
        /// Update current user's profile
        /// </summary>
        /// <param name="request">Updated profile information</param>
        /// <returns>Updated user profile</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid input or update failed</response>
        /// <response code="401">Unauthorized - user not authenticated</response>
        [HttpPut("profile")]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("UpdateProfile called with invalid user token");
                    return Unauthorized(new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                _logger.LogInformation("Updating profile for user: {UserId}", userId);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Profile update validation failed for user: {UserId}", userId);
                    return BadRequest(new ApiResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "Invalid profile data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _profileService.UpdateProfileAsync(userId, request);

                if (!result.Success)
                {
                    _logger.LogWarning("Profile update failed for user: {UserId}", userId);
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred while updating profile"
                });
            }
        }
    }
}
