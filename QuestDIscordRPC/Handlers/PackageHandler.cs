using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RestSharp;
using SharpAapt;

namespace QuestDiscordRPC.Handlers;

public class PackageHandler
{
    private readonly ADBHandler _adbHandler;
    private readonly StringHandler _stringHandler;
    private List<string> downloadingPackages = new List<string>();
    
    public PackageHandler(ADBHandler adbHandler)
    {
        _adbHandler = adbHandler;
        
    }
    
    private static bool isRunning;
    private static bool wasCalled;
    internal async Task<string> getAppNameFromPackageName(string packageName)
    {
        var tcs = new TaskCompletionSource<string>();
        var dbObject = _adbHandler.getDBHandler().Select(packageName);

        if (dbObject != null) // if image wasnt set just clear cache fuck this
        {
            tcs.SetResult(dbObject.AppName);
        }
        else
        {
            if (isRunning)
            {
                tcs.SetResult("Caching App..");
            }
            else
            {
                if (!File.Exists("cache/" + packageName + ".apk"))
                {
                    if (downloadingPackages.Contains(packageName))
                    {
                        tcs.SetResult("Caching App..");
                       
                    }
                    else
                    {               
                        var apkPath = _adbHandler.getAPKPathFromPackageName(packageName);
                        if (string.IsNullOrWhiteSpace(apkPath))
                        {
                            _adbHandler.getDBHandler().Insert(packageName, packageName, "NOTFOUND");
                            tcs.SetResult(packageName);
                        }
                        else
                        {
                            isRunning = true;
                            downloadingPackages.Add(packageName);

                            wasCalled = false;
                        
                            _adbHandler.downloadAPKFromPath(apkPath, "cache/" + packageName + ".apk", async s =>
                            {
                                if (s.TotalBytesToReceive == 0)
                                {
                                    _adbHandler.getDBHandler().Insert(packageName, packageName, null);
                                }
                                else if (Math.Abs(s.ProgressPercentage - 100) < 0.001)
                                {
                                    await Task.Delay(500);

                                    if (!wasCalled)
                                    {
                                        wasCalled = true;
                        
                                        var appname = getAppNameFromAPK("cache/" + packageName + ".apk");

                                        if (string.IsNullOrWhiteSpace(appname)) appname = packageName;

                                        var imageURL = await extractAppIconAndUpload("cache/" + packageName + ".apk");
                                    
                                        _adbHandler.getDBHandler().Insert(packageName, appname, imageURL);
                                    
                                        File.Delete("cache/" + packageName + ".apk");
                                        downloadingPackages.Remove(packageName);
                                        isRunning = false;
                                        tcs.TrySetResult(appname);
                                    }
                                }
                            });
                        }
                    }
                }
                else
                {
                    if (downloadingPackages.Contains(packageName))
                    {
                        tcs.SetResult("Caching App..");
                    }
                    else
                    {
                        File.Delete("cache/" + packageName + ".apk");
                        tcs.SetResult("Caching App..");
                    }
                }
            }
        }
        
        return await tcs.Task;
    }
    
    internal static string? getAppNameFromAPK(string apkPath)
    {
        try
        {
            var aaptClient = new AaptClient();
            aaptClient.AaptPath = "cache/aapt.exe";
            
            var strings = aaptClient.GetBadgingString(apkPath);
            
            var applicationName = StringHandler.extractAppNameFromString(strings);

            return applicationName;
        }
        catch (Exception e)
        {
            Console.WriteLine("ERROR: Failed getting app name " + e);
        }

        return null;
    }

    internal static async Task<string> extractAppIconAndUpload(string apkPath)
    {
        string highestResolutionIconPath = null;
        var highestResolution = 0;

        using (var apkArchive = ZipFile.OpenRead(apkPath))
        {
            foreach (var entry in apkArchive.Entries)
            {
                if (string.Equals(entry.Name, "app_icon.png", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.Name, "icon.png", StringComparison.OrdinalIgnoreCase))
                {
                    using (var iconStream = entry.Open())
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await iconStream.CopyToAsync(memoryStream);

                            if (memoryStream.Length > highestResolution)
                            {
                                highestResolution = (int)memoryStream.Length;
                                highestResolutionIconPath = Path.Combine("cache", entry.Name);
                                
                                using (var fileStream = File.Create(highestResolutionIconPath))
                                {
                                    memoryStream.Position = 0;
                                    await memoryStream.CopyToAsync(fileStream);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (highestResolutionIconPath != null)
        {
            return await UploadIcon(highestResolutionIconPath);
        }

        return "NOTFOUND";
    }

    internal static async Task<string> UploadIcon(string iconPath)
    {
        var client = new RestClient("https://questdiscordrpc.uwuclara.dev");
        var request = new RestRequest("/", Method.Post);

        request.AddFile("file", iconPath);

        var response = await client.ExecuteAsync(request);

        if (response.IsSuccessful)
        {
            var pattern = @"<p id=""fileLink"">(.*?)<\/p>";
            
            if (response.Content == null) return null;
            
            var match = Regex.Match(response.Content, pattern);
            var fileLink = match.Success ? match.Groups[1].Value : null;
            
            File.Delete(iconPath);
            
            return fileLink;
        }
        else
        {
            Console.WriteLine("ERROR: Uploading gameimage " + response.StatusCode);
            
            return null;
        }
    }
}