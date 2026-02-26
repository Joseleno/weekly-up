using WeeklyUp.Domain.Entities;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.Interfaces.Services;

public interface IWhatsAppSender
{
    public Task<Result<bool>> SendWeeklyReportAsync(string phoneNumber, string recipientName, Report report, CancellationToken ct = default);
}
