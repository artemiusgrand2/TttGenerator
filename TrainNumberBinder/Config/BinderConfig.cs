using System.Configuration;

namespace BCh.KTC.TrainNumberBinder.Config {
  internal static class BinderConfig {
    public static string GetGidDbConnectionString() {
      return ConfigurationManager.ConnectionStrings["gidDb"].ConnectionString;
    }

    public static int GetCycleTime() {
      return int.Parse(ConfigurationManager.AppSettings["cycleTime"]);
    }

    public static int GetMaxBindDelta() {
      return int.Parse(ConfigurationManager.AppSettings["maxBindDelta"]);
    }
  }
}