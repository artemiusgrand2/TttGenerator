using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStation {
    public string StationCode { get; private set; }
    public List<StationTimeRecord> StationTimeRecords { get; private set; }

    public ControlledStation(string stationCode) {
      StationCode = stationCode;
      StationTimeRecords = new List<StationTimeRecord>();
    }
  }
}
