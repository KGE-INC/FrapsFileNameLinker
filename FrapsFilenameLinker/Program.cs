using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FrapsFilenameLinker {
    /// <summary>
    /// This program checks all the files in its current working directory.
    /// It looks for raw footage created by the capture program FRAPS, which names 
    /// are in the following format:
    /// GameName year-month-date hour-minute-second-100th of a second.avi
    /// or in other words
    /// abCD123 0123-45-67 89-01-23-45.avi
    /// 
    /// The program VirtualDub will consider segments linked and automatically append them
    /// if they all have the same name ending with .[number].avi (xxx.00.avi, xxx.01.avi, etc.)
    /// 
    /// So what this program does is check the timestamps and modifies the names
    /// so that Virtual Dub will consider the segments linked. It considers two segments to be linked
    /// if they have the same GameName and are less than an arbitrary duration apart.
    /// This is by default 5 minutes and can be overridden with command-line parameter.
    /// 
    /// This is simply so that I don't have to append them manually which is tedious and error-prone.
    /// </summary>
    class Program {

        TimeSpan m_maxGap = TimeSpan.FromMinutes(5);
        readonly Regex m_gameIdRegex = new Regex("[A-Za-z0-9]+ ");
        readonly Regex m_timeRegex = new Regex("[0-9]{4}(-[0-9]{2}){2} [0-9]{2}(-[0-9]{2}){3}");
        readonly Regex m_fileMatchRegex = new Regex("[A-Za-z0-9]+ [0-9]{4}(-[0-9]{2}){2} [0-9]{2}(-[0-9]{2}){3}.avi");

        static void Main(string[] args) {
            var program = new Program();
            program.ProcessCommandLineArguments(args);
            program.Run();
        }

        void ProcessCommandLineArguments(string[] args) {
            if (args.Length > 0) {
                if (IsHelp(args[0])) {
                    DisplayHelpMessage();
                    Environment.Exit(0);
                }
                else {
                    double overrideMaxGap;
                    if (Double.TryParse(args[0], out overrideMaxGap)) {
                        if (IsValidOverrideMaxGap(overrideMaxGap)) {
                            m_maxGap = TimeSpan.FromMinutes(overrideMaxGap);
                            Console.WriteLine("Max gap overridden to {0} minutes.", overrideMaxGap);
                        }
                        else {
                            Console.WriteLine("Invalid max time gap between videos specified.");
                            Environment.Exit(0);
                        }
                    }
                    else {
                        Console.WriteLine("Invalid argument specified.");
                        DisplayHelpMessage();
                        Environment.Exit(0);
                    }
                }
            }
        }

        bool IsValidOverrideMaxGap(double overrideMaxGap) {
            return overrideMaxGap >= 1.0;
        }

        void DisplayHelpMessage() {
            Console.WriteLine("This utility renames raw FRAPS footage so that VirtualDub will consider the segments linked and automatically append them.");
            Console.WriteLine("It looks for all the .avi files in its current working directory.");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("[programname.exe]\tuses default time gap between videos ({0} minutes)", m_maxGap.Minutes);
            Console.WriteLine("[programname.exe] {0}\toverrides time gap between videos to be {0} minutes", 10.5);
            Console.WriteLine("[programname.exe] -h\tdisplays this help message.");
        }

        bool IsHelp(string arg) {
            arg = arg.ToUpper();
            return arg == "-H" || arg == "-HELP" || arg == "HELP" || arg == "H";
        }

        void Run() {
            var allFiles = from fileName in Directory.GetFiles(Environment.CurrentDirectory)
                           where m_fileMatchRegex.IsMatch(fileName)
                           orderby fileName
                           select fileName;
            var replacements = new Dictionary<string, string>();

            Console.WriteLine("Found {0} files to rename.", allFiles.Count());

            var partIndex = 0;
            var lastDateTime = default(DateTime);
            var lastGameID = "";
            var baseName = "";
            foreach (var fileName in allFiles) {
                var currentGameID = GetGameID(fileName);
                var currentDateTime = GetDateTime(fileName);
                if (currentGameID != lastGameID || currentDateTime.Subtract(lastDateTime) >= m_maxGap) {
                    partIndex = 0;
                    baseName = GetBaseName(currentDateTime, currentGameID);
                }
                replacements[fileName] = GetReplacementName(baseName, partIndex);
                ++partIndex;
                lastGameID = currentGameID;
                lastDateTime = currentDateTime;
            }

            if (replacements.Values.Any(f => File.Exists(f))) {
                Console.WriteLine("One of the renames cannot be performed because there is already a file of the same name.\nAborting.");
                return;
            }

            if (replacements.Keys.Any(f => ! File.Exists(f))) {
                Console.WriteLine("One of the renames cannot be performed because the file to be renamed is missing.\nAborting.");
                return;
            }

            foreach (var replacement in replacements) {
                File.Move(replacement.Key, replacement.Value);
            }

            Console.WriteLine("{0} files renamed successfully.", replacements.Keys.Count);
        }

        string GetBaseName(DateTime lastDateTime, string lastGameID) {
            return String.Format("{0} {1}-{2}-{3} {4}-{5}", lastGameID, lastDateTime.Year, lastDateTime.Month, lastDateTime.Day,
                                    lastDateTime.Hour, lastDateTime.Minute, lastDateTime.Second);
        }

        string GetReplacementName(string baseName, int partIndex) {
            var number = partIndex.ToString();
            // Insert leading zero if digit is below 10
            if (partIndex < 10) {
                number = number.Insert(0, "0");
            }
            return String.Format("{0}.{1}.avi", baseName, number);
        }

        DateTime GetDateTime(string fileName) {
            var dateTimeString = m_timeRegex.Match(fileName).Value.TrimStart(' ');
            var dateValuesAsStrings = dateTimeString.Split('-', ' ');
            var dateValues = (from s in dateValuesAsStrings
                              select Int32.Parse(s)).ToArray();
            Debug.Assert(dateValues.Length == 7);

            return new DateTime(dateValues[0], dateValues[1], dateValues[2], dateValues[3], dateValues[4], dateValues[5]);
        }

        string GetGameID(string file) {
            return m_gameIdRegex.Match(file).Value.TrimEnd(' ');
        }
    }
}
