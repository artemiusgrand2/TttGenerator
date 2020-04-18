using BCh.KTC.TttEntities;
using System;
using System.Collections.Generic;

namespace BCh.KTC.TttDal.Interfaces {
  public interface IPassedThreadsRepository {
    List<PassedTrainRecord> RetrievePassedTrainRecords(
      string station, string ndo, bool isArrival,
      DateTime from, DateTime till);

    List<PassedTrainRecord> RetrieveByHeader(int header);
  }
}
