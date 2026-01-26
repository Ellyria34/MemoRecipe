using AutoMapper;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.DTOs.Users;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Mappings.Profiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<RegisterDto, User>();
        CreateMap<User, UserDto>();
    }
}
