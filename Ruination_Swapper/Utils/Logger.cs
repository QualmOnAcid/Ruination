using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebviewAppShared.Utils
{
    public class Logger
    {
        private static string logFilePath = Utils.AppDataFolder + "\\Logs\\";
        private static string logFileName;
        public static void Init()
        {
            Directory.CreateDirectory(logFilePath);
            if (Directory.GetFiles(logFilePath).Length > 15)
            {
                var files = Directory.GetFiles(logFilePath);
                var fileInfos = new List<FileInfo>();
                
                for(int i = 0; i < files.Length; i++)
                {
                    fileInfos.Add(new FileInfo(files[i]));
                }

                fileInfos = fileInfos.OrderBy(x => x.LastWriteTime).ToList();

                File.Delete(fileInfos[0].FullName);
            }
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath += $"Ruination_{timestamp}.log";
            logFileName = logFilePath;
            using (FileStream fs = File.Create(logFileName)) fs.Close();
            Log("*************** Log Start ***************");
        }

        public static void Log(string message)
        {
            if (!File.Exists(logFileName)) return;
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            using (StreamWriter writer = new StreamWriter(logFileName, true))
            {
                writer.WriteLine(logMessage);
            }
        }

        public static void LogError(string errorMessage, Exception exception = null)
        {
            if (!File.Exists(logFileName)) return;
            string errorHeader = "***** ERROR *****";
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {errorHeader}\n{errorMessage}";

            if (exception != null)
            {
                logMessage += $"\nException: {exception.GetType().FullName}\nMessage: {exception.Message}";
                logMessage += $"\nStackTrace: {exception.StackTrace}";
            }

            using (StreamWriter writer = new StreamWriter(logFileName, true))
            {
                writer.WriteLine(logMessage);
            }
        }

        public static async Task Open()
        {
            try
            {
                Process.Start("notepad.exe", logFileName);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
            }
        }

    }
}
