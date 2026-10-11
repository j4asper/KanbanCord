using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using NetCord;
using NetCord.Gateway;
using NetCord.JsonModels;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Services;

/// <summary>
/// Based on https://github.com/NetCordDev/NetCord/blob/main/NetCord/Gateway/ConcurrentGatewayClientCache.cs
/// With almost everything disabled
/// </summary>
public class SlimGatewayClientCache : IGatewayClientCache
{
    private static readonly IReadOnlyDictionary<ulong, IGuildChannel> EmptyChannels = FrozenDictionary<ulong, IGuildChannel>.Empty;
    private static readonly IReadOnlyDictionary<ulong, Role> EmptyRoles = FrozenDictionary<ulong, Role>.Empty;
    private static readonly IReadOnlyDictionary<ulong, GuildEmoji> EmptyEmojis = FrozenDictionary<ulong, GuildEmoji>.Empty;
    private static readonly IReadOnlyDictionary<ulong, GuildSticker> EmptyStickers = FrozenDictionary<ulong, GuildSticker>.Empty;
    private static readonly IReadOnlyDictionary<ulong, GuildThread> EmptyActiveThreads = FrozenDictionary<ulong, GuildThread>.Empty;
    private static readonly IReadOnlyDictionary<ulong, GuildUser> EmptyUsers = FrozenDictionary<ulong, GuildUser>.Empty;
    private static readonly IReadOnlyDictionary<ulong, VoiceState> EmptyVoiceStates = FrozenDictionary<ulong, VoiceState>.Empty;
    private static readonly IReadOnlyDictionary<ulong, GuildScheduledEvent> EmptyScheduledEvent = FrozenDictionary<ulong, GuildScheduledEvent>.Empty;

    private static bool jsonModelUnavailable;

    private readonly ConcurrentDictionary<ulong, Guild> guilds = [];
    
    public CurrentUser? User { get; private set; }
    public IReadOnlyDictionary<ulong, Guild> Guilds => guilds;
    
    public IReadOnlyDictionary<TKey, TValue> CreateDictionary<TSource, TKey, TValue>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector)
        where TKey : notnull
        where TValue : class
    {
        return CreateConcurrentDictionary(source, keySelector, elementSelector);
    }

    private static ConcurrentDictionary<TKey, TValue> CreateConcurrentDictionary<TSource, TKey, TValue>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector)
        where TKey : notnull
        where TValue : class
    {
        return new(source.Select(s => new KeyValuePair<TKey, TValue>(keySelector(s), elementSelector(s))));
    }

    public void Dispose()
    {
        guilds.Clear();
    }

    public IGatewayClientCache CacheGuild(Guild guild)
    {
        guild.Channels = EmptyChannels;
        guild.Roles = EmptyRoles;
        guild.Emojis = EmptyEmojis;
        guild.Stickers = EmptyStickers;
        guild.ActiveThreads = EmptyActiveThreads;
        guild.Users = EmptyUsers;
        guild.VoiceStates = EmptyVoiceStates;
        guild.ScheduledEvents = EmptyScheduledEvent;

        TrimJsonModel(guild);

        guilds[guild.Id] = guild;

        return this;
    }

    private static void TrimJsonModel(Guild guild)
    {
        if (jsonModelUnavailable)
            return;

        try
        {
            var jsonModel = GetJsonModel(guild);

            jsonModel.Roles = [];
            jsonModel.Emojis = [];
            jsonModel.Stickers = [];
            jsonModel.Channels = [];
            jsonModel.ActiveThreads = [];
            jsonModel.Users = [];
            jsonModel.VoiceStates = [];
            jsonModel.Presences = [];
            jsonModel.StageInstances = [];
            jsonModel.ScheduledEvents = [];
        }
        catch (MissingFieldException)
        {
            jsonModelUnavailable = true;
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_jsonModel")]
    private static extern ref JsonGuild GetJsonModel(RestGuild guild);

    public IGatewayClientCache CacheCurrentUser(CurrentUser user)
    {
        User = user;
        
        return this;
    }
    
    public IGatewayClientCache SyncGuilds(IReadOnlyList<ulong> guildIds)
    {
        foreach (var guildId in guilds.Keys.Except(guildIds))
            guilds.TryRemove(guildId, out _);

        return this;
    }

    public IGatewayClientCache RemoveGuild(ulong guildId)
    {
        guilds.TryRemove(guildId, out _);
        
        return this;
    }
    
    # region Disabled caching methods
    public IGatewayClientCache CacheGuildUser(GuildUser user) => this;
    public IGatewayClientCache CacheGuildUsers(ulong guildId, IReadOnlyList<GuildUser> users) => this;
    public IGatewayClientCache CachePresences(ulong guildId, IReadOnlyList<Presence> presences) => this;
    public IGatewayClientCache CacheRole(Role role) => this;
    public IGatewayClientCache CacheGuildScheduledEvent(GuildScheduledEvent scheduledEvent) => this;
    public IGatewayClientCache CacheGuildThread(GuildThread thread) => this;
    public IGatewayClientCache CacheGuildChannel(IGuildChannel channel) => this;
    public IGatewayClientCache CacheStageInstance(StageInstance stageInstance) => this;
    public IGatewayClientCache CacheVoiceState(VoiceState voiceState) => this;
    public IGatewayClientCache CachePresence(Presence presence) => this;
    public IGatewayClientCache SyncGuildEmojis(ulong guildId, IReadOnlyDictionary<ulong, GuildEmoji> emojis) => this;
    public IGatewayClientCache SyncGuildStickers(ulong guildId, IReadOnlyDictionary<ulong, GuildSticker> stickers) => this;
    public IGatewayClientCache SyncGuildActiveThreads(ulong guildId, IReadOnlyList<ulong>? channelIds, IReadOnlyDictionary<ulong, GuildThread> threads) => this;
    # endregion
    
    # region Disabled remove methods
    public IGatewayClientCache RemoveRole(ulong guildId, ulong roleId) => this;
    public IGatewayClientCache RemoveGuildScheduledEvent(ulong guildId, ulong scheduledEventId) => this;
    public IGatewayClientCache RemoveGuildThread(ulong guildId, ulong threadId) => this;
    public IGatewayClientCache RemoveGuildChannel(ulong guildId, ulong channelId) => this;
    public IGatewayClientCache RemoveStageInstance(ulong guildId, ulong stageInstanceId) => this;
    public IGatewayClientCache RemoveVoiceState(ulong guildId, ulong userId) => this;
    public IGatewayClientCache RemoveGuildUser(ulong guildId, ulong userId) => this;
    # endregion
}