using WeeklyUp.Domain.Entities;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.Interfaces.Services;

public interface IEmailSender
{
    public Task<Result<bool>> SendWeeklyReportAsync(string recipientEmail, string recipientName, Report report, CancellationToken ct = default);
    public Task<Result<bool>> SendWelcomeAsync(string recipientEmail, string recipientName, CancellationToken ct = default);
}
