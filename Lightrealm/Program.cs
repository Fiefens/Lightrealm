using System;
using System.IO;
using System.Diagnostics;

namespace Lightrealm
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
#if DEBUG
            using (var game = new Game1())
            {
                game.Run(); // Let exceptions crash the game in Debug mode
            }
#else


                        try
                        {
                            // Outer try-catch to ensure normal exception behavior if inner one fails
                            try
                            {
                                using (var game = new Game1())
                                {
                                    game.Run();
                                }
                            }
                            catch (Exception ex)
                            {
                                crashHandled = true;

                                string basePath = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                    ".LightrealmData",
                                    "crashreports"
                                );

                                string gameWorldName = (Game1.GameWorld != null && !string.IsNullOrWhiteSpace(Game1.GameWorld.Name))
                                    ? $"_{Game1.GameWorld.Name}"
                                    : "";

                                string crashFolder = Path.Combine(basePath, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}{gameWorldName}");
                                Directory.CreateDirectory(crashFolder); // Ensure crash directory exists

                                string logFilePath = Path.Combine(crashFolder, "crash_log.txt");

                                string referredNames = Game1.ReferredToNamesOfLastCommandEntities != null
                                    ? string.Join("\n", Game1.ReferredToNamesOfLastCommandEntities)
                                    : "None";

                                string structureName = Game1.StructureIsNullIfNull != null ? Game1.StructureIsNullIfNull.Name : "Player was outside or missing";
                                string roomDetails = Game1.RoomIndexIsNegativeOneIfNull != -1 ? $"Room Index: {Game1.RoomIndexIsNegativeOneIfNull}" : "Player was outside or missing";

                                string crashMessage =
                "Your game has crashed! :(\n\n" +
                "You can still access your last Game Save if it manually/autosaved.\n" +
                $"A detailed crash report has been created in this folder:\n\"{crashFolder}\"\n" +
                "Consider emailing the crash report files to revenantentertainmentofficial@gmail.com, along with information about what exactly you did that caused the game to crash (if you remember or reloaded).\n\n" +
                "[Lightrealm Crash Report]\n\n";

                                string logDetails = $"[{DateTime.Now}] {ex.Message}\n{ex.StackTrace}\n\n" +
                                "=== Last Known Command Data ===\n" +
                                $"Command: {Game1.LastRanCommand ?? "None"}\n" +
                                $"Executor Block Position: X={Game1.LastRanCommandBlockX}, Z={Game1.LastRanCommandBlockZ}\n\n";

                                if (!string.IsNullOrWhiteSpace(Game1.LastRanCommand))
                                {
                                    logDetails +=
                                        "=== Entities Referenced In Said Command ===\n" +
                                        $"{referredNames}\n\n";
                                }


                                if(Game1.GameWorld != null)
                                {
                                    logDetails +=
                                        "=== Location Details ===\n" +
                                        $"Structure: {structureName}\n" +
                                        $"Room: {roomDetails}\n\n" +

                                        "=== WorldGen Details ===\n" +
                                        $"Seed: {Game1.GameWorld.Seed}\n" +
                                        $"Length: {Game1.GameWorld.Length}\n" +
                                        $"Width: {Game1.GameWorld.Width}\n" +
                                        $"Original Civilization Count: {Game1.GameWorld.OGCivCount + 4} (set number, includes extras)\n" +
                                        $"Prosperity Multiplier: {Game1.GameWorld.ProsperityMultiplier}\n" +
                                        $"Size Modifier: {Game1.GameWorld.SizeMod}\n" +
                                        $"Max Age: {Game1.GameWorld.MaxAge}\n\n" +
                                        $"Current Age: {(int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000))}";
                                }
                                else
                                {
                                    logDetails += "No world data found.\n";
                                }

                                try
                                {
                                    File.WriteAllText(logFilePath, crashMessage + logDetails);
                                }
                                catch
                                {
                                    // Fail silently if writing the log fails
                                }

                                if (Game1.GameWorld != null)
                                {
                                    try
                                    {
                                        string lastSavedGameDir = GetWorldSaveDirectory(Game1.GameWorld);
                                        if (Directory.Exists(lastSavedGameDir))
                                        {
                                            string backupSaveDir = Path.Combine(crashFolder, "SavedGameBackup");
                                            CopyDirectory(lastSavedGameDir, backupSaveDir);
                                        }
                                    }
                                    catch (Exception copyEx)
                                    {
                                        File.AppendAllText(logFilePath, $"[Backup Error] {copyEx.Message}\n{copyEx.StackTrace}\n\n");
                                    }
                                }

                                try
                                {
                                    Process.Start("notepad.exe", logFilePath);
                                }
                                catch
                                {
                                    // Fail silently if Notepad cannot be opened
                                }

                                throw; // Re-throw the exception to allow outer catch to execute
                            }
                        }
                        catch
                        {
                            if (!crashHandled)
                            {
                                try
                                {
                                    // Construct the message for Notepad
                                    string worldGenData =
                                        "Your game has crashed! :(\n\n" +
                                        "It seems something went wrong in world generation, or something else?\n" +
                                        "Consider emailing this data to revenantentertainmentofficial@gmail.com so that I can reproduce the issue.\n\n" +
                                        "[Lightrealm Crash Report]\n\n" +
                                        "=== WorldGen Details ===\n" +
                                        $"Seed: {Game1.GameWorld.Seed}\n" +
                                        $"Length: {Game1.GameWorld.Length}\n" +
                                        $"Width: {Game1.GameWorld.Width}\n" +
                                        $"Original Civilization Count: {Game1.GameWorld.OGCivCount}\n" +
                                        $"Prosperity Multiplier: {Game1.GameWorld.ProsperityMultiplier}\n" +
                                        $"Size Modifier: {Game1.GameWorld.SizeMod}\n" +
                                        $"Max Age: {Game1.GameWorld.MaxAge}\n";

                                    // Create a temporary file
                                    string tempFilePath = Path.Combine(Path.GetTempPath(), "WorldGenCrashReport.txt");
                                    File.WriteAllText(tempFilePath, worldGenData);

                                    // Try to open Notepad with the temp file
                                    Process.Start("notepad.exe", tempFilePath);
                                }
                                catch
                                {
                                    // Fail silently if Notepad cannot be opened
                                }
                            }

                            throw; // Re-throw the exception to allow normal crash behavior
                        }
        #endif

        }
        private static string GetWorldSaveDirectory(World world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));

            string name = (world.GamePlayerAssociation != null && world.GamePlayerAssociation.ActiveParty != null)
                ? $"{world.GamePlayerAssociation.ActiveParty.Leader.Name}, leader of {world.GamePlayerAssociation.ActiveParty.Name} in {char.ToLower(world.Name[0])}{world.Name.Substring(1)}"
                : world.Name;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                ".LightrealmData",
                name
            );
        }

        private static void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }
    }
}
