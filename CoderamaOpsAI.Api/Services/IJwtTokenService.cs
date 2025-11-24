using CoderamaOpsAI.Dal.Entities;

namespace CoderamaOpsAI.Api.Services;

public interface IJwtTokenService
{
    string GenerateToken(User user, DateTime expiresAt);
}
