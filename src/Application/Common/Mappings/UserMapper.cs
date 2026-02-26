using Riok.Mapperly.Abstractions;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Application.Common.Mappings;

[Mapper]
public static partial class UserMapper
{
    public static UserProfileDto ToProfileDto(this User user) =>
        new(
            Id: user.Id,
            Email: user.Email.Value,
            Name: user.Name,
            BusinessName: user.BusinessName.Value,
            BusinessType: user.BusinessType.ToString(),
            Plan: user.Plan.ToString(),
            IsEmailVerified: user.IsEmailVerified,
            IsActive: user.IsActive,
            CreatedAt: new DateTimeOffset(user.CreatedAt, TimeSpan.Zero));
}
