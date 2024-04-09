using System;
using DiscordRPC;

namespace QuestDiscordRPC.Handlers;

public class DiscordRPCHandler
{
    private readonly DiscordRpcClient _discordRpcClient;
    internal bool isReady { get; private set; }
    
    public DiscordRPCHandler()
    {
        _discordRpcClient = new DiscordRpcClient("1218222688028463224");

        _discordRpcClient.OnReady += (sender, e) =>
        {
            isReady = true;
            Console.WriteLine("DiscordRPC Ready!");
        };
        
        setNewPresece("in VR","Just Started Playing", new Assets());
            
        _discordRpcClient.Initialize();
    }

    internal void setNewPresece(string details, string state, Assets assets = null)
    {
        var presence = new RichPresence()
        {
            Details = details,
            State = state,
            Assets = assets,
            Timestamps = Timestamps.Now
        };
            
        _discordRpcClient.SetPresence(presence);
    }

    internal void clear() => _discordRpcClient.ClearPresence();
}