using Microsoft.Extensions.Logging;
using Stripe;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.DataSources.Stripe;

public sealed class StripeDataProvider : IDataSourceProvider
{
    private readonly ChargeService _chargeService;
    private readonly ILogger<StripeDataProvider> _logger;

    public IntegrationProvider ProviderType => IntegrationProvider.Stripe;

    public StripeDataProvider(ChargeService chargeService, ILogger<StripeDataProvider> logger)
    {
        _chargeService = chargeService;
        _logger = logger;
    }

    public async Task<Result<ReportMetrics>> GetMetricsAsync(
        Guid userId, string providerAccountId, string? propertyId, DateRange weekRange, CancellationToken ct = default)
    {
        try
        {
            StripeList<Charge> charges = await FetchChargesAsync(weekRange, ct);

            if (charges.HasMore)
            {
                _logger.LogWarning(
                    "Stripe retornou mais de 100 transacoes para usuario {UserId} na semana {WeekStart}. " +
                    "Dados podem estar incompletos — paginacao nao implementada.",
                    userId, weekRange.Start);
            }

            return MapCharges(charges.Data);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Erro ao buscar dados do Stripe para usuario {UserId}", userId);
            return AppError.Failure("Stripe.Error", ex.Message);
        }
    }

    public Task<Result<Demographics?>> GetDemographicsAsync(
        Guid userId, string providerAccountId, string? propertyId, DateRange weekRange, CancellationToken ct = default) =>
        Task.FromResult(Result.Success<Demographics?>(null));

    private async Task<StripeList<Charge>> FetchChargesAsync(DateRange weekRange, CancellationToken ct)
    {
        var options = new ChargeListOptions
        {
            Created = new DateRangeOptions
            {
                GreaterThanOrEqual = weekRange.Start.ToDateTime(TimeOnly.MinValue),
                LessThanOrEqual = weekRange.End.ToDateTime(TimeOnly.MaxValue),
            },
            Limit = 100,
        };
        return await _chargeService.ListAsync(options, null, ct);
    }

    private static ReportMetrics MapCharges(IEnumerable<Charge> charges)
    {
        var succeeded = charges.Where(c => c.Status == "succeeded").ToList();
        decimal totalAmount = succeeded.Sum(c => c.Amount) / 100m;
        int count = succeeded.Count;
        decimal avgTicket = count > 0 ? totalAmount / count : 0m;

        return new ReportMetrics(
            revenue: Money.BRL(totalAmount),
            salesCount: count,
            averageTicket: Money.BRL(avgTicket),
            newCustomers: 0,
            totalVisits: 0,
            uniqueVisitors: 0,
            pageViews: 0,
            topPage: null,
            topTrafficSource: "Stripe",
            previousRevenue: null,
            previousVisits: null);
    }
}
