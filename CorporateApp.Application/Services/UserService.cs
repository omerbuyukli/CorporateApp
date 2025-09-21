using AutoMapper;
using BCrypt.Net;
using CorporateApp.Application.DTOs;
using CorporateApp.Application.DTOs.User;
using CorporateApp.Application.Interfaces;
using CorporateApp.Core.Entities;
using CorporateApp.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CorporateApp.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IMapper mapper, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                var emailExists = await _userRepository.EmailExistsAsync(createUserDto.Email);
                if (emailExists)
                {
                    throw new Exception(string.Format("Email {0} is already in use", createUserDto.Email));
                }

                if (!string.IsNullOrEmpty(createUserDto.Tcno))
                {
                    var tcnoExists = await _userRepository.TcnoExistsAsync(createUserDto.Tcno);
                    if (tcnoExists)
                    {
                        throw new Exception(string.Format("TC No {0} is already in use", createUserDto.Tcno));
                    }
                }

                var user = new User
                {
                    Name = createUserDto.Name,
                    LastName = createUserDto.LastName,
                    Email = createUserDto.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                    Tcno = createUserDto.Tcno,
                    Location = createUserDto.Location,
                    RoleId = createUserDto.RoleId,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);
                
                _logger.LogInformation("User created successfully: {Email}", user.Email);
                return _mapper.Map<UserDto>(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                throw;
            }
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto> UpdateUserAsync(int id, UserDto userDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new Exception(string.Format("User with id {0} not found", id));
            }

            if (user.Email != userDto.Email)
            {
                var emailExists = await _userRepository.EmailExistsAsync(userDto.Email);
                if (emailExists)
                {
                    throw new Exception(string.Format("Email {0} is already in use", userDto.Email));
                }
            }

            if (user.Tcno != userDto.Tcno && !string.IsNullOrEmpty(userDto.Tcno))
            {
                var tcnoExists = await _userRepository.TcnoExistsAsync(userDto.Tcno);
                if (tcnoExists)
                {
                    throw new Exception(string.Format("TC No {0} is already in use", userDto.Tcno));
                }
            }

            user.Name = userDto.Name;
            user.LastName = userDto.LastName;
            user.Email = userDto.Email;
            user.Tcno = userDto.Tcno;
            user.Location = userDto.Location;
            user.RoleId = userDto.RoleId;
            user.IsActive = userDto.IsActive;
            user.UpdatedDate = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            
            _logger.LogInformation("User updated successfully: {Id}", id);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            await _userRepository.DeleteAsync(user);
            _logger.LogInformation("User deleted successfully: {Id}", id);
            return true;
        }

        public async Task<UserListResponse> GetUsersListAsync(GetUsersListRequest request)
        {
            var result = await _userRepository.GetPagedUsersAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.RoleId,
                request.IsActive
            );

            var userListDtos = _mapper.Map<IEnumerable<UserListDto>>(result.Users);
            
            return new UserListResponse
            {
                Users = userListDtos,
                TotalCount = result.TotalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}
