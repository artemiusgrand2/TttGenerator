using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal {
  public class RouteControlRepository : BaseRepository<RouteControlRecord>, IRepository<RouteControlRecord> {
    private const string SelectCmdText = "SELECT"
    + " RECORDID, STATION, BO_NAME, DO_NAME, MS_TYPE, EV_TIME, BO_TYPE, DO_TYPE, MS_IDN"
    + " FROM TROUTCONT";

    public RouteControlRepository(string conString)
      : base(conString, SelectCmdText) {
    }

    protected override RouteControlRecord RetrieveRecord(FbDataReader dr) {
      var record = new RouteControlRecord();
      record.RecId = dr.GetInt32Safely(0);
      record.StationCode = dr.GetStringSafely(1);
      record.BaseObjectName = dr.GetStringSafely(2);
      record.AddObjectName = dr.GetStringSafely(3);
      record.MessageType = dr.GetInt16Safely(4);
      record.EventTime = dr.GetDateTime(5);
      record.BaseObjectType = dr.GetInt32Safely(6);
      record.AddObjectType = dr.GetInt32Safely(7);
      record.MessageId = dr.GetInt32Safely(8);
      return record;
    }
  }
}
