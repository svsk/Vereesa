using Newtonsoft.Json;

namespace Vereesa.Neon.Services;

public class NeonCoinService
{
    private static Dictionary<ulong, int> _wallets = Load();
    private readonly Random _rng;

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
        EnsureUserHasWallet(userId);
        _wallets[userId] = _wallets[userId] + 1;
        Save(_wallets);
        return new NeonCoin { UserId = userId };
    }
}

public class NeonCoin
{
    public ulong UserId { get; set; }
}
