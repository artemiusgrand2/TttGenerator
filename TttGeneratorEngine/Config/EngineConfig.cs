using System.Configuration;

namespace KTC.BCh.TttGeneratorEngine.Config {
  internal static class EngineConfig {

    public static string GetGidDbConnectionString() {
      return ConfigurationManager.ConnectionStrings["gidDb"].ConnectionString;
    }

    public static int GetCycleTime() {
      return int.Parse(ConfigurationManager.AppSettings["cycleTime"]);
    }
  }
}
