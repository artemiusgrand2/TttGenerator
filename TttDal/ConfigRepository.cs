using BCh.KTC.TttDal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal {
  public class ConfigRepository : IConfigRepository {
    private const string StationTimeCmdText = "SELECT TIME_TYPE, ST_CODE, OB_STT_TYPE, OB_STT_NAME, OB_END_TYPE, OB_END_NAME, TTIME FROM TTIMES";
    private readonly string _conString;

    public ConfigRepository(string conString) {
      _conString = conString;
    }

    public List<StationTimeRecord> GetStationTimeRecords() {
      var retRecords = new List<StationTimeRecord>();
      using (var con = new FbConnection(_conString))
      using (var cmd = new FbCommand(StationTimeCmdText)) {
        cmd.Connection = con;
        con.Open();
        using (var dbReader = cmd.ExecuteReader()) {
          while(dbReader.Read()) {
            var record = new StationTimeRecord {
              TimeType = dbReader.GetInt16Safely(0),
              StationCode = dbReader.GetInt32Safely(1).ToString(),
              StartObjectType = dbReader.GetInt16Safely(2),
              StartObjectName = dbReader.GetStringSafely(3).TrimEnd(),
              EndObjectType = dbReader.GetInt16Safely(4),
              EndObjectName = dbReader.GetStringSafely(5).TrimEnd(),
              TimeValue = dbReader.GetInt16Safely(6)
            };
            retRecords.Add(record);
          }
        }
      }
      return retRecords;
    }

  }
}
