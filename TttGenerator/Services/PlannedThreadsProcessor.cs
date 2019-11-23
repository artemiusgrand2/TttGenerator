using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttGenerator.Services {
  public static class PlannedThreadsProcessor {
    public static List<PlannedTrainRecord[]> GetPlannedThreads(List<PlannedTrainRecord> list) {
      var retList = new List<PlannedTrainRecord[]>();
      int currentTrainId = -1;
      var currentList = new List<PlannedTrainRecord>();
      foreach (var record in list) {
        if (currentTrainId != record.TrainId) {
          currentTrainId = record.TrainId;
          if (currentList.Count > 0) {
            retList.Add(currentList.ToArray());
            currentList.Clear();
          }
        }
        currentList.Add(record);
      }
      if (currentList.Count > 0) {
        retList.Add(currentList.ToArray());
      }
      return retList;
    }
  }
}
