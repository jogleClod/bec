using Microsoft.AspNetCore.Mvc;
using MobileAppBackend.DTOs;
using MobileAppBackend.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MobileAppBackend.Models;


namespace MobileAppBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public UsersController(UserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            try
            {
                var user = await _userService.RegisterAsync(userDto);
                var token = GenerateJwtToken(user);
                
                var response = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    CreatedAt = user.CreatedAt,
                    Token = token
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = await _userService.LoginAsync(loginDto);
                var token = GenerateJwtToken(user);
                
                var response = new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    CreatedAt = user.CreatedAt,
                    Token = token
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        private string GenerateJwtToken(User user)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
    
    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: new[]
        {
            new Claim("id", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("name", user.Name)
        },
        expires: DateTime.UtcNow.AddDays(7),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    );

    return tokenHandler.WriteToken(token);
}
    }
}