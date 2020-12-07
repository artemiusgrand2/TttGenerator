using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStation {
    public string StationCode { get; private set; }
    public bool AllowGeneratingNotCfmArrival { get; private set; }
    public bool AllowGeneratingNotCfmDeparture { get; private set; }
    public bool IsCrossing { get; private set; }
        public List<StationTimeRecord> StationTimeRecords { get; private set; }

    public ControlledStation(string stationCode, bool allowGenNotCfmArr, bool allowGenNotCnfDep, bool isCrossing) {
      StationCode = stationCode;
      AllowGeneratingNotCfmArrival = allowGenNotCfmArr;
      AllowGeneratingNotCfmDeparture = allowGenNotCnfDep;
      IsCrossing = isCrossing;
      StationTimeRecords = new List<StationTimeRecord>();
    }
  }
}
