using System.Configuration;

namespace BCh.KTC.PlExBinder.Config {
  internal static class BinderConfig {
    public static string GetGidDbConnectionString() {
      return ConfigurationManager.ConnectionStrings["gidDb"].ConnectionString;
    }

    public static int GetCycleTime() {
      return int.Parse(ConfigurationManager.AppSettings["cycleTime"]);
    }

    public static BinderConfigDto GetBinderConfig() {
      var configDto = new BinderConfigDto {
        SearchThresholdBeforePlannedTask = int.Parse(ConfigurationManager.AppSettings["searchTimeBeforePlannedTask"]),
        SearchThresholdBeforeCurrentTime = int.Parse(ConfigurationManager.AppSettings["searchTimeBeforeCurrentTime"]),
        DeferredTimeLifespan = int.Parse(ConfigurationManager.AppSettings["deferredTimeLifespan"])
      };
      return configDto;
    }

  }
}