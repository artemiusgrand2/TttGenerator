using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal {
  public class PassedTrainRepository : BaseRepository<PassedTrainRecord>, IRepository<PassedTrainRecord> {
    private const string SelectCmdText = "SELECT"
    + " EV_REC_IDN, TRAIN_IDN, EV_TYPE, EV_TIME, EV_STATION, EV_AXIS, EV_NDO, EV_DOP,"
    + " EV_NE_STATION, EV_TIME_P"
    + " FROM TGRAPHICID";

    public PassedTrainRepository(string conString)
      : base(conString, SelectCmdText) {
    }

    protected override PassedTrainRecord RetrieveRecord(FbDataReader dr) {
      var record = new PassedTrainRecord();
      record.RecId = dr.GetInt32Safely(0);
      record.TrainId = dr.GetInt32Safely(1);
      record.EventType = dr.GetInt16Safely(2);
      record.EventTime = dr.GetDateTime(3);
      record.Station = dr.GetStringSafely(4);
      record.Axis = dr.GetStringSafely(5);
      record.Ndo = dr.GetStringSafely(6);
      record.NdoType = dr.GetInt32Safely(7);
      record.NeighbourStationCode = dr.GetStringSafely(8);
      record.PlannedTime = dr.GetDateTime(9);
      return record;
    }
  }
}
