using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    public string GenerateToken(User user);
    public string GenerateRefreshToken();
}
