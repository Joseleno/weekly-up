namespace WeeklyUp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    public Guid UserId { get; }
    public bool IsAuthenticated { get; }
}
