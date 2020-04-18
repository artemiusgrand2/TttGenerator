using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttDal.Interfaces;
using System.Collections.Generic;
using BCh.KTC.TttEntities;

namespace BCh.KTC.TttDal {
  public class TrainHeadersRepository : ITrainHeadersRepository {
    private const string BoundCmdText
      = "SELECT 1 FROM TTRAINHEADERS"
      + " WHERE TRAIN_IDN = @trainId AND FL_SOST = 2";
    private const string SelectTrainNumberCmdText = "SELECT TRAIN_NUM FROM TTRAINHEADERS"
      + " WHERE TRAIN_IDN = @trainId2";

    // (TRAIN_NUM <> '' OR TRAIN_NUM is null) - and not having train numbers
    private const string SelectNotBound =
      "SELECT TRAIN_IDN, NORM_IDN, TRAIN_NUM, FL_SOST FROM TTRAINHEADERS"
      + " WHERE (TRAIN_NUM <> '' OR TRAIN_NUM is null) AND ((FL_SOST is null AND (NORM_IDN = 0 or NORM_IDN is null)) OR (FL_SOST = 1))"
      + " ORDER BY FL_SOST";


    private readonly string _connectionString;
    private readonly FbCommand _boundCmd;
    private readonly FbCommand _selectTrainNumberCmd;
    private readonly FbCommand _selectNotBoundCmd;
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
      _selectNotBoundCmd = new FbCommand(SelectNotBound);
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

    public List<TrainHeaderRecord> RetrieveNotBoundHeaders() {
      var headers = new List<TrainHeaderRecord>();
      using (var con = new FbConnection(_connectionString)) {
        _selectNotBoundCmd.Connection = con;
        con.Open();
        using (var dbReader = _selectNotBoundCmd.ExecuteReader()) {
          while (dbReader.Read()) {
            // TRAIN_IDN, NORM_IDN, TRAIN_NUM, FL_SOST 
            var header = new TrainHeaderRecord {
              RecId = dbReader.GetInt32Safely(0),
              PlannedTrainThreadId = dbReader.GetInt32SafelyOr0(1),
              TrainNumber = dbReader.GetStringSafely(2),
              StateFlag = dbReader.GetInt16SafelyOr0(3)
            };
            headers.Add(header);
          }
        }
      }
      return headers;
    }
  }
}
