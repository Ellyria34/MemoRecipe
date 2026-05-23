using Riok.Mapperly.Abstractions;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.DTOs.Users;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Mappings.Profiles;


[Mapper]
public static partial class UserMapper
{
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.PasswordSalt))]
    [MapperIgnoreSource(nameof(User.IsActive))]
    [MapperIgnoreSource(nameof(User.Role))]
    [MapperIgnoreSource(nameof(User.CreatedAt))]
    [MapperIgnoreSource(nameof(User.UpdatedAt))]
    [MapperIgnoreSource(nameof(User.Recipes))]
    [MapperIgnoreSource(nameof(User.Comments))]
    [MapperIgnoreSource(nameof(User.Favorites))]
    public static partial UserDto ToDto(this User user);

    [MapperIgnoreSource(nameof(RegisterDto.Password))]
    [MapperIgnoreTarget(nameof(User.Id))]
    [MapperIgnoreTarget(nameof(User.PasswordHash))]
    [MapperIgnoreTarget(nameof(User.PasswordSalt))]
    [MapperIgnoreTarget(nameof(User.IsActive))]
    [MapperIgnoreTarget(nameof(User.Role))]
    [MapperIgnoreTarget(nameof(User.CreatedAt))]
    [MapperIgnoreTarget(nameof(User.UpdatedAt))]
    [MapperIgnoreTarget(nameof(User.Recipes))]
    [MapperIgnoreTarget(nameof(User.Comments))]
    [MapperIgnoreTarget(nameof(User.Favorites))]
    public static partial User ToEntity(this RegisterDto dto);
}