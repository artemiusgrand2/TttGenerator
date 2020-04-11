using BCh.KTC.TttDal;
using BCh.KTC.TttEntities;
using System.Collections.Generic;
using System.Configuration;

namespace BCh.KTC.TttGenerator.Config {
  internal static class GeneratorConfig {

    public static string GetGidDbConnectionString() {
      return ConfigurationManager.ConnectionStrings["gidDb"].ConnectionString;
    }

    public static string GetGidCnfConnectionString() {
      return ConfigurationManager.ConnectionStrings["cnfDb"].ConnectionString;
    }

    public static int GetCycleTime() {
      return int.Parse(ConfigurationManager.AppSettings["cycleTime"]);
    }

    public static int GetReserveTime() {
      return int.Parse(ConfigurationManager.AppSettings["reserveTime"]);
    }

    public static int GetPrevAckTime() {
      return int.Parse(ConfigurationManager.AppSettings["prevAckTime"]);
    }

    public static int GetAdvanceCommandExecutionPeriod() {
      return int.Parse(ConfigurationManager.AppSettings["advanceCmdExecPeriod"]);
    }

    public static Dictionary<string, ControlledStation> GetControlledStations() {
      var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      var engineSection = config.GetSection("engine") as EngineSection;

      var controlledStations = new Dictionary<string, ControlledStation>();
      foreach (ControlledStationElement station in engineSection.ControlledStations) {
        var controlledStation = new ControlledStation(station.Id, station.AllowGeneratingNotCfmArrival, station.AllowGeneratingNotCfmDeparture);
        controlledStations[station.Id] = controlledStation;
      }

      var configRepo = new ConfigRepository(GetGidCnfConnectionString());
      var stationTimes = configRepo.GetStationTimeRecords();
      foreach (StationTimeRecord rec in stationTimes) {
        if (controlledStations.ContainsKey(rec.StationCode)) {
          controlledStations[rec.StationCode].StationTimeRecords.Add(rec);
        }
      }

      return controlledStations;
    }

  }
}
