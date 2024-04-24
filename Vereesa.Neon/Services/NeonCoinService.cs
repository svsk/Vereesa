using Newtonsoft.Json;
using Vereesa.Neon.Exceptions;

namespace Vereesa.Neon.Services;

public class NeonCoinService
{
    private static Dictionary<ulong, int> _wallets = Load();
    private readonly Random _rng;
    private DateTimeOffset _lastCoinGeneration = DateTimeOffset.MinValue;
    private TimeSpan _coinGenerationCooldown = TimeSpan.FromMinutes(60);

    public NeonCoinService(Random rng)
    {
        _rng = rng;
    }

    private static Dictionary<ulong, int> Load()
    {
        try
        {
            var ledgerContent = File.ReadAllText("ledger.Local.json");
            if (ledgerContent == null)
            {
                return new();
            }

            return JsonConvert.DeserializeObject<Dictionary<ulong, int>>(ledgerContent) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public record NeonCoinHoldings(decimal CoinCount, decimal EuroWorth);

    public NeonCoinHoldings GetUserCoinHoldings(ulong userId)
    {
        EnsureUserHasWallet(userId);

        var currentStanding = (decimal)_wallets[userId];
        var euroExchangeRate = Math.Ceiling(_rng.Next(2, 3000) / (decimal)100);
        var euroWorth = currentStanding * euroExchangeRate;

        return new NeonCoinHoldings(currentStanding, euroWorth);
    }

    private void Save(Dictionary<ulong, int> wallets)
    {
        File.WriteAllText("ledger.Local.json", JsonConvert.SerializeObject(wallets));
    }

    private void EnsureUserHasWallet(ulong userId)
    {
        if (!_wallets.ContainsKey(userId))
        {
            _wallets.TryAdd(userId, 0);
        }
    }

    public NeonCoin GenerateCoin(ulong userId)
    {
        if (_lastCoinGeneration.Add(_coinGenerationCooldown) > DateTimeOffset.Now)
        {
            throw new OperationThrottledException(_coinGenerationCooldown);
        }

        EnsureUserHasWallet(userId);
        _wallets[userId] = _wallets[userId] + 1;
        Save(_wallets);

        _lastCoinGeneration = DateTimeOffset.Now;

        return new NeonCoin { UserId = userId };
    }

    public void TipCoin(ulong fromUser, ulong toUser)
    {
        EnsureUserHasWallet(fromUser);
        EnsureUserHasWallet(toUser);

        if (_wallets[fromUser] < 1)
        {
            throw new InsufficientFundsException();
        }

        _wallets[fromUser] = _wallets[fromUser] - 1;
        _wallets[toUser] = _wallets[toUser] + 1;

        Save(_wallets);
    }
}

public class NeonCoin
{
    public ulong UserId { get; set; }
}
