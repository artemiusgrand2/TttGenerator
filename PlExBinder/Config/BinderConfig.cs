using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using BCh.KTC.TttEntities.Enums;

namespace BCh.KTC.PlExBinder.Config
{
    internal static class BinderConfig
    {
        public static string GetGidDbConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["gidDb"].ConnectionString;
        }

        public static IList<CategoryTrain> GetCategoriesTrain()
        {
            var result = new List<CategoryTrain>();
            if (ConfigurationManager.AppSettings.AllKeys.Contains("categories"))
            {
                CategoryTrain buffer;
                foreach (var str in ConfigurationManager.AppSettings["categories"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Enum.TryParse<CategoryTrain>(str.Trim(), out buffer))
                        result.Add(buffer);
                }
            }
            if (result.Count == 0)
                result.Add(CategoryTrain.all);
            //
            return result;
        }

        public static int GetCycleTime()
        {
            return int.Parse(ConfigurationManager.AppSettings["cycleTime"]);
        }

        public static string GetUrlCategoriesTrain()
        {
            if (ConfigurationManager.AppSettings.AllKeys.Contains("urlCategories"))
                return ConfigurationManager.AppSettings["urlCategories"];
            //
            return string.Empty;
        }

        public static BinderConfigDto GetBinderConfig()
        {
            var configDto = new BinderConfigDto
            {
                SearchThresholdBeforePlannedTask = int.Parse(ConfigurationManager.AppSettings["searchTimeBeforePlannedTask"]),
                SearchThresholdBeforeCurrentTime = int.Parse(ConfigurationManager.AppSettings["searchTimeBeforeCurrentTime"]),
                DeferredTimeLifespan = int.Parse(ConfigurationManager.AppSettings["deferredTimeLifespan"])
            };
            return configDto;
        }

    }
}