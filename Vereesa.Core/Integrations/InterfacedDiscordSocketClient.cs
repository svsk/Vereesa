using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Vereesa.Core.Integrations.Interfaces;

namespace Vereesa.Core.Integrations 
{
    public class InterfacedDiscordSocketClient : IDiscordSocketClient
    {
        private DiscordSocketClient _client;

        public InterfacedDiscordSocketClient(DiscordSocketClient client) 
        {
            _client = client;
            _client.Ready += HandleDiscordReady;
            _client.MessageReceived += HandleMessageReceived;
        }

        private async Task HandleMessageReceived(SocketMessage arg)
        {
            await MessageReceived.Invoke(arg);
        }

        private async Task HandleDiscordReady()
        {
            await Ready.Invoke();
        }

        public event Func<Task> Ready;

        public event Func<SocketMessage, Task> MessageReceived;

        //Natives remapping

        public ConnectionState ConnectionState => _client.ConnectionState;

        public ISelfUser CurrentUser => _client.CurrentUser;

        public TokenType TokenType => _client.TokenType;

        public async Task<IGuild> CreateGuildAsync(string name, IVoiceRegion region, Stream jpegIcon = null, RequestOptions options = null) => await _client.CreateGuildAsync(name, region, jpegIcon, options);

        public void Dispose() => _client.Dispose();

        public async Task<IApplication> GetApplicationInfoAsync(RequestOptions options = null) => await _client.GetApplicationInfoAsync(options);

        public async Task<IChannel> GetChannelAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) => await ((IDiscordClient)_client).GetChannelAsync(id, mode, options);

        public async Task<IReadOnlyCollection<IConnection>> GetConnectionsAsync(RequestOptions options = null) => await _client.GetConnectionsAsync(options);
        
        public async Task<IReadOnlyCollection<IDMChannel>> GetDMChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) => await ((IDiscordClient)_client).GetDMChannelsAsync(mode, options);

        public async Task<IReadOnlyCollection<IGroupChannel>> GetGroupChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null)  => await ((IDiscordClient)_client).GetGroupChannelsAsync(mode, options);

        public async Task<IGuild> GetGuildAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) => await ((IDiscordClient)_client).GetGuildAsync(id, mode, options);

        public async Task<IReadOnlyCollection<IGuild>> GetGuildsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) => await ((IDiscordClient)_client).GetGuildsAsync(mode, options);

        public async Task<IInvite> GetInviteAsync(string inviteId, RequestOptions options = null) => await _client.GetInviteAsync(inviteId, options);

        public async Task<IReadOnlyCollection<IPrivateChannel>> GetPrivateChannelsAsync(CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) => await ((IDiscordClient)_client).GetPrivateChannelsAsync(mode, options);

        public async Task<IUser> GetUserAsync(ulong id, CacheMode mode = CacheMode.AllowDownload, RequestOptions options = null) => await ((IDiscordClient)_client).GetUserAsync(id, mode, options);

        public async Task<IUser> GetUserAsync(string username, string discriminator, RequestOptions options = null) => await ((IDiscordClient)_client).GetUserAsync(username, discriminator, options);

        public async Task<IVoiceRegion> GetVoiceRegionAsync(string id, RequestOptions options = null) => await ((IDiscordClient)_client).GetVoiceRegionAsync(id, options);

        public async Task<IReadOnlyCollection<IVoiceRegion>> GetVoiceRegionsAsync(RequestOptions options = null) => await ((IDiscordClient)_client).GetVoiceRegionsAsync(options);

        public async Task<IWebhook> GetWebhookAsync(ulong id, RequestOptions options = null) => await ((IDiscordClient)_client).GetWebhookAsync(id, options);

        public async Task StartAsync() => await _client.StartAsync();

        public async Task StopAsync() => await _client.StopAsync();

        public Task<int> GetRecommendedShardCountAsync(RequestOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}