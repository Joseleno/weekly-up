using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Queries.GetUserProfile;

public sealed class GetUserProfileQueryHandler
    : IQueryHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private static readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);

    private readonly IUserRepository _users;
    private readonly IApplicationCacheService _cache;

    public GetUserProfileQueryHandler(
        IUserRepository users,
        IApplicationCacheService cache)
    {
        _users = users;
        _cache = cache;
    }

    public async ValueTask<Result<UserProfileDto>> Handle(
        GetUserProfileQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UserProfile(query.UserId);
        var cached = await _cache.GetAsync<UserProfileDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var user = await _users.GetByIdAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuário '{query.UserId}' não encontrado.");
        }

        var dto = user.ToProfileDto();
        await _cache.SetAsync(cacheKey, dto, _cacheExpiry, cancellationToken);
        return dto;
    }
}
