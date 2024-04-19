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

    public async Task<string> GetUserTimeZone(ulong userId, string fallback = null)
    {
        var userTimezone = await GetUserTimezoneSettingsAsync(userId);
        return userTimezone?.TimeZoneId ?? fallback;
    }

    public async Task<Dictionary<ulong, string>> GetTimeZonesForUserIds(
        IEnumerable<ulong> userIds,
        string fallback = null
    )
    {
        if (userIds?.Any() != true)
        {
            return new();
        }

        var stringIds = userIds.Select(x => x.ToString()).ToList();

        var userTimezones = await _repository.GetAllAsync();

        return userIds.ToDictionary(
            userId => userId,
            userId =>
            {
                var userTimezone = userTimezones.FirstOrDefault(y => y.UserId == userId.ToString());
                return userTimezone?.TimeZoneId ?? fallback;
            }
        );
    }
}
