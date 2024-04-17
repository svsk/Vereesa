using Microsoft.Extensions.DependencyInjection;
using Vereesa.Neon.Data;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Repositories;

namespace Vereesa.Neon.Services;

public static class TimeZoneServiceExtensions
{
    public static IServiceCollection AddTimeZoneService(this IServiceCollection services)
    {
        services.AddTransient<TimeZoneService>();
        services.AddTransient<IRepository<UserTimezoneSettings>, AzureStorageRepository<UserTimezoneSettings>>();

        return services;
    }
}

public class TimeZoneService
{
    private readonly IRepository<UserTimezoneSettings> _repository;

    public TimeZoneService(IRepository<UserTimezoneSettings> repository)
    {
        _repository = repository;
    }

    public async Task<UserTimezoneSettings> GetUserTimezoneSettingsAsync(ulong userId) =>
        await _repository.FindByIdAsync(userId.ToString());

    public async Task<UserTimezoneSettings> SetUserTimezoneSettingsAsync(ulong userId, string timezoneId)
    {
        var settings = new UserTimezoneSettings
        {
            Id = userId.ToString(),
            UserId = userId.ToString(),
            TimeZoneId = timezoneId
        };

        await _repository.AddOrEditAsync(settings);

        return settings;
    }

    public async Task<DateTimeOffset> GetTimeForUser(ulong userId, DateTimeOffset value)
    {
        var userTimezone = await GetUserTimezoneSettingsAsync(userId);
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(userTimezone.TimeZoneId);
        return TimeZoneInfo.ConvertTime(value, timezone);
    }
}
