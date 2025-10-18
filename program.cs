using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.ComponentModel.Design;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

class Program
{
    const int HWND_TOPMOST = -1;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
    static bool iomessage = false;
    static readonly string targetProcessName = "RobloxPlayerBeta";
    static readonly HttpClient client = new HttpClient();
    static readonly string version = "RTT production V1.7.0";
    static bool skip = false;
    static bool printbool = false;
    static string localid = "";
    static string gameid = "";
    static string jobid = "";
    static string universeid = "";
    static List<string> playerlist = new() { };
    static bool allowrejoin = false;
    static bool jobrejoin = false;
    static bool exit = false;
    static string robloxfolder = "";
    static FileInfo? logFileInfo = null;
    static string currentLogFile = "";
    static long lastPosition = 0;
    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        int hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    static async Task Main()
    {
        
        // Make console always on top
        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
        {
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
        }

        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "logs");
        Console.Title = "Roblox TurboTool";
        Console.WriteLine($"[Info] Watching folder: logfolder\n");
        Console.WriteLine($"[Info] enter 'skip' to use the newest file found\n");
        Task twhile1 = Task.Run(while1);
        Task twhile2 = Task.Run(while2);
        
        await Task.WhenAll(twhile1, twhile2);
        // Wait for recent enough log file (by LastWriteTime)
         async Task while1() { 
        while (true)
        {
                
            try
                {
                  logFileInfo = new DirectoryInfo(logDirectory)
                  .GetFiles("*.log")
                  .Where(f =>
                  f.Name.Contains("Player", StringComparison.OrdinalIgnoreCase) &&
                  f.LastWriteTime <= DateTime.Now)
                  .OrderByDescending(f => f.LastWriteTime)
                  .FirstOrDefault();

                    if (logFileInfo != null && logFileInfo.LastWriteTime.AddSeconds(15) > DateTime.Now)
                    {
                        Console.WriteLine($"[Info] Found recent log: {logFileInfo.Name}, press enter to continue");
                        currentLogFile = logFileInfo.FullName;
                        skip = true;
                        break;
                    }
                    if (skip == true)
                    {
                        currentLogFile = logFileInfo.FullName;
                        break;
                    }
                    var newest = logFileInfo != null ? logFileInfo.Name : "<none>";
                    Console.WriteLine($"[Wait] No recent log found (newest: {newest}), retrying...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] {ex.Message}");
                }
                
                await Task.Delay(1000);
            }
        }
        static async Task while2()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (command.ToLower() == "skip")
                {
                    skip = true;
                    break;
                } else if (skip == true)
                {
                    break;
                }
            }
        }


            Console.Clear();
        await Task.Delay(1000);
        Console.WriteLine("Running" +" "+ version);
        Console.WriteLine($"\n[Monitor] Now Displaying Log: {Path.GetFileName(currentLogFile)}\n\n");
        Console.WriteLine("Enter 'help' to get a list of commands (and sometimes an explanation).\n");
        static string ExtractBetweenMarkers(string text, char startMarker, char endMarker)
        {
            int startIndex = text.IndexOf(startMarker);
            int endIndex = text.IndexOf(endMarker);

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
            {

                return text.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
            }

            return string.Empty; // or return original text, or throw an error
        }
        Task task1 = Task.Run(whileone);
        Task task2 = Task.Run(whiletwo);

        await Task.WhenAll(task1, task2);
        await Task.Delay(-1);

        static async void whileone()
        {
            while (true)
            {
                Process[] processes = Process.GetProcessesByName(targetProcessName);
                try
                {
                    var fileInfoOnDisk = new FileInfo(currentLogFile);

                    if (fileInfoOnDisk.Length > lastPosition)
                    {
                        using var fs = new FileStream(currentLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fs.Seek(lastPosition, SeekOrigin.Begin);

                        using var reader = new StreamReader(fs);
                        string? line;

                        while ((line = reader.ReadLine()) != null && !line.Contains("System"))
                        {

                            if (line.Contains("[ExpChat/mountClientApp (Trace)]"))
                            {
                                //[ExpChat/mountClientApp (Trace)] - Player added: must201234 1598581117
                                Console.ForegroundColor = ConsoleColor.Green;
                                string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                printbool = true;
                                string marker = "[ExpChat/mountClientApp (Trace)] - ";

                                int index = line.IndexOf(marker);
                                if (line.Contains("Player Removed: " + localid))
                                {
                                    Console.WriteLine(timestamp + " LocalPlayer Left game.");
                                }
                                if (index >= 0)
                                {
                                    // Remove everything before and including marker, then trim leading spaces
                                    string cleaned = line.Substring(index + marker.Length).TrimStart();
                                    Console.WriteLine(timestamp + " " + cleaned);
                                    if (cleaned.Contains("Player added:"))
                                    {
                                        string plrlistentry = cleaned.Substring(13).TrimStart();
                                        playerlist.Add(plrlistentry);
                                    } else if (cleaned.Contains("Player removed:"))
                                    {
                                        string plrlistentry = cleaned.Substring(16).TrimStart();
                                        playerlist.Remove(plrlistentry);
                                    }
                                }
                                else
                                {
                                    // marker not found, use original line
                                    Console.WriteLine(line);
                                }
                                Console.ResetColor();
                            }
                            else if (line.Contains("[ExpChat/mountClientApp (Debug)]"))
                            {
                                if (line.Contains("Incoming MessageReceived Status: Success Text:"))
                                {
                                    if (!iomessage)
                                    {
                                        string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                        string marker = "Incoming MessageReceived Status: Success Text:";

                                        int index = line.IndexOf(marker);
                                        if (index >= 0)
                                        {
                                            string cleaned = line.Substring(index + marker.Length).TrimStart();
                                            Console.WriteLine(timestamp + " Player messaged: " + cleaned);
                                        }
                                        else
                                        {
                                            Console.WriteLine(line);
                                        }
                                    }
                                    else
                                    {
                                        iomessage = false;
                                    }
                                }
                                else if (line.Contains("Outgoing SendingMessage Status: Sending Text: "))
                                {
                                    string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                    string marker = "Outgoing SendingMessage Status: Sending Text: ";

                                    int index = line.IndexOf(marker);
                                    if (index >= 0)
                                    {
                                        string cleaned = line.Substring(index + marker.Length).TrimStart();
                                        Console.WriteLine(timestamp + " LocalPlayer sent: " + cleaned);
                                        iomessage = true;
                                    }
                                    else
                                    {
                                        Console.WriteLine(line);
                                    }
                                    //Outgoing SendingMessage Status: Sending Text:
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                    string marker = "[ExpChat/mountClientApp (Debug)] - ";

                                    int index = line.IndexOf(marker);
                                    if (index >= 0)
                                    {
                                        string cleaned = line.Substring(index + marker.Length).TrimStart();
                                        Console.WriteLine(timestamp + " " + cleaned);
                                    }
                                    else
                                    {
                                        Console.WriteLine(line);
                                    }
                                    Console.ResetColor();
                                }
                            }
                            else if (line.Contains("[FLog::GameJoinLoadTime] Report game_join_loadtime:"))
                            {
                                string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                //https:// www.roblox.com/games/start?placeId=placeid&gameInstanceId=instanceid
                                string marker = "[FLog::GameJoinLoadTime] Report game_join_loadtime:";
                                string placeid = "placeid:";
                                string userid = "userid:";
                                string universeidtext = ", universeid:";
                                printbool = true;
                                int index = line.IndexOf(marker);
                                int placeidindex = line.IndexOf(placeid);
                                int useridindex = line.IndexOf(userid);
                                int universeidindex = line.IndexOf(universeidtext);
                                string consolejoininfo = "\nJoining place";
                                string cleanedplaceid = "";
                                if (placeidindex >= 0)
                                {
                                    cleanedplaceid = line.Substring(placeidindex + (placeid.Length - 1)).TrimStart();
                                    cleanedplaceid = ExtractBetweenMarkers(cleanedplaceid, ':', ',');
                                    gameid = cleanedplaceid;
                                    consolejoininfo = consolejoininfo + ", PlaceID: " + cleanedplaceid;
                                }
                                string cleaneduserid;
                                if (useridindex >= 0)
                                {
                                    cleaneduserid = line.Substring(useridindex + (userid.Length - 1)).TrimStart();
                                    cleaneduserid = ExtractBetweenMarkers(cleaneduserid, ':', ',');
                                    localid = cleaneduserid;
                                    consolejoininfo = consolejoininfo + ", UserID: " + cleaneduserid;
                                }
                                string cleaneduniverseid;
                                if (universeidindex >= 0)
                                {
                                    cleaneduniverseid = line.Substring(universeidindex + (universeidtext.Length - 1)).TrimStart();
                                    cleaneduniverseid = ExtractBetweenMarkers(cleaneduniverseid, ':', ',');
                                    universeid = cleaneduniverseid;
                                    consolejoininfo = consolejoininfo + ", UniverseID: " + cleaneduniverseid;
                                }

                                Console.WriteLine(timestamp + " " + consolejoininfo);

                            }
                            else if (line.Contains("Joining game '"))
                            {

                                string ftjobid = "Joining game '";
                                int jobidindex = line.IndexOf("Joining game '");
                                string cleanedjobid;
                                if (jobidindex >= 0)
                                {
                                    cleanedjobid = line.Substring(jobidindex + (ftjobid.Length - 1)).TrimStart();
                                    cleanedjobid = ExtractBetweenMarkers(cleanedjobid, '\'', ' ');
                                    cleanedjobid = cleanedjobid.Substring(0, cleanedjobid.Length - 1);
                                    jobid = cleanedjobid;
                                }
                            }
                            else if (line.Contains("[FLog::Network] UDMUX Address"))
                            {
                                string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                string placeid = "UDMUX Address =";
                                string port = "Port =";
                                int placeidindex = line.IndexOf(placeid);
                                int ipport = line.IndexOf(port);
                                string consolejoininfo = "Place server IP: ";
                                string cipport;
                                string cleanedplaceid;
                                if (placeidindex >= 0)
                                {
                                    cleanedplaceid = line.Substring(placeidindex + (placeid.Length - 1)).TrimStart();
                                    cleanedplaceid = ExtractBetweenMarkers(cleanedplaceid, '=', ',');
                                    consolejoininfo += cleanedplaceid;
                                }
                                if (ipport >= 0)
                                {
                                    cipport = line.Substring(ipport + (port.Length - 1)).TrimStart();
                                    cipport = ExtractBetweenMarkers(cipport, '=', '\n');
                                    //consolejoininfo += ", Port = " + cipport;
                                    //doesnt work for some reason, and i dont really expect this to get fixed because
                                    //who has a use for the port? and also if you're really that guy
                                    //just do it yourself.
                                    //lol my past me reasoning my skill issue
                                }
                                Console.Write(timestamp + " " + consolejoininfo + ", JobID: " + jobid + "\n");

                                try
                                {
                                    int limit = 100;
                                    int cursorCount = 0;
                                    string? cursor = null;
                                    bool isdone = false;

                                    while (cursorCount < 10) // Avoid infinite loop
                                    {
                                        string url = $"https://games.roblox.com/v1/games/{gameid}/servers/Public?limit={limit}";
                                        if (!string.IsNullOrEmpty(cursor))
                                            url += $"&cursor={cursor}";
                                        await Task.Delay(1000);

                                        HttpResponseMessage response = await client.GetAsync(url);
                                        response.EnsureSuccessStatusCode();

                                        string responseBody = await response.Content.ReadAsStringAsync();

                                        JsonObject json = JsonNode.Parse(responseBody)?.AsObject();
                                        JsonArray servers = json?["data"]?.AsArray();

                                        foreach (JsonNode? server in servers)
                                        {
                                            string? jobId1 = server?["id"]?.ToString();
                                            if (jobId1 == jobid)
                                            {
                                                Console.WriteLine("Server data fetched:");
                                                Console.WriteLine(server?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                                                await Task.Delay(1500); // not a *good* solution but otherwise this wont work.
                                                isdone = true;
                                                break;

                                            }
                                        }

                                        cursor = json?["nextPageCursor"]?.ToString();
                                        if (string.IsNullOrEmpty(cursor))
                                            break;

                                        cursorCount++;
                                    }
                                    if (isdone == false) { Console.WriteLine("Server with JobId not found."); }

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Error: {e.Message}");
                                }


                            }
                            else if (line.Contains("[FLog::Output]"))
                            {

                                string flop = "[FLog::Output] ";
                                string info = "Info: ";

                                /*
                                if (line.Contains(flop + "Info: ")) {
                                    string marker = info;

                                    int index = line.IndexOf(marker);
                                    if (index >= 0)
                                    {
                                        // Remove everything before and including marker, then trim leading spaces
                                        string cleaned = line.Substring(index + marker.Length).TrimStart();
                                        Console.WriteLine(cleaned);
                                    }
                                    else
                                    {
                                        // marker not found, use original line
                                        Console.WriteLine(line);
                                    }
                                }
                                */
                                if (line.Contains(flop + info))
                                {
                                    string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                                    if (line.Contains("Stack End"))
                                    {
                                        string marker = info;

                                        int index = line.IndexOf(marker);
                                        if (index >= 0)
                                        {
                                            // Remove everything before and including marker, then trim leading spaces
                                            string cleaned = line.Substring(index + marker.Length).TrimStart();
                                            Console.WriteLine(timestamp + " Info: " + cleaned + "\n"); //just incase
                                        }
                                        else
                                        {
                                            // marker not found, use original line
                                            Console.WriteLine(line);
                                        }
                                    }
                                    else
                                    {
                                        string marker = info;

                                        int index = line.IndexOf(marker);
                                        if (index >= 0)
                                        {
                                            // Remove everything before and including marker, then trim leading spaces
                                            string cleaned = line.Substring(index + marker.Length).TrimStart();
                                            Console.WriteLine(timestamp + " Info: " + cleaned);
                                        }
                                        else
                                        {
                                            // marker not found, use original line
                                            Console.WriteLine(line);
                                        }
                                    }

                                    /* else if (line.Contains(flop + warn))
                                {
                                    string marker = warn;

                                    int index = line.IndexOf(marker);
                                    if (index >= 0)
                                    {
                                        // Remove everything before and including marker, then trim leading spaces
                                        string cleaned = line.Substring(index + marker.Length).TrimStart();
                                        Console.WriteLine(cleaned);
                                    }
                                    else
                                    {
                                        // marker not found, use original line
                                        Console.WriteLine(line);
                                    }
                                } */
                                    Console.ResetColor();
                                }
                                else if (line.Contains(flop) && printbool == true)
                                {
                                    if (!line.Contains("[tid:") && !line.Contains("Settings Date"))
                                    {
                                        string marker = flop;
                                        string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                        int index = line.IndexOf(marker);
                                        if (index >= 0)
                                        {
                                            // Remove everything before and including marker, then trim leading spaces
                                            string cleaned = line.Substring(index + marker.Length).TrimStart();
                                            Console.WriteLine(timestamp + " Output: " + cleaned);
                                        }
                                        else
                                        {
                                            // marker not found, use original line
                                            Console.WriteLine(line);
                                        }
                                    }
                                }
                            }
                            else if (line.Contains("[FLog::Error] Error: "))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                string marker = "[FLog::Error] Error: ";

                                int index = line.IndexOf(marker);
                                if (index >= 0)
                                {
                                    // Remove everything before and including marker, then trim leading spaces
                                    string cleaned = line.Substring(index + marker.Length).TrimStart();
                                    Console.WriteLine(timestamp + " Error: " + cleaned);
                                }
                                else
                                {
                                    // marker not found, use original line
                                    Console.WriteLine(line);
                                }

                                /*
                                if (line.Contains(flop + "Info: ")) {

                                }
                                */
                                Console.ResetColor();
                            }
                            else if (line.Contains("[FLog::Warning] Warning: "))
                            {
                                string timestamp = ExtractBetweenMarkers(line, 'T', '.');
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                string marker = "[FLog::Warning] Warning: ";

                                int index = line.IndexOf(marker);
                                if (index >= 0)
                                {
                                    // Remove everything before and including marker, then trim leading spaces
                                    string cleaned = line.Substring(index + marker.Length).TrimStart();
                                    Console.WriteLine(timestamp + " Warning: " + cleaned);
                                }
                                else
                                {
                                    // marker not found, use original line
                                    Console.WriteLine(line);
                                }

                                /*
                                if (line.Contains(flop + "Info: ")) {

                                }
                                */
                                Console.ResetColor();
                            }
                            //[FLog::Network] Client:Disconnect
                            else if (line.Contains("[FLog::Network] Client:Disconnect"))
                            {
                                Console.WriteLine("Client got disconnected");
                                printbool = false;
                            }
                            else if (line.Contains("[FLog::Network] Sending disconnect with reason: 277"))
                            {
                                Console.WriteLine("code 277: lost connection, sending rejoin");
                                foreach (Process proc in processes)
                                {
                                    Console.WriteLine(gameid);
                                    Console.WriteLine($"Attempting to close process with ID: {proc.Id}");
                                    proc.Kill();
                                    Console.WriteLine($"{targetProcessName} has closed.");

                                    Process.Start(new ProcessStartInfo("cmd", $"/c start roblox://placeId={gameid}") { CreateNoWindow = true });

                                    Environment.Exit(0);
                                    break;
                                }
                            } else if (line.Contains("[FLog::SingleSurfaceApp] destroy controllers")) {
                                Environment.Exit(0);
                                break; //it's a tradition for me idk why lol // NO MORE FISHSTRAP INTERGRATION RELIANCY :DDDDDD  cuz windows 11 broke it for me idk why :P
                            }
                            else if (line.Contains("[FLog::SingleSurfaceApp]"))
                            {
                                int index = line.IndexOf("[FLog::SingleSurfaceApp]");
                                
                                if (index >= 0)
                                {
                                    Console.WriteLine( line.Substring(index).TrimStart());
                                }
                            }
                            else if (line.Contains("[FLog::SurfaceController]"))
                            {
                                int index = line.IndexOf("[FLog::SurfaceController]");
                                
                                if (index >= 0)
                                {
                                    Console.WriteLine(line.Substring(index).TrimStart());
                                }
                            }
                            else if (line.Contains("[FLog::UpdateController] WindowsUpdateController: updaterFullPath: "))
                            {
                                string startMarker = "[FLog::UpdateController] WindowsUpdateController: updaterFullPath: ";
                                string endMarker = "\\RobloxPlayerInstaller.exe";

                                int startIndex = line.IndexOf(startMarker);
                                int endIndex = line.IndexOf(endMarker);

                                if (startIndex >= 0 && endIndex > startIndex)
                                {
                                    startIndex += startMarker.Length;
                                    string result = line.Substring(startIndex, endIndex - startIndex).Trim();
                                    Console.WriteLine("Found roblox folder");
                                    robloxfolder = result;
                                }
                                else
                                {
                                    Console.WriteLine("Markers not found in the string.");
                                }

                            }
                        }
                        lastPosition = fs.Position;
                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == ex.Message) { }
                    //sad little "ex" lmao, i'm lazy enough, and scared enough to not remove this.
                    //did this just to remove the warning lol
                    //also, this is basically just if (true) {} but... using exception
                }
                
            }
        }
        static async Task whiletwo()
        {
            //split string aka join place using roblox://
            //string function = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
            //same here above change array val to 1, dont forget use proper clang array notation(aka 0 = 1), var name is arg(ig)... elseif(function == "join") {
            //if (arg != null) { quit roblox and start another session using roblox://placeid= and ofc add the arg val}
            //}
            

            while (true)
            {
                string command = Console.ReadLine();
                string function = "";
                string arg = "";
                if (command.Contains(' '))
                {
                    function = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
                    arg = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[1];
                }
                Process[] processes = Process.GetProcessesByName(targetProcessName);
                if (command == null)
                {
                    command = "";
                }
                else if (command.ToLower() == "rejoin")
                {
                    allowrejoin = true;
                }
                else if (command.ToLower() == "job rejoin")
                {
                    jobrejoin = true;
                }
                else if (command.ToLower() == "exit")
                {
                    exit = true;
                }
                else if (command.ToLower() == "list places") {

                    try
                    {
                        bool isdone = false;

                        string url = $"https://develop.roblox.com/v1/universes/{universeid}/places";

                        HttpResponseMessage response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        string responseBody = await response.Content.ReadAsStringAsync();

                        using JsonDocument doc = JsonDocument.Parse(responseBody);

                        // "data" is the array of places
                        JsonElement dataArray = doc.RootElement.GetProperty("data");

                        foreach (JsonElement place in dataArray.EnumerateArray())
                        {
                                Console.WriteLine(place.ToString());
                                isdone = true; 
                            
                        }
                        if (!isdone)
                        {
                            Console.WriteLine("Server with JobId not found.");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }

                } else if (command.ToLower() == "help")
                {
                    Console.WriteLine("help, rejoin (using gameid), job rejoin (means server rejoin), list places (places from the universeid), exit (to exit roblox and program), list playerlist (self-explanitory), join [place] (uses the first argument as a placeid and joins it.), patch corescript (patches roblox configs to error corescripts out, useful to remove topbar), unpatch corescript (self-explanitory)");
                } else if (command.ToLower() == "list players")
                {
                    foreach (string player in playerlist)
                    {
                        Console.WriteLine($"{player}");
                    }
                } else if (function == "join")
                {
                    if (arg != null)
                    {
                        foreach (Process proc in processes)
                        {
                            Console.WriteLine($"Attempting to close process with ID: {proc.Id}");
                            proc.Kill();
                            Console.WriteLine($"{targetProcessName} has closed.");

                            Process.Start(new ProcessStartInfo("cmd", $"/c start roblox://placeId={arg}") { CreateNoWindow = true });

                            Environment.Exit(0);
                            break;
                        }
                    }
                        //elseif(function == "join") {
                        //if (arg != null) { quit roblox and start another session using roblox://placeid= and ofc add the arg val}
                        //}
                } else if (command.ToLower() == "patch corescript")
                {
                    File.WriteAllText(robloxfolder + "\\content\\configs\\InExperiencePatchConfig\\InExperiencePatchConfig.json", "{\"AppStorageResetId\": \"0\", \"AssetId\": \"80471914653504\", \"AssetVersion\": \"4297\", \"IsForcedUpdate\": false, \"LocalAssetURI\": \"rbxasset://models/UniversalApp/UniversalApp.rbxm\", \"LocalAssetHash\": \"43b45664c86890781da847cb82ea794f\", \"MaxAppVersion\": \"695\"}");
                    Console.WriteLine("Succesfully Patched! (rejoin to make it take effect)");
                } else if (command.ToLower() == "unpatch corescript")
                {
                    File.WriteAllText(robloxfolder + "\\content\\configs\\InExperiencePatchConfig\\InExperiencePatchConfig.json", "{\"AppStorageResetId\": \"0\", \"AssetId\": \"80471914653504\", \"AssetVersion\": \"4297\", \"IsForcedUpdate\": false, \"LocalAssetURI\": \"rbxasset://models/InExperience/InExperience.rbxm\", \"LocalAssetHash\": \"43b45664c86890781da847cb82ea794f\", \"MaxAppVersion\": \"695\"}");
                    Console.WriteLine("Succesfully Unpatched! (rejoin to make it take effect)");
                }

            // kinda legacy but still works so idc lol and it jacks up the amount of lines of code lol
            if (allowrejoin == true)
                {
                    foreach (Process proc in processes)
                    {
                        Console.WriteLine(gameid);
                        Console.WriteLine($"Attempting to close process with ID: {proc.Id}");
                        proc.Kill();
                        Console.WriteLine($"{targetProcessName} has closed.");

                        Process.Start(new ProcessStartInfo("cmd", $"/c start roblox://placeId={gameid}") { CreateNoWindow = true });

                        Environment.Exit(0);
                        break;
                    }
                }
                else if (jobrejoin == true)
                {
                    foreach (Process proc in processes)
                    {
                        Console.WriteLine(gameid);
                        Console.WriteLine($"Attempting to close process with ID: {proc.Id}");
                        proc.Kill();
                        Console.WriteLine($"{targetProcessName} has closed.");

                        Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.roblox.com/games/start?placeId={gameid}" + "&gameInstanceId=" + jobid) { CreateNoWindow = true });

                        Environment.Exit(0);
                        break;
                    }
                }
                else if (exit == true)
                {
                    foreach (Process proc in processes)
                    {
                        Console.WriteLine(gameid);
                        Console.WriteLine($"Attempting to close process with ID: {proc.Id}");
                        proc.Kill();
                        Console.WriteLine($"{targetProcessName} has closed.");
                        Environment.Exit(0);
                        break; //incase lol
                    }
                }
            }
        }
    }
}
