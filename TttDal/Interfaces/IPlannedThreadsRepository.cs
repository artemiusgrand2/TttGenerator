using BCh.KTC.TttEntities;
using System;
using System.Collections.Generic;

namespace BCh.KTC.TttDal.Interfaces {
  public interface IPlannedThreadsRepository {
    List<PlannedTrainRecord> RetrievePlannedThreads(DateTime till);
    List<PlannedTrainRecord> RetrieveThreadsForTttGenerator(DateTime currentTime);
  }
}
