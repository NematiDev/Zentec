using Zentec.UserService.Models.DTOs;

namespace Zentec.UserService.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    }
}
