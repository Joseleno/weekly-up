using WeeklyUp.Domain.Entities;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.Interfaces.Services;

public interface IEmailSender
{
    public Task<Result<bool>> SendWeeklyReportAsync(string recipientEmail, string recipientName, Report report, CancellationToken cancellationToken = default);
    public Task<Result<bool>> SendWelcomeAsync(string recipientEmail, string recipientName, CancellationToken cancellationToken = default);
    public Task<Result<bool>> SendVerificationEmailAsync(string recipientEmail, string recipientName, string token, CancellationToken cancellationToken = default);
}
