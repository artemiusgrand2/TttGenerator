using System;
using System.Collections.Generic;
using System.Linq;
using BCh.KTC.TttEntities;
using BCh.KTC.PlExBinder.Interfaces;

namespace BCh.KTC.PlExBinder {
  public class DeferredTaskStorage : IDeferredTaskStorage {
    private Dictionary<int, DeferredTask> _tasks;

    public DeferredTaskStorage() {
      _tasks = new Dictionary<int, DeferredTask>();
    }

    public void AddTask(DeferredTask task) {
      _tasks.Add(task.EventId, task);
    }

        public IList<string> CleanUpOldTask(DateTime untilTime)
        {
            var toCleanUp = new List<int>();
            var result = new List<string>();
            foreach (var task in _tasks)
            {
                if (task.Value.PlannedTime < untilTime)
                {
                    toCleanUp.Add(task.Key);
                }
            }
            foreach (var key in toCleanUp)
            {
                result.Add($"Task delete (id: {key}; trId: {_tasks[key].TrainId}; type: {_tasks[key].EventType}; station: {_tasks[key].EventStation}) ,because it timed out");
                _tasks.Remove(key);
            }
            //
            return result;
        }

    public void DeleteAllTasksWithTrainId(int trainId) {
      var toCleanUp = new List<int>();
      foreach (var task in _tasks) {
        if (task.Value.TrainId == trainId) {
          toCleanUp.Add(task.Key);
        }
      }
      foreach (var key in toCleanUp) {
        _tasks.Remove(key);
      }
    }

    public bool DoesTaskExistForEventId(int eventId) {
      return _tasks.ContainsKey(eventId);
    }

    public List<DeferredTask> GetDeferredTasks() {
      return _tasks.Values.ToList();
    }

    public bool DoesSimilarOneExist(string station,
        int eventType, string eventAxis, string neighbourStationCode) {
      var tasks = _tasks.Values.ToList();
      foreach (var task in tasks) {
        if (task.EventStation == station
            && task.EventType == eventType
            && task.EventAxis == eventAxis
            && task.NeighbourStationCode == neighbourStationCode) {
          return true;
        }
      }
      return false;
    }

  }
}
