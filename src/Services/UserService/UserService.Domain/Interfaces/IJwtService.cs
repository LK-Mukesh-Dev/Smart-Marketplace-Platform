using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    Guid? ValidateToken(string token);
}
