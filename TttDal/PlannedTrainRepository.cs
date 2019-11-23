using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal {
  public class PlannedTrainRepository : BaseUpdatingRepository<PlannedTrainRecord>, IUpdatingRepository<PlannedTrainRecord> {
    private const string SelectCmdText = "SELECT"
    + " EV_REC_IDN, TRAIN_IDN, EV_TYPE, EV_TIME, EV_TIME_P, EV_STATION, EV_AXIS, EV_NDO,"
    + " EV_NE_STATION, EV_CNFM, LNKE_REC_IDN, FL_DEF"
    + " FROM TGRAPHICPL ORDER BY TRAIN_IDN, EV_TIME_P";
    private const string UpdateCmdText = "";

    public PlannedTrainRepository(string conString)
      : base(conString, SelectCmdText, UpdateCmdText) {
    }

    protected override PlannedTrainRecord RetrieveRecord(FbDataReader dr) {
      var record = new PlannedTrainRecord();
      record.RecId = dr.GetInt32Safely(0);
      record.TrainId = dr.GetInt32Safely(1);
      record.EventType = dr.GetInt16Safely(2);
      record.ForecastTime = dr.GetDateTime(3);
      record.PlannedTime = dr.GetDateTime(4);
      record.Station = dr.GetStringSafely(5);
      record.Axis = dr.GetStringSafely(6);
      record.Ndo = dr.GetStringSafely(7);
      record.NeighbourStationCode = dr.GetStringSafely(8);
      record.AckEventFlag = dr.GetInt16Safely(9);
      record.PlannedEventReference = dr.GetInt32Safely(10);
      record.AutopilotState = dr.GetInt16Safely(11);
      return record;
    }

    protected override void UpdateRecord(PlannedTrainRecord record) {
      throw new System.NotImplementedException();
    }
  }
}
