using System;

namespace wBeatSaberCamera.Utils
{
    public static class Log
    {
        public static void Warn(string message)
        {
            WriteLine("[WARN]", message);
        }

        public static void Error(string message)
        {
            WriteLine("[ERROR]", message);
        }

        public static void Debug(string message)
        {
            WriteLine("[DEBUG]", message);
        }

        private static void WriteLine(string level, string message)
        {
            Console.WriteLine($"{DateTime.Now} {level}: {message}");
        }
    }
}