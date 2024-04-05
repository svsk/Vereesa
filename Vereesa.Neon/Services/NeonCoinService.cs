using Discord;
using Newtonsoft.Json;
using Vereesa.Core.Infrastructure;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class NeonCoinService : IBotModule
    {
        private static Dictionary<ulong, int> _wallets = Load();
        private readonly Random _rng;

        public NeonCoinService(IServiceProvider services, Random rng)
        {
            _rng = rng;
        }

        private static Dictionary<ulong, int> Load()
        {
            try
            {
                var ledgerContent = File.ReadAllText("ledger.Local.json");
                return JsonConvert.DeserializeObject<Dictionary<ulong, int>>(ledgerContent);
            }
            catch
            {
                return new();
            }
        }

        [OnCommand("!givecoin")]
        public async Task HandleMessageAsync(IMessage message)
        {
            if (!_wallets.ContainsKey(message.Author.Id))
            {
                _wallets.TryAdd(message.Author.Id, 1);
            }
            else
            {
                _wallets[message.Author.Id] = _wallets[message.Author.Id] + 1;
            }

            var currentStanding = (decimal)_wallets[message.Author.Id];
            var euroExchangeRate = Math.Ceiling(_rng.Next(2, 3000) / (decimal)100);
            var euroWorth = currentStanding * euroExchangeRate;

            var response =
                $"🪙 Here's a Neon€oin for you, {message.Author.Username}! You now have {currentStanding} N€.";
            response += $"\n💰 That's €{euroWorth} in real money!";

            await message.Channel.SendMessageAsync(response);

            Save(_wallets);
        }

        private void Save(Dictionary<ulong, int> wallets)
        {
            File.WriteAllText("ledger.Local.json", JsonConvert.SerializeObject(wallets));
        }
    }
}
