using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Zentec.UserService.Models.DTOs;
using Zentec.UserService.Services;
using LoginRequest = Zentec.UserService.Models.DTOs.LoginRequest;
using RegisterRequest = Zentec.UserService.Models.DTOs.RegisterRequest;

namespace Zentec.UserService.Controllers
{
    /// <summary>
    /// Controller for authentication operations (register, login)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger) 
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">User registration details</param>
        /// <returns>Authentication response with JWT token</returns>
        /// <response code="200">User registered successfully</response>
        /// <response code="400">Invalid input or registration failed</response>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Registration validation failed for {Email}", request.Email);

                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid registration data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _authService.RegisterAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("Registration failed for {Email}: {Message}",
                        request.Email, result.Message);
                    return BadRequest(result);
                }

                _logger.LogInformation("User registered successfully: {Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for {Email}", request.Email);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = new List<string> { "Please try again later or contact support" }
                });
            }
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication response with JWT token</returns>
        /// <response code="200">Login successful</response>
        /// <response code="400">Invalid credentials or login failed</response>
        /// <response code="401">Unauthorized - invalid email or password</response>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt for email: {Email} from IP: {IpAddress}",
                    request.Email,
                    HttpContext.Connection.RemoteIpAddress);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login validation failed for {Email}", request.Email);

                    return BadRequest(new ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid login data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _authService.LoginAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("Login failed for {Email}: {Message}",
                        request.Email, result.Message);

                    return Unauthorized(result);
                }

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for {Email}", request.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "An unexpected error occurred",
                    Errors = new List<string> { "Please try again later or contact support" }
                });
            }
        }
    }
}
