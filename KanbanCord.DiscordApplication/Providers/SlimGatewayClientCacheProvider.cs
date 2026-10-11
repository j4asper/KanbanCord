using KanbanCord.DiscordApplication.Services;
using NetCord.Gateway;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Providers;

public class SlimGatewayClientCacheProvider : IGatewayClientCacheProvider
{
    public IGatewayClientCache Create(ulong clientId, RestClient client)
    {
        return new SlimGatewayClientCache();
    }
}