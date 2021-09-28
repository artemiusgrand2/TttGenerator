using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttDal.Interfaces;
using System.Collections.Generic;
using BCh.KTC.TttEntities;

namespace BCh.KTC.TttDal {
    public class TrainHeadersRepository : ITrainHeadersRepository
    {
        private const string BoundCmdText
          = "SELECT 1 FROM TTRAINHEADERS"
          + " WHERE TRAIN_IDN = @trainId AND FL_SOST = 2";
        private const string SelectTrainNumberCmdText = "SELECT TRAIN_NUM FROM TTRAINHEADERS"
          + " WHERE TRAIN_IDN = @trainId2";

        // (TRAIN_NUM <> '' OR TRAIN_NUM is null) - and not having train numbers
        private const string SelectNotBound =
          "SELECT TRAIN_IDN, NORM_IDN, TRAIN_NUM, FL_SOST FROM TTRAINHEADERS"
          + " WHERE (TRAIN_NUM <> '' OR TRAIN_NUM is null) AND ((FL_SOST is null AND (NORM_IDN = 0 or NORM_IDN is null or NORM_IDN > 0)) OR (FL_SOST = 1 OR FL_SOST = 2))"
          + " ORDER BY FL_SOST";

        private const string SelectPassedIdCmdText
        = "SELECT TRAIN_IDN FROM TTRAINHEADERS"
        + " WHERE NORM_IDN = @normId AND FL_SOST is null";

        private const string SetStateFlagCmdText = "UPDATE TTrainHeaders"
         + " SET Fl_Sost = @FlSost"
         + " WHERE Train_Idn = @trainId3";

        //Удалить события плановой нитки
        private const string DeletePlanEventsCmdText = "DELETE FROM TGraphicPl"
          + " WHERE Train_Idn = @trainIdn4";

        //Установить (сбросить) ссылку на плановую нитку
        private const string BreakNormIdCmdText = "UPDATE TTrainHeaders"
          + " SET Norm_Idn = 0"
          + " WHERE Train_Idn = @trainIdn5";


        private readonly string _connectionString;
        private readonly FbCommand _boundCmd;
        private readonly FbCommand _selectTrainNumberCmd;
        private readonly FbCommand _selectNotBoundCmd;
        private readonly FbCommand _selectPassedIdCmd;
        private readonly FbCommand _setStatFlagCmd;

        private FbCommand _deletePlanEventsCmd;
        private FbCommand _breakNordIdCmd;


        private readonly FbParameter _parTrainId;
        private readonly FbParameter _parTrainId2;
        private readonly FbParameter _parTrainId3;
        private readonly FbParameter _parTrainId4;
        private readonly FbParameter _parTrainId5;
        private readonly FbParameter _parNormId;
        private readonly FbParameter _parStatFlag;




        public TrainHeadersRepository(string conString)
        {
            _connectionString = conString;
            _boundCmd = new FbCommand(BoundCmdText);
            _parTrainId = new FbParameter("@trainId", FbDbType.Integer);
            _boundCmd.Parameters.Add(_parTrainId);
            _selectTrainNumberCmd = new FbCommand(SelectTrainNumberCmdText);
            _parTrainId2 = new FbParameter("@trainId2", FbDbType.Integer);
            _selectTrainNumberCmd.Parameters.Add(_parTrainId2);
            _selectNotBoundCmd = new FbCommand(SelectNotBound);
            //
            _selectPassedIdCmd = new FbCommand(SelectPassedIdCmdText);
            _parNormId = new FbParameter("@normId", FbDbType.Integer);
            _selectPassedIdCmd.Parameters.Add(_parNormId);
            //
            _setStatFlagCmd = new FbCommand(SetStateFlagCmdText);
            _parTrainId3 = new FbParameter("@trainId3", FbDbType.Integer);
            _parStatFlag = new FbParameter("@FlSost", FbDbType.Integer);
            _setStatFlagCmd.Parameters.Add(_parTrainId3);
            _setStatFlagCmd.Parameters.Add(_parStatFlag);

            _deletePlanEventsCmd = new FbCommand(DeletePlanEventsCmdText);
            _parTrainId4 = new FbParameter("@trainIdn4", FbDbType.Integer);
            _deletePlanEventsCmd.Parameters.Add(_parTrainId4);

            _breakNordIdCmd = new FbCommand(BreakNormIdCmdText);
            _parTrainId5 = new FbParameter("@trainIdn5", FbDbType.Integer);
            _breakNordIdCmd.Parameters.Add(_parTrainId5);
        }

        public bool IsTrainThreadBound(int trainId)
        {
            using (var con = new FbConnection(_connectionString))
            {
                _boundCmd.Connection = con;
                _parTrainId.Value = trainId;
                con.Open();
                using (var dbReader = _boundCmd.ExecuteReader())
                {
                    return dbReader.Read();
                }
            }
        }

        public string GetTrainNumberByTrainId(int trainId)
        {
            string trainNum = "";
            using (var con = new FbConnection(_connectionString))
            {
                _selectTrainNumberCmd.Connection = con;
                _parTrainId2.Value = trainId;
                con.Open();
                using (var dbReader = _selectTrainNumberCmd.ExecuteReader())
                {
                    if (dbReader.Read())
                    {
                        trainNum = dbReader.GetStringSafely(0);
                    }
                }
            }
            return trainNum;
        }

        public List<TrainHeaderRecord> RetrieveNotBoundHeaders()
        {
            var headers = new List<TrainHeaderRecord>();
            using (var con = new FbConnection(_connectionString))
            {
                _selectNotBoundCmd.Connection = con;
                con.Open();
                using (var dbReader = _selectNotBoundCmd.ExecuteReader())
                {
                    while (dbReader.Read())
                    {
                        // TRAIN_IDN, NORM_IDN, TRAIN_NUM, FL_SOST 
                        var header = new TrainHeaderRecord
                        {
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

        public int? GetPassedIdByPlannedId(int trainId)
        {
            int? passedId = null;
            using (var con = new FbConnection(_connectionString))
            {
                _selectPassedIdCmd.Connection = con;
                _parNormId.Value = trainId;
                con.Open();
                using (var dbReader = _selectPassedIdCmd.ExecuteReader())
                {
                    if (dbReader.Read())
                    {
                        passedId = dbReader.GetInt32(0);
                    }
                }
            }
            return passedId;
        }

        public bool SetStateFlag(int trainId, int statFlag)
        {
            var result = false;
            using (var con = new FbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    _setStatFlagCmd.Connection = con;
                    _setStatFlagCmd.Transaction = tx;

                    _parTrainId3.Value = trainId;
                    _parStatFlag.Value = statFlag;
                    if (_setStatFlagCmd.ExecuteNonQuery() > 0)
                        result = true;
                    tx.Commit();
                }
            }
            return result;
        }

        public bool DeletePlanRope(int trainId)
        {
            var result = false;
            using (var connection = new FbConnection(_connectionString))
            {
                connection.Open();
                //
                using (var transaction = connection.BeginTransaction())
                {
                    _deletePlanEventsCmd.Connection = connection;
                    _deletePlanEventsCmd.Transaction = transaction;
                    //
                    _breakNordIdCmd.Connection = connection;
                    _breakNordIdCmd.Transaction = transaction;
                    _parTrainId4.Value = trainId;
                    _parTrainId5.Value = trainId;

                    if (_deletePlanEventsCmd.ExecuteNonQuery() > 0)
                        result = true;
                    _breakNordIdCmd.ExecuteNonQuery();
                    //
                    transaction.Commit();
                }
                connection.Close();
            }
            //
            return result;
        }
    }
}
