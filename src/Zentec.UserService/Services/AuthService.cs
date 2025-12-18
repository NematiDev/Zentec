using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Zentec.UserService.Models.DTOs;
using Zentec.UserService.Models.Entities;

namespace Zentec.UserService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);

                if (existingUser != null)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "User with this email already exists.",
                        Errors = new List<string>() { "Email already registered." }
                    };
                }

                var user = new ApplicationUser
                {
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Registration failed.",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }
            }

            catch (Exception ex)
            {

            }
        }

        public Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            throw new NotImplementedException();
        }

        private Task<string> GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),

            };

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
