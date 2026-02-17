using UserService.Application.DTOs;
using UserService.Application.Queries;
using UserService.Domain.Interfaces;

namespace UserService.Application.Handlers;

public class GetUserProfileHandler
{
    private readonly IUserRepository _userRepository;

    public GetUserProfileHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> HandleAsync(GetUserProfileQuery query)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId);
        
        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
