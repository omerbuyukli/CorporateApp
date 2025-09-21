using AutoMapper;
using CorporateApp.Application.DTOs;
using CorporateApp.Core.Entities;
using CorporateApp.Application.DTOs.User;  // Bu satırı ekleyin!

namespace CorporateApp.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>();
            CreateMap<User, UserListDto>();

        }
    }
}