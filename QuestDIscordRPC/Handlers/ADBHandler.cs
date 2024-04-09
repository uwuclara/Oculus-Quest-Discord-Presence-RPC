using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;

namespace QuestDiscordRPC.Handlers;

public class ADBHandler
{
    private readonly AdbClient _adbClient;
    private readonly IPAddress _ipAddress;
    private readonly DBHandler _dbHandler;
    private readonly StringHandler _stringHandler;
    private readonly PackageHandler _packageHandler;
    private DeviceData _deviceData;
    
    public ADBHandler(IPAddress ip_address, DBHandler dbHandler)
    {
        _ipAddress = ip_address;
        _dbHandler = dbHandler;
        _packageHandler = new PackageHandler(this);
        _stringHandler = new StringHandler(_packageHandler);
        
        if (!AdbServer.Instance.GetStatus().IsRunning)
        {
            var server = new AdbServer();
            var result = server.StartServer("cache/adb.exe", false);
            
            if (result != StartServerResult.Started)
            {
                Console.WriteLine("ERROR: Can't start adb server");
                return;
            }
        }
        
        _adbClient = new AdbClient();
        connect();
    }

    internal string getDeviceName()
    {
        var receiver = new ConsoleOutputReceiver();
        
        _adbClient.ExecuteRemoteCommand("getprop ro.product.model", _deviceData, receiver, Encoding.Default);

        var output = receiver.ToString();

        return output;
    }

    internal string getActivePackageName()
    {
        if (_deviceData.State == DeviceState.Offline)
        {
            connect();
            return null;
        }
        
        var receiver = new ConsoleOutputReceiver();
        
        _adbClient.ExecuteRemoteCommand("dumpsys window windows", _deviceData, receiver, Encoding.Default);

        var output = receiver.ToString();
        var packageName = _stringHandler.proccessActivePackageName(output);

        return packageName;
    }

    internal string getAPKPathFromPackageName(string packageName)
    {
        var receiver = new ConsoleOutputReceiver();
        
        _adbClient.ExecuteRemoteCommand("pm path " + packageName, _deviceData, receiver, Encoding.Default);

        var output = receiver.ToString();
        var colonIndex = output.IndexOf(':');
        var apkIndex = output.IndexOf(".apk", colonIndex);

        if (apkIndex != -1)
        {
            return output.Substring(colonIndex + 1, apkIndex + 4 - colonIndex).Trim();
        }
        else
        {
            return output;
        }
    }
    
    internal void downloadAPKFromPath(string apkPath, string downloadPath, Action<SyncProgressChangedEventArgs>? callback = null)
    {
        using (var service = new SyncService(_deviceData))
        {
            using (var stream = File.OpenWrite(downloadPath))
            {
                service.Pull(apkPath, stream, callback);
            }
        }
    }

    internal void connect()
    {
        try
        {
            _adbClient.Connect(_ipAddress);
            _deviceData = _adbClient.GetDevices().FirstOrDefault();
        
            Console.WriteLine(_deviceData.Serial + " connected!");
        }
        catch { }
    }
    
    internal DBHandler getDBHandler() => _dbHandler;
}