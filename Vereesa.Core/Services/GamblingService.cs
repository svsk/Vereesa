using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord.WebSocket;
using Vereesa.Core.Configuration;
using Vereesa.Core.Extensions;
using Vereesa.Core.Helpers;
using Vereesa.Data.Interfaces;
using Vereesa.Data.Models.Gambling;
using Vereesa.Data.Repositories;

namespace Vereesa.Core.Services
{
    public class GamblingService
    {
        private GamblingSettings _settings;
        private DiscordSocketClient _discord;
        private Random _rng;
        private IRepository<GamblingStandings> _standings;
        private GamblingRound _currentRound;
        private ISocketMessageChannel _gamblingChannel;
        private List<string> _gameCommands = new List<string> { "!roll", "!jb" };
        private List<string> _outOfGameCommands = new List<string> { "!startbet", "!hallofshame", "!halloffame" };
        private Timer _awaitingRollsTimer;
        private Timer _rollRemindTimer;
        private Timer _roundTimeoutTimer;

        public GamblingService(GamblingSettings settings, DiscordSocketClient discord, Random rng, IRepository<GamblingStandings> standings)
        {
            _settings = settings;
            _discord = discord;
            _rng = rng;
            _standings = standings;

            _discord.Ready += StartGamblingService;
            _discord.MessageReceived += EvaluateMessage;
        }

        

        private async Task StartGamblingService()
        {
            //_gamblingChannel = guild.GetChannelByName(_settings.GamblingChannelName);
            _gamblingChannel = (ISocketMessageChannel)(await _discord.GetGuildChannelByNameAsync("Neon", _settings.GamblingChannelName));
        }

        private async Task EvaluateMessage(SocketMessage message)
        {
            if (message.Channel.Id != _gamblingChannel.Id)
                return;

            var command = message.Content.GetCommand();

            if (_currentRound != null && _gameCommands.Contains(command))
            {
                await HandleGameCommand(command, message);
            }

            if (_currentRound == null && _outOfGameCommands.Contains(command))
            {
                await HandleOutOfGameCommand(command, message);
            }
        }

        private async Task HandleGameCommand(string command, SocketMessage message)
        {
            switch (command)
            {
                case "!jb":
                    await JoinBet(message.Author.Id);
                    break;
                case "!roll":
                    await Roll(message.Author.Id);
                    await EndRoundIfAllHaveRolled();
                    break;
            }
        }

        private async Task EndRoundIfAllHaveRolled()
        {
            if (_currentRound.Rolls.Any(roll => roll.Value == null))
                return;

            await EndRound();
        }

        private async Task JoinBet(ulong userId)
        {
            if (!_currentRound.Rolls.ContainsKey(userId))
                _currentRound.Rolls.Add(userId, null);
            else
                await _gamblingChannel.SendMessageAsync($"You're already in the current round, <@{userId}>.");
        }

        private async Task HandleOutOfGameCommand(string command, SocketMessage message)
        {
            switch (command)
            {
                case "!startbet":
                    var maxValue = message.Content.Split(' ').Skip(1).First();
                    await StartNewBet(maxValue);
                    break;
                case "!halloffame":
                    await ShowHallOfFame();
                    break;
                case "!hallofshame":
                    await ShowHallOfShame();
                    break;
            }
        }

        private async Task Roll(ulong userId)
        {
            if (!_currentRound.Rolls.ContainsKey(userId)) //not part of the round
                return;

            if (_currentRound.Rolls[userId] != null) //already rolled
                return;

            if (!_currentRound.AwaitingRolls) //round rolling has not begun
                return;

            var roll = _rng.Next(1, _currentRound.MaxValue);
            _currentRound.Rolls[userId] = roll;

            await _gamblingChannel.SendMessageAsync($"<@{userId}> rolled {roll}.");
        }

        private async Task ShowHallOfShame()
        {
            var standings = _standings.GetAll().FirstOrDefault();

            if (standings == null)
                return;

            var shamers = string.Join(Environment.NewLine, standings.Ranking.Where(player => player.Value < 0).OrderBy(player => player.Value).Select(player => $"<@{player.Key}> {player.Value}g"));
            await _gamblingChannel.SendMessageAsync(shamers);
        }

        private async Task ShowHallOfFame()
        {
            var standings = _standings.GetAll().FirstOrDefault();

            if (standings == null)
                return;

            var famers = string.Join(Environment.NewLine, standings.Ranking.Where(player => player.Value > 0).OrderByDescending(player => player.Value).Select(player => $"<@{player.Key}> +{player.Value}g"));
            await _gamblingChannel.SendMessageAsync(famers);
        }

        private async Task StartNewBet(string maxValue)
        {
            if (!int.TryParse(maxValue, out var maxRollValue))
                return;

            _currentRound = GamblingRound.CreateInstance(maxRollValue);
            await _gamblingChannel.SendMessageAsync($"New bet (Max roll value: {maxRollValue}) started. Type `!jb` to join this round. Starting in 15 seconds.");

            _awaitingRollsTimer = TimerHelpers.SetTimeout(async () => { await StartAwaitingRolls(); }, 15000);
            _rollRemindTimer = TimerHelpers.SetTimeout(async () => { await RemindRollers(); }, 30000);
            _roundTimeoutTimer = TimerHelpers.SetTimeout(async () => { await EndRound(); }, 45000);
        }

        private async Task StartAwaitingRolls()
        {
            _currentRound.AwaitingRolls = true;
            await _gamblingChannel.SendMessageAsync("Start rolling now! (Use !roll to roll).");

            // //todo: remove this test line
            // _currentRound.Rolls.Add(123456, _rng.Next(1, _currentRound.MaxValue));
        }

        private async Task EndRound()
        {
            if (_currentRound == null)
                return;

            var ranking = _currentRound.Rolls.Where(roll => roll.Value != null).OrderByDescending(roll => roll.Value);

            if (ranking.Any())
            {
                var minRoll = ranking.Last().Value;
                var maxRoll = ranking.First().Value;

                var winningRolls = ranking.Where(roll => roll.Value == maxRoll);
                var losingRolls = ranking.Where(roll => roll.Value == minRoll);
                var nullRollers = _currentRound.Rolls.Where(roll => roll.Value == null).ToList();

                if (nullRollers.Any())
                {
                    losingRolls = nullRollers;
                }

                var prize = maxRoll.Value - (losingRolls.First().Value ?? 0);
                var losersString = string.Join(", ", losingRolls.Select(roller => $"<@{roller.Key}>"));
                var winnersString = string.Join(", ", winningRolls.Select(roller => $"<@{roller.Key}>"));

                await _gamblingChannel.SendMessageAsync($"{losersString} owes {winnersString} {prize} gold.");
                
                UpdateStandings(winningRolls, losingRolls, prize);
            }
            else
            {
                await _gamblingChannel.SendMessageAsync($"Round ended with no rolls. Type `!startbet <max roll value>` to start a new bet.");
            }

            _currentRound = null;
            _awaitingRollsTimer?.Stop();
            _rollRemindTimer?.Stop();
            _roundTimeoutTimer?.Stop();
        }

        private void UpdateStandings(IEnumerable<KeyValuePair<ulong, int?>> winningRolls, IEnumerable<KeyValuePair<ulong, int?>> losingRolls, int prize)
        {
            var standings = _standings.GetAll().FirstOrDefault();

            if (standings == null)
            {
                standings = new GamblingStandings();
                _standings.Add(standings);
            }

            foreach (var roll in winningRolls) {
                if (!standings.Ranking.ContainsKey(roll.Key)) {
                    standings.Ranking.Add(roll.Key, 0);
                }

                standings.Ranking[roll.Key] += prize;
            }

            foreach (var roll in losingRolls) {
                if (!standings.Ranking.ContainsKey(roll.Key)) {
                    standings.Ranking.Add(roll.Key, 0);
                }

                standings.Ranking[roll.Key] -= prize;
            }

            _standings.Save();
        }

        private async Task RemindRollers()
        {
            if (_currentRound == null)
                return;

            var nonRollers = string.Join(", ", _currentRound.Rolls.Where(roller => roller.Value == null).Select(roller => $"<@{roller.Key}>"));
            await _gamblingChannel.SendMessageAsync($"Still waiting for {nonRollers} to roll. Round will end in 15 seconds and you will be considered losers if you do not roll.");
        }
    }
}