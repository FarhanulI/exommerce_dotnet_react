using API.DTOs;
using API.Entities;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        // UserManager and TokenService dependencies injected via constructor
        private readonly UserManager<User> _userManager;
        private readonly TokenService _tokenService;

        public AccountController(UserManager<User> userManager, TokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        // Endpoint for user login
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            // Find the user by username
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            // Check if user exists and password is correct
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
                return Unauthorized();

            // Return user details with generated token
            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.GenerateToken(user)
            };
        }

        // Endpoint for user registration
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterDto registerDto)
        {
            // Create a new user object with provided details
            var user = new User { UserName = registerDto.UserName, Email = registerDto.Email };

            // Attempt to create user in database
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            // If user creation failed, return validation errors
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return ValidationProblem();
            }

            // Add user to "Member" role
            await _userManager.AddToRoleAsync(user, "Member");

            // Return success status code
            return StatusCode(201);
        }

        // Endpoint to get details of currently authenticated user
        [Authorize]
        [HttpGet("currentUser")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            // Find the user by username
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            // Return user details with generated token
            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.GenerateToken(user),
            };
        }
    }
}
