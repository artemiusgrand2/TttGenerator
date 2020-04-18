using BCh.KTC.TttDal.Interfaces;
using System;
using System.Collections.Generic;
using BCh.KTC.TttEntities;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal {
  public class PassedThreadsRepository : IPassedThreadsRepository {
    private const string SelectSqlTxt = "SELECT EV_REC_IDN, TRAIN_IDN, EV_TYPE, EV_TIME,"
      + " EV_STATION, EV_AXIS, EV_NDO, EV_DOP, EV_NE_STATION, EV_TIME_P"
      + " FROM TGRAPHICID"
      + " WHERE"
      + " EV_STATION = @station"
      + " AND EV_NDO = @ndo"
      + " AND(EV_TYPE = @evType1 OR EV_TYPE = @evType2)"
      + " AND EV_TIME BETWEEN @from AND @till";
    private const string SelectByHeader = "SELECT EV_TYPE, EV_TIME, EV_STATION, EV_NDO FROM tgraphicid"
      + " WHERE TRAIN_IDN = @header";

    private readonly FbCommand _selectCmd;
    private readonly FbParameter _parStation;
    private readonly FbParameter _parNdo;
    private readonly FbParameter _parEvType1;
    private readonly FbParameter _parEvType2;
    private readonly FbParameter _parFrom;
    private readonly FbParameter _parTill;

    private readonly FbCommand _selectByHeader;
    private readonly FbParameter _parHeader;


    private readonly string _conString;


    public PassedThreadsRepository(string conString) {
      _conString = conString;
      _selectCmd = new FbCommand(SelectSqlTxt);
      _parStation = new FbParameter("@station", FbDbType.VarChar, 8);
      //_parAxis = new FbParameter("@axis", FbDbType.VarChar, 8);
      _parNdo = new FbParameter("@ndo", FbDbType.VarChar, 9);
      _parEvType1 = new FbParameter("@evType1", FbDbType.SmallInt);
      _parEvType2 = new FbParameter("@evType2", FbDbType.SmallInt);
      _parFrom = new FbParameter("@from", FbDbType.TimeStamp);
      _parTill = new FbParameter("@till", FbDbType.TimeStamp);
      _selectCmd.Parameters.AddRange(new[] { _parStation, _parNdo, _parEvType1, _parEvType2, _parFrom, _parTill });

      _selectByHeader = new FbCommand(SelectByHeader);
      _parHeader = new FbParameter("@header", FbDbType.Integer);
      _selectByHeader.Parameters.Add(_parHeader);
    }

    public List<PassedTrainRecord> RetrieveByHeader(int header) {
      var records = new List<PassedTrainRecord>();
      using (var con = new FbConnection(_conString)) {
        _selectByHeader.Connection = con;
        _parHeader.Value = header;
        con.Open();
        using (var dbReader = _selectByHeader.ExecuteReader()) {
          // SELECT EV_TYPE, EV_TIME, EV_STATION, EV_NDO FROM tgraphicid
          while (dbReader.Read()) {
            var record = new PassedTrainRecord {
              EventType = dbReader.GetInt16Safely(0),
              EventTime = dbReader.GetMinDateTimeIfNull(1),
              Station = dbReader.GetStringSafely(2),
              Ndo = dbReader.GetStringSafely(3)
            };
            records.Add(record);
          }
        }
      }
      return records;
    }


    public List<PassedTrainRecord> RetrievePassedTrainRecords(
        string station, string ndo, bool isArrival,
        DateTime from, DateTime till) {
      var retRecords = new List<PassedTrainRecord>();
      using (var con = new FbConnection(_conString)) {
        _selectCmd.Connection = con;
        _parStation.Value = station;
        _parNdo.Value = ndo;
        if (isArrival) {
          _parEvType1.Value = 1;
          _parEvType2.Value = 2;
        } else {
          _parEvType1.Value = 3;
          _parEvType2.Value = 3;
        }
        _parFrom.Value = from;
        _parTill.Value = till;
        con.Open();
        using (var dbReader = _selectCmd.ExecuteReader()) {
          while (dbReader.Read()) {
            var record = new PassedTrainRecord {
              RecId = dbReader.GetInt32Safely(0),
              TrainId = dbReader.GetInt32Safely(1),
              EventType = dbReader.GetInt16Safely(2),
              EventTime = dbReader.GetMinDateTimeIfNull(3),
              Station = dbReader.GetStringSafely(4),
              Axis = dbReader.GetStringSafely(5),
              Ndo = dbReader.GetStringSafely(6),
              NdoType = dbReader.GetInt32(7),
              NeighbourStationCode = dbReader.GetStringSafely(8),
              PlannedTime = dbReader.GetMinDateTimeIfNull(9)
            };
            retRecords.Add(record);
          }
          return retRecords;
        }
      }
    }
  }
}
