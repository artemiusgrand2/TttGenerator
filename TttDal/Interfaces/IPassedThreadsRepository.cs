using BCh.KTC.TttEntities;
using System;
using System.Collections.Generic;

namespace BCh.KTC.TttDal.Interfaces {
  public interface IPassedThreadsRepository {
    List<PassedTrainRecord> RetrievePassedTrainRecords(
      string station, string /* string ndo*/neighbourStationCode, bool isArrival,
      DateTime from, DateTime till);

      List<PassedTrainRecord> RetrieveByHeader(int header);

        List<PassedTrainRecord> GetLastTrainRecordsForStation(int header);
    }
}
