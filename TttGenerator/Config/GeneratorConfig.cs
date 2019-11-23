using BCh.KTC.TttDal;
using BCh.KTC.TttEntities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCh.KTC.TttGenerator.Config {
  public static class GeneratorConfig {

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


    public static Dictionary<string, ControlledStation> GetControlledStations() {
      var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      var engineSection = config.GetSection("engine") as EngineSection;
      var controlledStationCodes = new HashSet<string>();
      foreach (ControlledStationElement station in engineSection.ControlledStations) {
        controlledStationCodes.Add(station.Id);
      }

      var controlledStations = new Dictionary<string, ControlledStation>();
      foreach (string stationCode in controlledStationCodes) {
        var controlledStation = new ControlledStation(stationCode);
        controlledStations[stationCode] = controlledStation;
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
