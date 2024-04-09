using System;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using DiscordRPC;
using QuestDiscordRPC.Handlers;

namespace QuestDiscordRPC;

public class RPCMain
{
    public const string Name = "QuestDiscordRPC";
    public const string Version = "1.0.0";
    private const int refreshDelay = 3;
    private static string? lastGame;
    private static DBHandler _dbHandler;
    
    private static void Main(string[] args) => StartAsync(args).GetAwaiter().GetResult();
    
    private static async Task StartAsync(string[] args)
    {
        _dbHandler = new DBHandler();
        
        string ipAddress;
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide Quest's IP address or start this app with argument or press ENTER to load last KNOWN Quests IP address");
            ipAddress = Console.ReadLine();
        }
        else
        {
            ipAddress = args[0];
        }

        var dbobject = _dbHandler.Select("QuestLastIP");
            
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            if (dbobject == null) return;

            ipAddress = dbobject.AppName;
        }

        IPAddress ip;
        try
        {
            ip = IPAddress.Parse(ipAddress);
        }
        catch (Exception e)
        {
            Console.WriteLine("Invalid IP address format.");
            return;
        }

        if (dbobject == null)
        {
            _dbHandler.Insert("QuestLastIP", ipAddress, null);
        }
        else
        {
            _dbHandler.Update(dbobject.Id, dbobject.PackageName, ipAddress, null);
        }
       
        var adbHandler = new ADBHandler(ip, _dbHandler);
        var discordRPCHandler = new DiscordRPCHandler();
        
        var refreshTimer = new Timer(TimeSpan.FromSeconds(refreshDelay).TotalMilliseconds);
        refreshTimer.AutoReset = true;
        refreshTimer.Elapsed += (sender, e) => Refresh(sender, e, adbHandler, discordRPCHandler);
        refreshTimer.Start();
        
        await Task.Delay(-1);
    }

    private static void Refresh(object? state, ElapsedEventArgs elapsedEventArgs, ADBHandler adbHandler, DiscordRPCHandler discordRpcHandler)
    {
        if (!discordRpcHandler.isReady) return;

        var currentGame = adbHandler.getActivePackageName();
        
        if (currentGame != lastGame)
        {
            if (currentGame is null or "sleep")
            {
                lastGame = currentGame;
                discordRpcHandler.clear();
            }
            else
            {
                lastGame = currentGame;
            
                var dbobject = _dbHandler.Select(AppName: currentGame);
                string imageURL;
                if (dbobject != null && dbobject.ImageURL != "NOTFOUND")
                {
                    imageURL = dbobject.ImageURL;
                }
                else
                {
                    imageURL = "default";
                }
                
                discordRpcHandler.setNewPresece("Playing " + currentGame, "on " + adbHandler.getDeviceName(), 
                    new Assets(){LargeImageKey = imageURL, LargeImageText = Name + " v" + Version});
            }
        }
    }
}