using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorporateApp.Application.DTOs;
using CorporateApp.Application.DTOs.User;
using CorporateApp.Application.Interfaces;
using System.Threading.Tasks;
using System.Security.Claims;

namespace CorporateApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            var result = await _userService.CreateUserAsync(createUserDto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var result = await _userService.GetUserByIdAsync(id);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        [HttpPost("list")]
        public async Task<IActionResult> GetUsersList([FromBody] GetUsersListRequest request)
        {
            var result = await _userService.GetUsersListAsync(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
        {
            var result = await _userService.UpdateUserAsync(id, userDto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
                return NotFound();
            return NoContent();
        }

        // ÖNEMLİ: Route farklı olmalı - "change-password" eklendi
        [HttpPost("change-password")]
        [AllowAnonymous] // veya sadece [Authorize] - kendi şifresini değiştirebilir
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                // Token'dan user ID'yi al
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Geçersiz kullanıcı token'ı" });
                }

                var result = await _userService.ChangePasswordAsync(userId, changePasswordDto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Sunucu hatası", error = ex.Message });
            }
        }

        // Admin için başka kullanıcının şifresini değiştirme - farklı route
        [HttpPost("{id}/change-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminChangePassword(int id, [FromBody] AdminChangePasswordDto adminChangePasswordDto)
        {
            try
            {
                var result = await _userService.AdminChangePasswordAsync(id, adminChangePasswordDto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Sunucu hatası", error = ex.Message });
            }
        }
    }
}