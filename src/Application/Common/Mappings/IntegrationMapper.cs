using Riok.Mapperly.Abstractions;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Application.Common.Mappings;

[Mapper]
public static partial class IntegrationMapper
{
    public static IntegrationDto ToDto(this Integration integration) =>
        new(
            Id: integration.Id,
            Provider: integration.Provider.ToString(),
            Status: integration.Status.ToString(),
            AccountId: integration.ProviderAccountId,
            ConnectedAt: new DateTimeOffset(integration.CreatedAt, TimeSpan.Zero));
}
