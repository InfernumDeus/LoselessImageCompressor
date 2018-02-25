using System;
using System.IO;

namespace LoselessImageCompressor
{
    static class Logger
    {
        public static object synchronizationObject = new object();

        public static void LogSkippedFile(string exceptionType, string message, string filepath)
        {
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "skipped files " + DateTime.Now.Date.ToString("dd-MM-yyyy") + ".txt";
            using (StreamWriter sw = File.AppendText(logPath))
            {
                if (new FileInfo(logPath).Length == 0) sw.WriteLine("For some reasone this files wasn't processed:");

                sw.WriteLine(exceptionType + ": " + message + " File location: " + filepath);
            }
        }
    }
}
