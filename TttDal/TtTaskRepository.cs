using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttDal.Interfaces;
using System.Collections.Generic;

namespace BCh.KTC.TttDal {
  public class TtTaskRepository : ITtTaskRepository {
    private const string SelectCmdText = "SELECT"
    + " DEF_IDN, ST_CODE, TR_NUM_P, TR_NUM, TR_NUM_S, OB_STT_TYPE, OB_STT_NAME,"
    + " OB_END_TYPE, OB_END_NAME, STAY_FND, LNK_DEF_IDN_N, LNK_DEF_IDN_E,"
    + " EV_IDN_PLN, TM_DEF_START, TM_DEF_CREAT, STD_FORM, FL_SND, RUN_CODE, CHK_SUM"
    + " FROM TCOMDEFINITIONS"
    + " ORDER BY EV_IDN_PLN";
    private const string InsertCmdText = "INSERT INTO TCOMDEFINITIONS"
    + " (ST_CODE, TR_NUM,"
    + " OB_STT_TYPE, OB_STT_NAME, OB_END_TYPE, OB_END_NAME,"
    + " EV_IDN_PLN, TM_DEF_START, TM_DEF_CREAT, STD_FORM, FL_SND)"
    + " VALUES (@station, @trainNumber, @startObjType, @startObjName, @endObjType, @endObjName,"
    + " @eventRecId, @execTime, @creationTime, 2, 0)";
    
    private readonly string _conString;
    private readonly FbCommand _selectCmd;
    private readonly FbCommand _insertCmd;
    private readonly FbParameter _parStation;
    private readonly FbParameter _parTrainNumber;
    private readonly FbParameter _parStartObjType;
    private readonly FbParameter _parStartObjName;
    private readonly FbParameter _parEndObjType;
    private readonly FbParameter _parEndObjName;
    private readonly FbParameter _parEventRecId;
    private readonly FbParameter _parExecTime;
    private readonly FbParameter _parCreationTime;

    public TtTaskRepository(string conString) {
      _conString = conString;
      _selectCmd = new FbCommand(SelectCmdText);
      _insertCmd = new FbCommand(InsertCmdText);
      _parStation = new FbParameter("@station", FbDbType.Integer);
      _parTrainNumber = new FbParameter("@trainNumber", FbDbType.Char, 4);
      _parStartObjType = new FbParameter("@startObjType", FbDbType.SmallInt);
      _parStartObjName = new FbParameter("@startObjName", FbDbType.Char, 8);
      _parEndObjType = new FbParameter("@endObjType", FbDbType.SmallInt);
      _parEndObjName = new FbParameter("@endObjName", FbDbType.Char, 8);
      _parEventRecId = new FbParameter("@eventRecId", FbDbType.Integer);
      _parExecTime = new FbParameter("@execTime", FbDbType.TimeStamp);
      _parCreationTime = new FbParameter("@creationTime", FbDbType.TimeStamp);
      _insertCmd.Parameters.AddRange(new[] {
        _parStation,
        _parTrainNumber,
        _parStartObjType,
        _parStartObjName,
        _parEndObjType,
        _parEndObjName,
        _parEventRecId,
        _parExecTime,
        _parCreationTime,
      });
    }


    public void InsertTtTask(TtTaskRecord task) {
      using (var con = new FbConnection(_conString)) {
        con.Open();
        using (var tx = con.BeginTransaction()) {
          _insertCmd.Connection = con;
          _insertCmd.Transaction = tx;

          _parStation.Value = int.Parse(task.Station);
          _parTrainNumber.Value = task.TrainNumber;
          _parStartObjType.Value = task.RouteStartObjectType;
          _parStartObjName.Value = task.RouteStartObjectName;
          _parEndObjType.Value = task.RouteEndObjectType;
          _parEndObjName.Value = task.RouteEndObjectName;
          _parEventRecId.Value = task.PlannedEventReference;
          _parExecTime.Value = task.ExecutionTime;
          _parCreationTime.Value = task.CreationTime;

          _insertCmd.ExecuteNonQuery();
          tx.Commit();
        }
      }
    }

    public List<TtTaskRecord> GetTtTasks() {
      var retRecords = new List<TtTaskRecord>();
      using (var con = new FbConnection(_conString)) {
        con.Open();
        _selectCmd.Connection = con;
        using (var dr = _selectCmd.ExecuteReader()) {
          while (dr.Read()) {
            var record = new TtTaskRecord();
            record.RecId = dr.GetInt32Safely(0);
            record.Station = dr.GetStringSafely(1);
            record.TrainPrefix = dr.GetStringSafely(2);
            record.TrainNumber = dr.GetStringSafely(3);
            record.TrainSuffix = dr.GetStringSafely(4);
            record.RouteStartObjectType = dr.GetInt16Safely(5);
            record.RouteStartObjectName = dr.GetStringSafely(6);
            record.RouteEndObjectType = dr.GetInt16Safely(7);
            record.RouteEndObjectName = dr.GetStringSafely(8);
            record.StopFlag = dr.GetInt16Safely(9);
            //record.SelfLink = dr.GetInt32Safely(10);
            record.DependencyEventReference = dr.GetInt32Safely(11);
            record.PlannedEventReference = dr.GetInt32Safely(12);
            record.ExecutionTime = dr.GetDateTime(13);
            record.CreationTime = dr.GetDateTime(14);
            record.FormationFlag = dr.GetInt16Safely(15);
            record.SentFlag = dr.GetInt16Safely(16);
            record.ExecutionCode = dr.GetStringSafely(17);
            retRecords.Add(record);
          }
        }
        return retRecords;
      }
    }
  }
}
