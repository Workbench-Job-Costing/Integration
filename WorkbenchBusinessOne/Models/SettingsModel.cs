using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Workbench.Agent.BusinessOne.Models
{
    public class  SettingsModel
    {
        public string Name { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string Value { get; internal set; }
    }

    public static class SettingsModelList
    {
        public static List<SettingsModel> settingsList = null;

        public static DateTime GetUpdateDate(string name)
        {
            return SettingsList.FirstOrDefault(s => s.Name == name).LastUpdateDate;
        }

        public static void SetUpdateDate(string name, DateTime newUpdateDate)
        {
            SettingsList.FirstOrDefault(s => s.Name == name).LastUpdateDate = newUpdateDate;
            var settings = SettingsList;
            // serialize JSON directly to a file again
            using (StreamWriter file = File.CreateText($@"{Environment.CurrentDirectory}\IntegrationSettings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, SettingsList);
            }
        }
        public static void SetFinCoCode(string finCoCode)
        {
            SettingsList.FirstOrDefault(s => s.Name == "FinCoCode").Value = finCoCode;
            var settings = SettingsList;
            // serialize JSON directly to a file again
            //SettingsList = JsonConvert.DeserializeObject<List<SettingsModel>>(File.ReadAllText($@"C:\Repo\Workbench.Agent.BusinessOne.Service\bin\Release\IntegrationSettings.json"));
#if DEBUG
            using (StreamWriter file = File.CreateText($@"{Environment.CurrentDirectory}\IntegrationSettings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, SettingsList);
            }

#else
            using (StreamWriter file = File.CreateText($@"{ Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\IntegrationSettings.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, SettingsList);
            }
#endif
        }

        public static string GetFinCoCode()
        {
            return SettingsList.FirstOrDefault(s => s.Name == "FinCoCode").Value;
        }

        public static List<SettingsModel> SettingsList 
        {
            get
            {
                if (settingsList == null)
#if DEBUG
                    settingsList = JsonConvert.DeserializeObject<List<SettingsModel>>(File.ReadAllText($@"{Environment.CurrentDirectory}\IntegrationSettings.json"));
#else
                    settingsList = JsonConvert.DeserializeObject<List<SettingsModel>>(File.ReadAllText($@"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\IntegrationSettings.json"));
                          
#endif
                return settingsList;
            }
        }
    }
}
