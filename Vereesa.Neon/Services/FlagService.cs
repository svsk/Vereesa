using System.Threading.Tasks;
using Vereesa.Core.Extensions;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Statistics;
using Vereesa.Core.Infrastructure;
using System.ComponentModel;
using Discord;

namespace Vereesa.Neon.Services
{
    public class FlagService : IBotService
    {
        private readonly IRepository<Statistics> _statRepository;

        private Statistics GetFlags() => _statRepository.FindById("flags") ?? new Statistics { Id = "flags" };

        public FlagService(IRepository<Statistics> statRepository)
        {
            _statRepository = statRepository;
        }

        [OnCommand("!flag set")]
        [WithArgument("countryName", 0)]
        [WithArgument("flagEmoji", 1)]
        [Authorize("Guild Master")]
        [Description("Sets a flag for the specified country.")]
        [CommandUsage("`!flag set <flag emoji> <country name>`")]
        public async Task EvaluateMessageAsync(IMessage message, string flagEmoji, string countryName) =>
            await message.Channel.SendMessageAsync(SetFlag(flagEmoji, countryName));

        private string SetFlag(string flagEmoji, string countryName)
        {
            if (string.IsNullOrWhiteSpace(flagEmoji) || string.IsNullOrWhiteSpace(countryName))
            {
                return "Please specify a flag emoji and a country name.";
            }

            var flags = GetFlags();
            flags.Upsert(countryName.ToTitleCase(), flagEmoji);
            _statRepository.AddOrEdit(flags);

            return $"OK! {countryName.ToTitleCase()} now has the flag {flagEmoji}!";
        }
    }
}
