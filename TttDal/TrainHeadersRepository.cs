using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttDal.Interfaces;

namespace BCh.KTC.TttDal {
  public class TrainHeadersRepository : ITrainHeadersRepository {
    private const string BoundCmdText
      = "SELECT 1 FROM TTRAINHEADERS"
      + " WHERE TRAIN_IDN = @trainId AND FL_SOST = 2";
    private const string SelectTrainNumberCmdText = "SELECT TRAIN_NUM FROM TTRAINHEADERS"
      + " WHERE TRAIN_IDN = @trainId2";


    private readonly string _connectionString;
    private readonly FbCommand _boundCmd;
    private readonly FbCommand _selectTrainNumberCmd;
    private readonly FbParameter _parTrainId;
    private readonly FbParameter _parTrainId2;

    public TrainHeadersRepository(string conString) {
      _connectionString = conString;
      _boundCmd = new FbCommand(BoundCmdText);
      _parTrainId = new FbParameter("@trainId", FbDbType.Integer);
      _boundCmd.Parameters.Add(_parTrainId);
      _selectTrainNumberCmd = new FbCommand(SelectTrainNumberCmdText);
      _parTrainId2 = new FbParameter("@trainId2", FbDbType.Integer);
      _selectTrainNumberCmd.Parameters.Add(_parTrainId2);
    }

    public bool IsTrainThreadBound(int trainId) {
      using (var con = new FbConnection(_connectionString)) {
        _boundCmd.Connection = con;
        _parTrainId.Value = trainId;
        con.Open();
        using (var dbReader = _boundCmd.ExecuteReader()) {
          return dbReader.Read();
        }
      }
    }

    public string GetTrainNumberByTrainId(int trainId) {
      string trainNum = "";
      using (var con = new FbConnection(_connectionString)) {
        _selectTrainNumberCmd.Connection = con;
        _parTrainId2.Value = trainId;
        con.Open();
        using (var dbReader = _selectTrainNumberCmd.ExecuteReader()) {
          if (dbReader.Read()) {
            trainNum = dbReader.GetStringSafely(0);
          }
        }
      }
      return trainNum;
    }

  }
}
