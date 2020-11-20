using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NodaTime;
using Vereesa.Core.Extensions;
using Vereesa.Core.Infrastructure;

namespace Vereesa.Core.Services
{
	public class BronjahmService : BotServiceBase
	{
		private readonly IJobScheduler _scheduler;
		const ulong _bagBoiRole = 776483894269575189;

		public BronjahmService(DiscordSocketClient discord, IJobScheduler scheduler) 
			: base(discord)
		{
			_scheduler = scheduler;
			//ScheduleNextNotifications();
		}

		private const int _cycleDurationInSeconds = 200 * 60;
		private const int _spawnRateInSeconds = 10 * 60;
		private const int _activationTimeInSeconds = (60 * 2);
		private Instant _bronjahmBaselineSpawnTime = Instant.FromUnixTimeSeconds(1605546600 + (60 + 47));
		private static readonly string[] _rares = new string[] 
		{
			"Bronjahm",
			"Scourgelord Tyrannus",
			"Forgemaster Garfrost",
			"Marwyn",
			"Falric",
			"The Prophet Tharon'ja",
			"Novos the Summoner",
			"Trollgore",
			"Krik'thir the Gatewatcher",
			"Prince Taldaram",
			"Elder Nadox",
			"Noth the Plaguebringer",
			"Patchwerk",
			"Blood Queen Lana'thel",
			"Professor Putricide",
			"Lady Deathwhisper",
			"Skadi the Ruthless",
			"Ingvar the Plunderer",
			"Prince Keleseth",
			"The Black Knight",
		};
		
		private IMessageChannel Channel => ((IMessageChannel)this.Discord.GetChannel(124246560178438145));

		private Instant Now() => SystemClock.Instance.GetCurrentInstant();		
		

		private Instant GetNextBronjahmSpawnRelativeTo(Instant instant) 
		{
			var currentPointInCycle = ((instant - _bronjahmBaselineSpawnTime).TotalSeconds % _cycleDurationInSeconds);
			return instant.Plus(Duration.FromSeconds(_cycleDurationInSeconds - currentPointInCycle));
		}

		[OnCommand("!bagboi")]
		public async Task ShowNextSpawn(IMessage message) 
		{
			var now = Now();
			var nextSpawnInstant = GetNextBronjahmSpawnRelativeTo(now);
			var nextSpawn = new ZonedDateTime(nextSpawnInstant, DateTimeZoneProviders.Tzdb["Europe/Paris"]);
			var timeUntilNextSpawn = nextSpawnInstant - now;
			var response = $":handbag: Bag boi will spawn at **{nextSpawn.ToPrettyTime()} server time**." + 
				$"That is **{timeUntilNextSpawn.ToPrettyDuration()}** from now." +
				"\n\n:handbag: If you want me to ping you a couple of times before he spawns type `!join bagbois`";
			
			await message.Channel.SendMessageAsync(response);
		}

		[OnCommand("!nextrare")]
		public async Task ShowNextRare(IMessage request) 
		{
			var now = Now();
			var pointInCycle = ((now - _bronjahmBaselineSpawnTime).TotalSeconds - _activationTimeInSeconds) 
				% _cycleDurationInSeconds;
			var nextRareIndex = (int)Math.Ceiling(pointInCycle / _spawnRateInSeconds);
			nextRareIndex = nextRareIndex > _rares.Length - 1 ? 0 : nextRareIndex;
			var currentCycleStart = now.Minus(Duration.FromSeconds(pointInCycle));
			var spawnTime = currentCycleStart.Plus(Duration.FromSeconds(nextRareIndex * _spawnRateInSeconds))
				.AsServerTime();
			
			var responseText = "The next rare to spawn in :snowflake: Icecrown should be " +
				$":crown: **{_rares[nextRareIndex]}** (activates at {spawnTime.ToPrettyTime()}).";

			await request.Channel.SendMessageAsync(responseText);
		}

		// [OnReady]
		[OnCommand("!test")]
		[AsyncHandler]
		public async Task Test(IMessage message) 
		{
			var members = Discord.GetRolesByName("bagbois").First().Members.ToList();
			
			
		}

		private void ScheduleNextNotifications()
		{
			var now = Now();
			var nextSpawn = GetNextBronjahmSpawnRelativeTo(now);
			var firstWarning = nextSpawn.Minus(Duration.FromMinutes(15));
			var secondWarning = nextSpawn.Minus(Duration.FromMinutes(5));
			
			if (now < firstWarning) 
			{
				var firstWarningMessage = $"{_bagBoiRole.MentionRole()} " +
					"Bag boi spawns in 15 minutes!\n"+
					"\nType `!join bagbois` to be notified." +
					"\nType `!leave bagbois` to stop notifications.";

				

				_scheduler.Schedule(firstWarning, async () => {
				 	await Channel
					 .SendMessageAsync(firstWarningMessage);
				});
			}

			if (now < secondWarning) 
			{
				var secondWarningMessage = $"{_bagBoiRole.MentionRole()} " +
					"Bag boi spawns in 5 minutes!\n"+
					"\nType `!join bagbois` to be notified." +
					"\nType `!leave bagbois` to stop notifications.";

				_scheduler.Schedule(secondWarning, async () => {
				 	await Channel
					 .SendMessageAsync(secondWarningMessage);
				});
			}

			_scheduler.Schedule(nextSpawn, async () => {
				await AnnounceBronjahmSpawnAsync();
				ScheduleNextNotifications();
			});
		}

		private async Task AnnounceBronjahmSpawnAsync() 
		{
			var nextSpawnInstant = GetNextBronjahmSpawnRelativeTo(Now());
			var nextSpawn = nextSpawnInstant.AsServerTime();

			var activationTime = Now() + Duration.FromSeconds(_activationTimeInSeconds);
			
			Func<Task> updateMessage = null;
			updateMessage = await Channel.SendDynamicMessage(() => {
				var remaining = activationTime - Now();

				if (remaining <= Duration.FromSeconds(0)) 
				{
					_scheduler.EveryTenSeconds -= updateMessage;	
				}

				return $"{_bagBoiRole.MentionRole()}\n\n" +
				(
					remaining <= Duration.FromSeconds(0) 
						? $":skull: Bag boi has spawned and most likely killed!" 
						: $":clock10: Bag boi has spawned and will activate in **{remaining.ToPrettyDuration()}**!"
				) +
				"\n\nType `!join bagbois` to be notified about the next spawn!" +
				$"\nNext bag boi spawn is at **{nextSpawn.ToPrettyTime()} server time.**";
			});

			_scheduler.EveryTenSeconds += updateMessage;
		}
	}
}