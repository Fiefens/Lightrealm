using System;
using System.IO;

namespace Lightrealm
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                Console.WriteLine("First chance exception: " + eventArgs.Exception.Message);
            };

            try
            {
                using var game = new Game1();
                game.Run();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private static void HandleException(Exception ex)
        {
            if (ex != null)
            {
                // Log the exception to a file and the console
                File.WriteAllText("error_log.txt", ex.ToString());
                Console.WriteLine("Unhandled exception: " + ex);
            }
            // Prevent the application from crashing
            Console.WriteLine("An unexpected error occurred. Please check the log file for details.");
        }
    }
}
