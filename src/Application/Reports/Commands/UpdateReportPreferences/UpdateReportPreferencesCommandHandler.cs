using Mediator;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Commands.UpdateReportPreferences;

public sealed class UpdateReportPreferencesCommandHandler
    : ICommandHandler<UpdateReportPreferencesCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpdateReportPreferencesCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<bool>> Handle(
        UpdateReportPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        ReportPreference? preference = await _uow.ReportPreferences.GetByUserIdAsync(
            command.UserId, cancellationToken);

        if (preference is null)
        {
            preference = ReportPreference.CreateDefault(command.UserId);
            preference.Update(command.SendDay, command.SendTime, command.EnabledSections);
            await _uow.ReportPreferences.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.Update(command.SendDay, command.SendTime, command.EnabledSections);
            _uow.ReportPreferences.Update(preference);
        }

        return true;
    }
}
