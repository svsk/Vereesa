using System.Text.RegularExpressions;
using Discord;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;
using Vereesa.Core;

namespace Vereesa.Neon.Services
{
    public class MovieSuggestionService : IBotModule
    {
        [OnMessage]
        public async Task HandleMessageReceivedAsync(IMessage message)
        {
            string command = message.GetCommand()?.ToLowerInvariant();

            switch (command)
            {
                case "!moviesuggest":
                case "!moviesuggestion":
                case "!movie":
                    (string title, string year) titleAndYear = await GetRandomMovieSuggestionAsync();
                    await message.Channel.SendMessageAsync(
                        $"Try \"{titleAndYear.title}\", {message.Author.Username}. It was made in {titleAndYear.year} and should be on Netflix."
                    );
                    break;
            }
        }

        private async Task<(string title, string year)> GetRandomMovieSuggestionAsync()
        {
            using (var client = new HttpClient())
            {
                HttpResponseMessage result = await client.GetAsync("https://agoodmovietowatch.com/random?netflix=1");
                string response = await result.Content.ReadAsStringAsync();
                response = response.Replace(Environment.NewLine, string.Empty).Replace("\n", string.Empty);

                Regex movieTitlePattern = new Regex("<h1>(.+?)<a>");
                Match movieTitleMatch = movieTitlePattern.Match(response);
                Group movieTitleGroup = movieTitleMatch.Groups[1];

                Regex movieYearPattern = new Regex(@"\((.+?)\)");
                Match movieYearMatch = movieYearPattern.Match(response);
                Group movieYearGroup = movieYearMatch.Groups[1];

                string movieTitle = movieTitleGroup.Value;
                string movieYear = movieYearGroup.Value;

                return (movieTitle, movieYear);
            }
        }
    }
}
