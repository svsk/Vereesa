using Vereesa.Core.Extensions;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Statistics;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class FlagService
    {
        private readonly IRepository<Statistics> _statRepository;

        private Statistics GetFlags() => _statRepository.FindById("flags") ?? new Statistics { Id = "flags" };

        public FlagService(IRepository<Statistics> statRepository)
        {
            _statRepository = statRepository;
        }

        public string SetFlag(string flagEmoji, string countryName)
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
