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

class Program
{
    const int HWND_TOPMOST = -1;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
    static bool iomessage = false;
    static readonly string targetProcessName = "RobloxPlayerBeta";
    static string localid = "";
    static string gameid = "";
    static bool allowrejoin = false;
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
        Console.WriteLine($"[Info] Watching folder: {logDirectory}\n");

        

        // Wait for recent enough log file (by LastWriteTime)
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
                    Console.WriteLine($"[Info] Found recent log: {logFileInfo.Name}");
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



        Console.Clear();
        Console.WriteLine($"\n[Monitor] Now Displaying Log: {Path.GetFileName(currentLogFile)}\n\n");
        Console.WriteLine("Enter 'Rejoin' to... rejoin your current game once you join.");
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

        static void whileone()
        {
            while (true)
            {
               
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
                                string marker = "[ExpChat/mountClientApp (Trace)] - ";

                                int index = line.IndexOf(marker);
                                if (line.Contains("Player Removed: ") && line.Contains(localid))
                                {
                                    Console.WriteLine("Player Left game, but still print, incase of any attempts to curb logging somehow.\n\n");
                                }
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
                            else if (line.Contains("[ExpChat/mountClientApp (Debug)]"))
                            {
                                if (line.Contains("Incoming MessageReceived Status: Success Text:"))
                                {
                                    if (!iomessage)
                                    {
                                        string marker = "Incoming MessageReceived Status: Success Text:";

                                        int index = line.IndexOf(marker);
                                        if (index >= 0)
                                        {
                                            string cleaned = line.Substring(index + marker.Length).TrimStart();
                                            Console.WriteLine("Player messaged: " + cleaned);
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
                                    string marker = "Outgoing SendingMessage Status: Sending Text: ";

                                    int index = line.IndexOf(marker);
                                    if (index >= 0)
                                    {
                                        string cleaned = line.Substring(index + marker.Length).TrimStart();
                                        Console.WriteLine("LocalPlayer sent: " + cleaned);
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
                                    string marker = "[ExpChat/mountClientApp (Debug)] - ";

                                    int index = line.IndexOf(marker);
                                    if (index >= 0)
                                    {
                                        string cleaned = line.Substring(index + marker.Length).TrimStart();
                                        Console.WriteLine(cleaned);
                                    }
                                    else
                                    {
                                        Console.WriteLine(line);
                                    }
                                }
                            }
                            else if (line.Contains("[FLog::GameJoinLoadTime] Report game_join_loadtime:"))
                            {
                                string marker = "[FLog::GameJoinLoadTime] Report game_join_loadtime:";
                                string placeid = "placeid:";
                                string userid = "userid:";
                                int index = line.IndexOf(marker);
                                int placeidindex = line.IndexOf(placeid);
                                int useridindex = line.IndexOf(userid);
                                string consolejoininfo = "\nJoining place";
                                string cleanedplaceid = "";
                                if (placeidindex >= 0)
                                {
                                    cleanedplaceid = line.Substring(placeidindex + (placeid.Length - 1)).TrimStart();
                                    cleanedplaceid = ExtractBetweenMarkers(cleanedplaceid, ':', ',');
                                    gameid = cleanedplaceid;
                                    consolejoininfo = consolejoininfo + ", PlaceID:" + cleanedplaceid;
                                }
                                string cleaneduserid;
                                if (useridindex >= 0)
                                {
                                    cleaneduserid = line.Substring(useridindex + (userid.Length - 1)).TrimStart();
                                    cleaneduserid = ExtractBetweenMarkers(cleaneduserid, ':', ',');
                                    localid = cleaneduserid;
                                    consolejoininfo = consolejoininfo + ", UserID:" + cleaneduserid;
                                }
                                Console.WriteLine(consolejoininfo);

                            }
                            else if (line.Contains("[FLog::Network] UDMUX Address"))
                            {
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
                                }
                                Console.WriteLine(consolejoininfo);
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
                                }

                            }
                            else if (line.Contains("[FLog::Error] Error: "))
                            {
                                string marker = "[FLog::Error] Error: ";

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

                                /*
                                if (line.Contains(flop + "Info: ")) {

                                }
                                */
                            }
                            else if (line.Contains("[FLog::Warning] Warning: "))
                            {
                                string marker = "[FLog::Warning] Warning: ";

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

                                /*
                                if (line.Contains(flop + "Info: ")) {

                                }
                                */
                            }
                            //[FLog::Network] Client:Disconnect
                            else if (line.Contains("[FLog::Network] Client:Disconnect"))
                            {
                                Console.WriteLine("Client got disconnected");
                                Console.WriteLine(localid);
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
        static void whiletwo()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (command == null)
                {
                    command = "";
                }
                else if (command.ToLower() == "rejoin")
                {
                    allowrejoin = true;
                }
                Process[] processes = Process.GetProcessesByName(targetProcessName);
                if (processes.Length == 0)
                {
                    Console.WriteLine($"{targetProcessName} has closed.");
                    Environment.Exit(0);
                    break;
                }

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
            }
        }
    }
}
