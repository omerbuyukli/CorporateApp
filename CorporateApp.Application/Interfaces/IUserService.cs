using CorporateApp.Application.DTOs;
using CorporateApp.Application.DTOs.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CorporateApp.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> GetUserByIdAsync(int id);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> UpdateUserAsync(int id, UserDto userDto);
        Task<bool> DeleteUserAsync(int id);
        Task<UserListResponse> GetUsersListAsync(GetUsersListRequest request);
        Task<ChangePasswordResponseDto> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<ChangePasswordResponseDto> AdminChangePasswordAsync(int userId, AdminChangePasswordDto adminChangePasswordDto);

    }
}
