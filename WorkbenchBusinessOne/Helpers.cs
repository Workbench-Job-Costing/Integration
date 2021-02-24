using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Workbench.Agent.BusinessOne
{
    public static class Helpers
    {
        public static string FromBase64(this string encoded)
        {
            byte[] data = System.Convert.FromBase64String(encoded);
            var base64Decoded = System.Text.UTF8Encoding.Default.GetString(data);
            return base64Decoded;
        }

        public static string ToBase64(this string decoded)
        {
            var base64Decoded = System.Text.UTF8Encoding.Default.GetBytes(decoded);
            var encoded = System.Convert.ToBase64String(base64Decoded);
            return encoded;
        }

        public static void LogError(this Exception ex, string message = null)
        {
            var textMessage = message + " \r\n" ?? "";
            textMessage += ex?.ToString() ?? "";

            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine(textMessage);
            //Console.ResetColor();

            EventLog.WriteEntry(
                    $"{ConfigurationManager.AppSettings["ServiceDisplayName"]}",
                    textMessage,
                    EventLogEntryType.Error,
                    1000);
        }

        public static void LogAppError(string errorMessage)
        {
            EventLog.WriteEntry(
               $"{ConfigurationManager.AppSettings["ServiceDisplayName"]}",
               $"{errorMessage}\r\n{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)} \r\nv1",
               EventLogEntryType.Information,
               1000);
        }


        public static void LogInfo(string info)
        {
            EventLog.WriteEntry(
                    $"{ConfigurationManager.AppSettings["ServiceDisplayName"]}",
                    $"{info}\r\n{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)} \r\nv1",
                    EventLogEntryType.Information,
                    1000);
        }
    }
}
