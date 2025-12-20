using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Zentec.UserService.Models.DTOs;
using Zentec.UserService.Models.Entities;

namespace Zentec.UserService.Services
{
    /// <summary>
    /// Service for handling registration, login, and JWT token generation.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration config,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
                    UserName = request.Email,
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

                if (!await _roleManager.RoleExistsAsync("User"))
                {
                    var userRole = new IdentityRole<Guid>("User");
                    await _roleManager.CreateAsync(userRole);
                }

                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var adminRole = new IdentityRole<Guid>("Admin");
                    await _roleManager.CreateAsync(adminRole);
                }

                await _userManager.AddToRoleAsync(user, "Admin");

                var token = await GenerateJwtTokenAsync(user);

                var jwtSettings = _config.GetSection("JwtSettings");

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Registration successful.",
                    Data = new AuthResponse
                    {
                        Token = token,
                        ExpiresAt = DateTime.UtcNow.AddHours(int.Parse(jwtSettings["AccessTokenHours"] ?? "24")),
                        User = new UserBasicResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            PhoneNumber = user.PhoneNumber,
                            CreatedAt = user.CreatedAt
                        }
                    }
                };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registration for email: {Email}", request.Email);
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An unexpected error occured during registration.",
                    Errors = new List<string>() { "Please try again later or contact support." }
                };
            }
        }

        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email}", request.Email);

                // Find user by email
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Errors = new List<string> { "Email or password is incorrect" }
                    };
                }

                // Verify password
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);

                if (!isPasswordValid)
                {
                    _logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);

                    return new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid credentials",
                        Errors = new List<string> { "Email or password is incorrect" }
                    };
                }

                _logger.LogInformation("User {Email} logged in successfully", request.Email);

                // Generate JWT token
                var token = await GenerateJwtTokenAsync(user);

                return new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        ExpiresAt = DateTime.UtcNow.AddHours(
                            int.Parse(_config["JwtSettings:AccessTokenHours"] ?? "24")),
                        User = new UserBasicResponse
                        {
                            Id = user.Id,
                            Email = user.Email!,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            PhoneNumber = user.PhoneNumber,
                            CreatedAt = user.CreatedAt
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for email: {Email}", request.Email);
                return new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred during login",
                    Errors = new List<string> { "Please try again later or contact support" }
                };
            }
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            try
            {
                var jwtSettings = _config.GetSection("JwtSettings");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SigningKey"]!));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("firstName", user.FirstName),
                new Claim("lastName", user.LastName)
            };

                var roles = await _userManager.GetRolesAsync(user);
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(int.Parse(jwtSettings["AccessTokenHours"] ?? "24")),
                    signingCredentials: credentials
                    );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                _logger.LogDebug("JWT token generated for user {UserId}", user.Id);

                return tokenString;
            }
            
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
                throw;
            }
        }
    }
}
