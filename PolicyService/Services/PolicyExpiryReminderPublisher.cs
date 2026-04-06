using PolicyService.Models;
using PolicyService.Repositories;

namespace PolicyService.Services
{
    public sealed class PolicyExpiryReminderPublisher(IServiceScopeFactory scopeFactory) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IPolicyRepository>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
                    var todayUtc = DateTime.UtcNow.Date;
                    var customerPolicies = await repository.GetPoliciesForReminderAsync(todayUtc, stoppingToken);
                    var policiesToExpire = await repository.GetPoliciesToExpireAsync(todayUtc, stoppingToken);
                    foreach (var expiredPolicy in policiesToExpire)
                    {
                        expiredPolicy.Status = CustomerPolicyStatus.Expired;
                        if (!expiredPolicy.ExpiredNotifiedOnUtc.HasValue)
                        {
                            expiredPolicy.ExpiredNotifiedOnUtc = DateTime.UtcNow;
                            await publisher.PublishAsync("PolicyExpired", new { expiredPolicy.CustomerPolicyId, expiredPolicy.PolicyId, expiredPolicy.CustomerId, ExpiredOn = expiredPolicy.EndDate.Date }, stoppingToken);
                        }
                    }
                    foreach (var customerPolicy in customerPolicies)
                    {
                        var daysUntilExpiry = (customerPolicy.EndDate.Date - todayUtc).Days;
                        if (daysUntilExpiry is <= 0 or > 30) continue;

                        if (daysUntilExpiry > 7)
                        {
                            if (customerPolicy.LastMonthlyWindowReminderSentOnUtc.HasValue && (todayUtc - customerPolicy.LastMonthlyWindowReminderSentOnUtc.Value.Date).TotalDays < 7)
                            {
                                continue;
                            }

                            customerPolicy.LastMonthlyWindowReminderSentOnUtc = DateTime.UtcNow;
                            await publisher.PublishAsync("PolicyExpiringReminder", new { customerPolicy.CustomerPolicyId, customerPolicy.PolicyId, customerPolicy.CustomerId, DaysUntilExpiry = daysUntilExpiry, ReminderWindow = "MonthlyWindow" }, stoppingToken);
                        }
                        else
                        {
                            if (customerPolicy.LastFinalWeekReminderSentOnUtc?.Date == todayUtc)
                            {
                                continue;
                            }
                            customerPolicy.LastFinalWeekReminderSentOnUtc = DateTime.UtcNow;
                            await publisher.PublishAsync("PolicyExpiringReminder", new { customerPolicy.CustomerPolicyId, customerPolicy.PolicyId, customerPolicy.CustomerId, DaysUntilExpiry = daysUntilExpiry, ReminderWindow = "FinalWeek" }, stoppingToken);
                        }
                    }
                    await repository.SaveChangesAsync(stoppingToken);
                }
                catch
                {
                }
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}