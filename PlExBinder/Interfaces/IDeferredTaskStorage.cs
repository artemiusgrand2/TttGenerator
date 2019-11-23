using BCh.KTC.TttEntities;
using System;
using System.Collections.Generic;

namespace BCh.KTC.PlExBinder.Interfaces {
  public interface IDeferredTaskStorage {
    bool DoesTaskExistForEventId(int eventId);
    void CleanUpOldTask(DateTime untilTime);
    void AddTask(DeferredTask task);
    void DeleteAllTasksWithTrainId(int trainId);
    List<DeferredTask> GetDeferredTasks();
    bool DoesSimilarOneExist(string station, int eventType, string eventAxis, string eventNdo);
  }
}
