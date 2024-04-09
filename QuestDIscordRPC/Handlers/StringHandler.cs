using System;
using System.Linq;

namespace QuestDiscordRPC.Handlers;

public class StringHandler
{
    private readonly PackageHandler _packageHandler;
    
    public StringHandler(PackageHandler packageHandler)
    {
        _packageHandler = packageHandler;
    }
    
    internal string proccessActivePackageName(string data)
    {
        
        var mObscuringWindowLine = "mObscuringWindow";
        var mTopFocusedDisplayIdLine = "mTopFocusedDisplayId";
        
        var globalConfigurationIndex = data.IndexOf(mTopFocusedDisplayIdLine);
        var mObscuringWindowLineIndex = data.IndexOf(mObscuringWindowLine);
        
        var lineObscuringWindowStartIndex = data.LastIndexOf('\n', mObscuringWindowLineIndex) + 1;
        var lineObscuringWindowEndIndex = data.IndexOf('\n', mObscuringWindowLineIndex);
        
        var outputObscuringWindowLine = data.Substring(lineObscuringWindowStartIndex, lineObscuringWindowEndIndex - lineObscuringWindowStartIndex);

        if (globalConfigurationIndex != -1)
        {
            if (!outputObscuringWindowLine.Contains("null"))
            {
                var uObscuringWindowIndex = outputObscuringWindowLine.IndexOf(" u");
                
                var startObscuringWindowIndex = uObscuringWindowIndex + 2;

                if (startObscuringWindowIndex < outputObscuringWindowLine.Length && char.IsDigit(outputObscuringWindowLine[startObscuringWindowIndex]))
                {
                    var endIndex = outputObscuringWindowLine.IndexOf("/", startObscuringWindowIndex);

                    if (endIndex != -1)
                    {
                        var packageName = outputObscuringWindowLine.Substring(startObscuringWindowIndex, endIndex - startObscuringWindowIndex);
                        var spaceIndex = packageName.IndexOf(' ');
    
                        if (spaceIndex != -1)
                        {
                            packageName = packageName.Substring(spaceIndex + 1);
                        }

                        if (packageName is "com.oculus.shellenv" or "com.oculus.systemux")
                        {
                            return "Oculus Home";
                        }
    
                        var appName = _packageHandler.getAppNameFromPackageName(packageName).Result;

                        return appName;
                    }
                }
            }
            
            var substring = data.Substring(globalConfigurationIndex);
            var idStartIndex = substring.IndexOf('=') + 1;
            var idEndIndex = substring.IndexOf(' ', idStartIndex);
            var idString = substring.Substring(idStartIndex, idEndIndex - idStartIndex);

            if (int.TryParse(idString, out var id))
            {
                var imeLayeringTargetLine = "imeInputTarget in display# " + id;
                var imeLayeringTargetIndex = data.IndexOf(imeLayeringTargetLine);
                
                var imeInputTargetLine = "imeLayeringTarget in display# " + id;
                var imeInputTargetIndex = data.IndexOf(imeInputTargetLine);
                
                var mHoldScreenWindowLine = "mHoldScreenWindow";
                    
                var mHoldScreenWindowIndex = data.IndexOf(mHoldScreenWindowLine);
                var mHoldScreenWindowStartIndex = data.LastIndexOf('\n', mHoldScreenWindowIndex) + 1;
                    
                var mHoldScreenWindowEndIndex = data.IndexOf('\n', mHoldScreenWindowIndex);
                var outputmHoldScreenWindowLine = data.Substring(mHoldScreenWindowStartIndex, mHoldScreenWindowEndIndex - mHoldScreenWindowStartIndex);

                if (imeInputTargetIndex != -1 || (imeLayeringTargetIndex > -1 && id != 0) || (imeLayeringTargetIndex > -1 && id == 0 && !outputmHoldScreenWindowLine.Contains("null")))
                {
                    if (imeInputTargetIndex == -1)
                        imeInputTargetIndex = imeLayeringTargetIndex;
                    
                    var lineStartIndex = data.LastIndexOf('\n', imeInputTargetIndex) + 1;
                    var lineEndIndex = data.IndexOf('\n', imeInputTargetIndex);
                    var outputLine = data.Substring(lineStartIndex, lineEndIndex - lineStartIndex);
                    var uIndex = outputLine.IndexOf(" u");

                    while (uIndex != -1)
                    {
                        var startIndex = uIndex + 2;

                        if (startIndex < outputLine.Length && char.IsDigit(outputLine[startIndex]))
                        {
                            var endIndex = outputLine.IndexOf("/", startIndex);
        
                            if (endIndex != -1)
                            {
                                var packageName = outputLine.Substring(startIndex, endIndex - startIndex);
                                var spaceIndex = packageName.IndexOf(' ');
            
                                if (spaceIndex != -1)
                                {
                                    packageName = packageName.Substring(spaceIndex + 1);
                                }

                                if (packageName is "com.oculus.shellenv" or "com.oculus.systemux")
                                {
                                    return "Oculus Home";
                                }
            
                                var appName = _packageHandler.getAppNameFromPackageName(packageName).Result;

                                return appName;
                            }
                            else
                            {
                                Console.WriteLine("ERROR: App name not found.");
                            }
                        }

                        uIndex = outputLine.IndexOf(" u", uIndex + 1);
                        if (uIndex == -1)
                        {
                            break;
                        }
                    }

                    Console.WriteLine("ERROR: Oculus User Index not found.");
                }
                else
                {
                    if (id == 0 && outputObscuringWindowLine.Contains("null") && outputmHoldScreenWindowLine.Contains("null"))
                    {
                        return "sleep";
                    }
                    else
                    {
                        return "Oculus Home";
                    }
                }
            }
            else
            {
                Console.WriteLine("ERROR: Can not find focused window");
            }
        }
        
        return null;
    }
    
    internal static string? extractAppNameFromString(string data)
    {
        var lines = data.Split('\n');

        return (
            from line 
                in lines
            select line.Trim() 
            into trimmedLine 
            where trimmedLine.StartsWith("application-label:") 
            let startIndex = trimmedLine.IndexOf("'") 
            let endIndex = trimmedLine.LastIndexOf("'") 
            where startIndex != -1 && endIndex != -1 && endIndex > startIndex 
            select trimmedLine.Substring(startIndex + 1, endIndex - startIndex - 1)).FirstOrDefault();
    }
}