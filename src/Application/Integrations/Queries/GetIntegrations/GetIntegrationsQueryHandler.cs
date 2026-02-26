using Mediator;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Integrations.Queries.GetIntegrations;

public sealed class GetIntegrationsQueryHandler
    : IQueryHandler<GetIntegrationsQuery, Result<IReadOnlyList<IntegrationDto>>>
{
    private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    private readonly IUserRepository _users;
    private readonly IApplicationCacheService _cache;

    public GetIntegrationsQueryHandler(
        IUserRepository users,
        IApplicationCacheService cache)
    {
        _users = users;
        _cache = cache;
    }

    public async ValueTask<Result<IReadOnlyList<IntegrationDto>>> Handle(
        GetIntegrationsQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Integrations(query.UserId);
        var cached = await _cache.GetAsync<IReadOnlyList<IntegrationDto>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var user = await _users.GetByIdWithIntegrationsAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuário '{query.UserId}' não encontrado.");
        }

        IReadOnlyList<IntegrationDto> dtos = user.Integrations
            .Select(i => i.ToDto())
            .ToList()
            .AsReadOnly();

        await _cache.SetAsync(cacheKey, dtos, _cacheExpiry, cancellationToken);
        return Result.Success(dtos);
    }
}
