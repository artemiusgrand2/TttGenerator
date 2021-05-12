using BCh.KTC.TttEntities;
using System.Collections.Generic;
using System.Linq;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStation {
    public string StationCode { get; private set; }
    public bool AllowGeneratingNotCfmArrival { get; private set; }
    public bool AllowGeneratingNotCfmDeparture { get; private set; }
    public bool IsGidColtrol { get; private set; }
        public bool IsCrossing { get; private set; }
        public bool IsConstol { get; private set; }

        public IList<string> ListStNotDep { get; private set; } = new List<string>();
        public List<StationTimeRecord> StationTimeRecords { get; private set; }

        public ControlledStation(string stationCode, bool allowGenNotCfmArr, bool allowGenNotCnfDep, bool isCrossing, bool isGidColtrol, bool isConstol, string listStNotDep)
        {
            StationCode = stationCode;
            AllowGeneratingNotCfmArrival = allowGenNotCfmArr;
            AllowGeneratingNotCfmDeparture = allowGenNotCnfDep;
            IsCrossing = isCrossing;
            IsGidColtrol = isGidColtrol;
            IsConstol = isConstol;
            if (!string.IsNullOrEmpty(listStNotDep))
                ListStNotDep = listStNotDep.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
            StationTimeRecords = new List<StationTimeRecord>();
        }
  }
}
