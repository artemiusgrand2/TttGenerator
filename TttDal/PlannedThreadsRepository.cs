using BCh.KTC.TttDal.Interfaces;
using System;
using System.Collections.Generic;
using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal
{
    public class PlannedThreadsRepository : IPlannedThreadsRepository
    {
        private const string SelectCmdTxt = "SELECT"
        + "    RR.EV_REC_IDN, RR.TRAIN_IDN, RR.EV_TYPE, RR.EV_TIME, RR.EV_TIME_P,"
        + "    RR.EV_STATION, RR.EV_AXIS, RR.EV_NDO, RR.EV_NE_STATION, RR.EV_CNFM,"
        + "    RR.LNKE_REC_IDN, RR.FL_DEF, RR.TRAIN_NUM"
        + " FROM (SELECT TRAIN_IDN, MAX(EV_TIME_p) as MaxTime"
        + "        FROM TGRAPHICPL"
        + "        WHERE EV_TIME_P < @untilTime"
        + "        GROUP BY TRAIN_IDN) R"
        + " INNER JOIN(SELECT"
        + "    P.EV_REC_IDN, P.TRAIN_IDN, P.EV_TYPE, P.EV_TIME, P.EV_TIME_P,"
        + "    P.EV_STATION, P.EV_AXIS, P.EV_NDO, P.EV_NE_STATION, P.EV_CNFM,"
        + "    P.LNKE_REC_IDN, P.FL_DEF, H.TRAIN_NUM"
        + "            FROM TGRAPHICPL AS P"
        + "            INNER JOIN TTRAINHEADERS AS H"
        + "            ON H.TRAIN_IDN = P.TRAIN_IDN"
        + "            WHERE H.FL_SOST = 1 AND P.EV_TIME_P < @untilTime) RR"
        + " ON R.TRAIN_IDN = RR.TRAIN_IDN AND R.MaxTime = RR.EV_TIME_P";

        //SELECT t.Train, t.Dest, r.MaxTime
        //FROM (
        //      SELECT Train, MAX(Time) as MaxTime
        //      FROM TrainTable
        //      GROUP BY Train
        //) r
        //INNER JOIN TrainTable t
        //ON t.Train = r.Train AND t.Time = r.MaxTime

        private const string SelectForTttGenCmdTxt = "SELECT"
        + "    EV_REC_IDN, TRAIN_IDN, EV_TYPE, EV_TIME, EV_TIME_P,"
        + "    EV_STATION, EV_AXIS, EV_NDO, EV_NE_STATION, EV_CNFM,"
        + "    LNKE_REC_IDN, FL_DEF"
        + "    FROM TGRAPHICPL"
        //    + "    WHERE EV_CNFM IS NULL "
        + "    ORDER BY TRAIN_IDN, Ev_Rec_Idn";
        //+ "    ORDER BY TRAIN_IDN, EV_TIME_P";

        private const string SelectByHeaderCmdTxt = "SELECT EV_TYPE, EV_TIME_P, EV_STATION, EV_NDO FROM tgraphicpl"
          + " WHERE TRAIN_IDN = @header";

        private const string SetAckEventFlagCmdTxt = "UPDATE tgraphicpl "
        + " SET EV_CNFM = @flag"
        + " WHERE EV_REC_IDN = @evRecId";

        private readonly string _conString;
        private readonly FbCommand _selectCmd;
        private readonly FbParameter _parUntilTime;
        private readonly FbCommand _selectForTttGeneratorCmd;

        private readonly FbCommand _selectByHeader;
        private readonly FbParameter _parHeader;

        private readonly  FbCommand _setAckEventFlag;
        private readonly  FbParameter _parEvRecId;
        private readonly  FbParameter _parAckEvFlag;

        public PlannedThreadsRepository(string conString)
        {
            _conString = conString;
            _selectCmd = new FbCommand(SelectCmdTxt);
            _parUntilTime = new FbParameter("@untilTime", FbDbType.TimeStamp);
            _selectCmd.Parameters.Add(_parUntilTime);
            _selectForTttGeneratorCmd = new FbCommand(SelectForTttGenCmdTxt);
            //
            _selectByHeader = new FbCommand(SelectByHeaderCmdTxt);
            _parHeader = new FbParameter("@header", FbDbType.Integer);
            _selectByHeader.Parameters.Add(_parHeader);
            //
            _setAckEventFlag = new FbCommand(SetAckEventFlagCmdTxt);
            _parEvRecId = new FbParameter("@evRecId", FbDbType.Integer);
            _parAckEvFlag = new FbParameter("@flag", FbDbType.SmallInt);
            _setAckEventFlag.Parameters.Add(_parEvRecId);
            _setAckEventFlag.Parameters.Add(_parAckEvFlag);
        }

        public List<PlannedTrainRecord> RetrieveByHeader(int header)
        {
            var records = new List<PlannedTrainRecord>();
            using (var con = new FbConnection(_conString))
            {
                _selectByHeader.Connection = con;
                _parHeader.Value = header;
                con.Open();
                using (var dbReader = _selectByHeader.ExecuteReader())
                {
                    // SELECT EV_TYPE, EV_TIME, EV_STATION, EV_NDO FROM tgraphicid
                    while (dbReader.Read())
                    {
                        var record = new PlannedTrainRecord
                        {
                            EventType = dbReader.GetInt16Safely(0),
                            PlannedTime = dbReader.GetMinDateTimeIfNull(1),
                            // ForecastTime = dbReader.GetMinDateTimeIfNull(1),
                            Station = dbReader.GetStringSafely(2),
                            Ndo = dbReader.GetStringSafely(3)
                        };
                        records.Add(record);
                    }
                }
            }
            return records;
        }

        public List<PlannedTrainRecord> RetrievePlannedThreads(DateTime till)
        {
            var records = new List<PlannedTrainRecord>();
            using (var con = new FbConnection(_conString))
            {
                _selectCmd.Connection = con;
                _parUntilTime.Value = till;
                con.Open();
                using (var dbReader = _selectCmd.ExecuteReader())
                {
                    while (dbReader.Read())
                    {
                        /*
                + "    RR.EV_REC_IDN, RR.TRAIN_IDN, RR.EV_TYPE, RR.EV_TIME, RR.EV_TIME_P,"
                + "    RR.EV_STATION, RR.EV_AXIS, RR.EV_NDO, RR.EV_NE_STATION, RR.EV_CNFM,"
                + "    RR.LNKE_REC_IDN, RR.FL_DEF"
                         */
                        var record = new PlannedTrainRecord
                        {
                            RecId = dbReader.GetInt32Safely(0),
                            TrainId = dbReader.GetInt32Safely(1),
                            EventType = dbReader.GetInt16Safely(2),
                            ForecastTime = dbReader.GetDateTime(3),
                            PlannedTime = dbReader.GetDateTime(4),
                            Station = dbReader.GetStringSafely(5),
                            Axis = dbReader.GetStringSafely(6),
                            Ndo = dbReader.GetStringSafely(7),
                            NeighbourStationCode = dbReader.GetStringSafely(8), // ev_ne_station
                                                                                //AckEventFlag { get; set; } // ev_cnfm: 2 - the event has been acknoledged
                            AckEventFlag = dbReader.GetInt16Safely(9),
                            TrainNumber = dbReader.GetStringSafely(12)
                            //PlannedEventReference { get; set; } // lnke_rec_idn
                            //AutopilotState { get; set; } // fl_def
                        };
                        //if (record.Station.Length == 8)
                        //{
                        //    record.Station = record.Station.Substring(2, 6);
                        //    record.NeighbourStationCode = record.NeighbourStationCode.Substring(2, 6);
                        //}
                        records.Add(record);
                    }
                }
            }
            return records;
        }


        public List<PlannedTrainRecord> RetrieveThreadsForTttGenerator(DateTime currentTime)
        {
            var records = new List<PlannedTrainRecord>();
            using (var con = new FbConnection(_conString))
            {
                _selectForTttGeneratorCmd.Connection = con;
                con.Open();
                using (var dbReader = _selectForTttGeneratorCmd.ExecuteReader())
                {
                    while (dbReader.Read())
                    {
                        /*
                + "    RR.EV_REC_IDN, RR.TRAIN_IDN, RR.EV_TYPE, RR.EV_TIME, RR.EV_TIME_P,"
                + "    RR.EV_STATION, RR.EV_AXIS, RR.EV_NDO, RR.EV_NE_STATION, RR.EV_CNFM,"
                + "    RR.LNKE_REC_IDN, RR.FL_DEF"
                         */
                        var record = new PlannedTrainRecord
                        {
                            RecId = dbReader.GetInt32Safely(0),
                            TrainId = dbReader.GetInt32Safely(1),
                            EventType = dbReader.GetInt16Safely(2),
                            ForecastTime = dbReader.GetDateTime(3),
                            PlannedTime = dbReader.GetDateTime(4),
                            Station = dbReader.GetStringSafely(5),
                            Axis = dbReader.GetStringSafely(6),
                            Ndo = dbReader.GetStringSafely(7),
                            NeighbourStationCode = dbReader.GetStringSafely(8),
                            AckEventFlag = dbReader.GetInt16Safely(9),
                            //PlannedEventReference { get; set; } // lnke_rec_idn
                            //AutopilotState { get; set; } // fl_def
                        };
                        if (record.Station.Length == 8)
                        {
                            record.Station = record.Station.Substring(2, 6);
                        }
                        if (record.NeighbourStationCode.Length == 8)
                        {
                            record.NeighbourStationCode = record.NeighbourStationCode.Substring(2, 6);
                        }
                        records.Add(record);
                    }
                }
            }
            return records;
        }




        public bool SetAckEventFlag(int plEvId, short? flag)
        {
            var result = false;
            using (var con = new FbConnection(_conString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    _setAckEventFlag.Connection = con;
                    _setAckEventFlag.Transaction = tx;

                    _parEvRecId.Value = plEvId;
                    _parAckEvFlag.Value = flag;
                    if (_setAckEventFlag.ExecuteNonQuery() > 0)
                        result = true;
                    tx.Commit();
                }
            }
            return result;
        }

    }
}

