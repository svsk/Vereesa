using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Moq;
using Shouldly;
using Vereesa.Core.Infrastructure;
using Vereesa.Neon.Data.Interfaces;
using Vereesa.Neon.Data.Models.Reminders;
using Xunit;

namespace Vereesa.Neon.Tests.ServicesTests
{
    public class RemindMeServiceTests
    {
        [Fact(Skip = "Wrong way to test")]
        public async Task CreatePeriodicReminder_WithChannelName_ParsesChannelCorrectly()
        {
            // Arrange
            Reminder reminder = null;
            var discordMock = new Mock<IChatClient>();
            var scheduler = new Mock<IJobScheduler>();
            var repo = new Mock<IRepository<Reminder>>();
            repo.Setup(r => r.AddAsync(It.IsAny<Reminder>()))
                .Callback<Reminder>(
                    (r) =>
                    {
                        reminder = r;
                    }
                );

            var interaction = new ChatInteractionBuilder(discordMock)
                .SendMessage("!reminder create periodic")
                .OnMessageContaining("How long between each reminder?")
                .RespondWith("2 hours")
                .OnMessageContaining("When should the first reminder be?")
                .RespondWith("2020-01-01 00:00:00")
                .OnMessageContaining("Who should I remind?")
                .RespondWith("Veinlash")
                .OnMessageContaining("What should the reminder say?")
                .RespondWith("This is a test reminder.")
                .OnMessageContaining("What channel should the reminder be sent to?")
                .RespondWith("<#123>")
                .OnMessageContaining("OK, I'll remind Veinlash")
                .Finish()
                .Build();

            // Act
            //var target = new RemindMeService(discordMock.Object, scheduler.Object, repo.Object);
            //await target.CreatePeriodicReminder(interaction.StartMessage);

            // Assert
            interaction.Resolved.ShouldBe(true);
            interaction.FinishedSuccessfully.ShouldBe(true);
            reminder.ShouldNotBeNull();
            reminder.RemindTime.ShouldBe(1577833200);
            reminder.Message.ShouldContain("This is a test reminder.");
        }
    }

    public class ChatInteractionBuilder : IChatInteraction
    {
        private Mock<IChatClient> _discordMock;
        public IMessage StartMessage { get; private set; }
        private string _resolveId = Guid.NewGuid().ToString();
        private string _currentSearchString = null;
        private string _firstMessage;

        public ChatInteractionBuilder(Mock<IChatClient> discordMock)
        {
            _discordMock = discordMock;
        }

        private List<(string searchString, string response)> _responses =
            new List<(string searchString, string response)>();

        public bool? FinishedSuccessfully { get; private set; }
        public bool Resolved { get; private set; } = false;

        public ChatInteractionBuilder SendMessage(string message)
        {
            _firstMessage = message;
            return this;
        }

        public ChatInteractionBuilder OnMessageContaining(string awaitedResponseContent)
        {
            _currentSearchString = awaitedResponseContent;
            return this;
        }

        public ChatInteractionBuilder RespondWith(string responseMessage)
        {
            _responses.Add((_currentSearchString, responseMessage));
            return this;
        }

        public ChatInteractionBuilder Finish()
        {
            _responses.Add((_currentSearchString, _resolveId));
            return this;
        }

        private string FindResponse(string message)
        {
            var hit = _responses.FirstOrDefault(resp => message.Contains(resp.searchString));
            if (hit == default)
            {
                throw new Exception($"Could not find a valid response to: {message}");
            }

            return hit.response;
        }

        private void Resolve(bool succeeded)
        {
            Resolved = true;
            FinishedSuccessfully = succeeded;
        }

        public IChatInteraction Build()
        {
            var message = new Mock<IMessage>();
            var author = new Mock<IUser>();
            var channel = new Mock<IMessageChannel>();

            ulong userId = 12345678910;
            author.Setup(a => a.Mention).Returns($"<@{userId}>");
            author.Setup(a => a.Id).Returns(userId);

            channel
                .Setup(
                    c =>
                        c.SendMessageAsync(
                            It.IsAny<string>(),
                            false,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            MessageFlags.None,
                            null
                        )
                )
                .Callback<string, bool, Embed, RequestOptions, AllowedMentions, MessageReference>(
                    (message, isTTS, embed, requestOptions, allowedMentions, messageReference) =>
                    {
                        var response = FindResponse(message);

                        if (response != _resolveId)
                        {
                            var responseMessage = new Mock<IMessage>();
                            responseMessage.Setup(m => m.Channel).Returns(channel.Object);
                            responseMessage.Setup(m => m.Author).Returns(author.Object);
                            responseMessage.Setup(m => m.Content).Returns(response);
                            _discordMock.Raise(e => e.MessageReceived += null, responseMessage.Object);
                        }
                        else
                        {
                            Resolve(true);
                        }
                    }
                );

            message.Setup(m => m.Channel).Returns(channel.Object);
            message.Setup(m => m.Author).Returns(author.Object);
            message.Setup(m => m.Content).Returns(_firstMessage);

            this.StartMessage = message.Object;
            return this;
        }
    }

    public interface IChatInteraction
    {
        IMessage StartMessage { get; }

        bool Resolved { get; }

        bool? FinishedSuccessfully { get; }
    }

    public interface IChatClient
    {
        event Func<IUser, IUser, Task> GuildMemberUpdated;

        event Func<Task> Ready;

        event Func<IMessage, Task> MessageReceived;

        IMessageChannel GetChannel(ulong channelId);

        IGuild GetGuild(ulong guildId);

        IRole GetRole(ulong roleId);
    }
}
