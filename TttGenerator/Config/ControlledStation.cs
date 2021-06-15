using BCh.KTC.TttEntities;
using System.Collections.Generic;
using System.Linq;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStation {
    public string StationCode { get; private set; }
    public bool AllowGeneratingNotCfmArrival { get; private set; }
    public bool AllowGeneratingNotCfmDeparture { get; private set; }
        public bool IsCrossing { get; private set; }
        public bool Autonomous { get; private set; }

        public IList<string> ListStNotDep { get; private set; } = new List<string>();
        public List<StationTimeRecord> StationTimeRecords { get; private set; }

        public ControlledStation(string stationCode, bool allowGenNotCfmArr, bool allowGenNotCnfDep, bool isCrossing, string listStNotDep, bool autonomous)
        {
            StationCode = stationCode;
            AllowGeneratingNotCfmArrival = allowGenNotCfmArr;
            AllowGeneratingNotCfmDeparture = allowGenNotCnfDep;
            IsCrossing = isCrossing;
            Autonomous = autonomous;
            if (!string.IsNullOrEmpty(listStNotDep))
                ListStNotDep = listStNotDep.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            StationTimeRecords = new List<StationTimeRecord>();
        }
  }
}
