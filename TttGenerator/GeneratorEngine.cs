using BCh.KTC.TttDal.Interfaces;
using BCh.KTC.TttEntities;
using BCh.KTC.TttGenerator.Config;
using BCh.KTC.TttGenerator.Services;
using log4net;
using System;
using System.Collections.Generic;

namespace BCh.KTC.TttGenerator {
  public class GeneratorEngine {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(GeneratorEngine));

    private readonly Dictionary<string, ControlledStation> _controlledStations;
    private readonly IPlannedThreadsRepository _plannedRepo;
    private readonly ITtTaskRepository _taskRepo;
    private readonly ITrainHeadersRepository _trainHeadersRepo;
    private readonly int _reserveTime; // 1 - 2 minutes
    private readonly TimeSpan _prevAckPeriod; // 15 - 20 minutes


    public GeneratorEngine(Dictionary<string, ControlledStation> controlledStations,
        IPlannedThreadsRepository plannedRepo,
        ITtTaskRepository taskRepo,
        ITrainHeadersRepository trainHeadersRepo,
        int reserveTime,
        int prevAckPeriod) {
      _controlledStations = controlledStations;
      _plannedRepo = plannedRepo;
      _taskRepo = taskRepo;
      _trainHeadersRepo = trainHeadersRepo;
      _reserveTime = reserveTime;
      _prevAckPeriod = new TimeSpan(0, prevAckPeriod, 0);
    }


    public void PerformWorkingCycle(DateTime currentTime) {
      var plannedRecords = _plannedRepo.RetrieveThreadsForTttGenerator(currentTime);
      var plannedThreads = PlannedThreadsProcessor.GetPlannedThreads(plannedRecords);
      var issuedTasks = _taskRepo.GetTtTasks();
      foreach (var thread in plannedThreads) {
        int i = GetIndexOfLastNotConfirmedRecord(thread);
        if (i != thread.Length) {
          ProcessThread(plannedThreads, thread, i, issuedTasks, currentTime);
        }
      }
    }


    private int GetIndexOfLastNotConfirmedRecord(PlannedTrainRecord[] thread) {
      int i = thread.Length;
      while (i > 0) {
        if (thread[i - 1].AckEventFlag == -1) {
          --i;
          if (i == 0) break;
        } else {
          break;
        }
      }
      return i;
    }


    private void ProcessThread(List<PlannedTrainRecord[]> allThreads, PlannedTrainRecord[] thread, int index, List<TtTaskRecord> tasks, DateTime currentTime) {
      // -1 checking whether the station is controlled
      if (!_controlledStations.ContainsKey(thread[index].Station)) return;

      // 0 checking already issued tasks
      var existingTask = FindIssuedTask(thread[index].RecId, tasks);
      if (existingTask != null) {
        if (existingTask.SentFlag != 4) {
          return;
        }
        if (++index < thread.Length) {
          ProcessThread(allThreads, thread, index, tasks, currentTime);
        }
        return;
      }

      // 1 time constraints
      bool passed = HaveTimeConstraintsBeenPassed(thread, index, currentTime);
      if (!passed) return;

      // 2 self dependency constraints
      passed = HaveSelfDependenciesBeenPassed(thread, index, currentTime);
      if (!passed) return;

      // 3 other train dependencies constraints
      passed = HaveOtherTrainDependenciesBeenPasssed(allThreads, thread, index);
      if (!passed) return;

      TtTaskRecord task = CreateTask(thread[index]);
      _logger.Info($"Task created: {task.PlannedEventReference} - {task.Station}, {task.RouteEndObjectType}:{task.RouteEndObjectName}, {task.RouteEndObjectType}:{task.RouteEndObjectName}");
      _taskRepo.InsertTtTask(task);
      _logger.Info("The task has been written to the database.");

    }

    private TtTaskRecord CreateTask(PlannedTrainRecord plannedTrainRecord) {
      var task = new TtTaskRecord();
      task.Station = plannedTrainRecord.Station;
      // ?!!! there might be situations of moving from a track to another track
      if (plannedTrainRecord.EventType != 3) { // Arrival
        task.RouteStartObjectType = 5;
        task.RouteStartObjectName = plannedTrainRecord.Ndo;
        task.RouteEndObjectType = 3;
        task.RouteEndObjectName = plannedTrainRecord.Axis;
      } else { // Departure
        task.RouteStartObjectType = 3;
        task.RouteStartObjectName = plannedTrainRecord.Axis;
        task.RouteEndObjectType = 5;
        task.RouteEndObjectName = plannedTrainRecord.Ndo;
      }
      task.CreationTime = task.ExecutionTime = DateTime.Now;
      task.PlannedEventReference = plannedTrainRecord.RecId;
      task.TrainNumber = _trainHeadersRepo.GetTrainNumberByTrainId(plannedTrainRecord.TrainId);
      return task;
    }

    private bool HaveOtherTrainDependenciesBeenPasssed(List<PlannedTrainRecord[]> allThreads,
        PlannedTrainRecord[] thread, int index) {
      PlannedTrainRecord previousEvent = null;
      foreach (PlannedTrainRecord[] aThread in allThreads) {
        if (aThread == thread) continue;
        for (int i = aThread.Length - 1; i >= 0; --i) {
          if (aThread[i].PlannedTime > thread[index].PlannedTime) continue;

          bool eventFound = false;
          if (thread[index].EventType != 3) { // Arrival
            if (aThread[i].Station == thread[index].Station
                && ((aThread[i].EventType == 2 && aThread[i].Ndo == thread[index].Ndo)
                  || (aThread[i].EventType == 3 && aThread[i].Axis == thread[index].Axis))) {
              eventFound = true;
            }
          } else { // Depature
            if (aThread[i].Station == thread[index].Station
                && aThread[i].Ndo == thread[index].Ndo) {
              eventFound = true;
            }
          }
          if (eventFound) {
            if (previousEvent == null) {
              previousEvent = aThread[i];
            } else if (aThread[i].PlannedTime > previousEvent.PlannedTime) {
              previousEvent = aThread[i];
            }
          }
          break;
        }
      }

      if (previousEvent == null) {
        return true;
      } else if (previousEvent.AckEventFlag != -1) {
        return true;
      }
      return false;
    }


    private bool HaveSelfDependenciesBeenPassed(PlannedTrainRecord[] thread,
        int index, DateTime currentTime) {
      int i = index;
      while (--i >= 0) {
        if (thread[index].PlannedTime - thread[i].PlannedTime < _prevAckPeriod) {
          if (thread[i].AckEventFlag != -1) {
            return true;
          }
        } else {
          if (index - i == 1
              && thread[i].AckEventFlag != -1
              && thread[index].PlannedTime - currentTime < _prevAckPeriod) {
            return true;
          }
          return false;
        }
      }
      return true;
    }


    private bool HaveTimeConstraintsBeenPassed(PlannedTrainRecord[] thread, int index, DateTime currentTime) {
      int delta = 0;
      if (!_controlledStations.ContainsKey(thread[index].Station)) {
        _logger.Error($"Configuration for station {thread[index].Station} not found!");
        delta = CalculateDefaultDelta(thread, index);
      } else {
        var station = _controlledStations[thread[index].Station];
        if (thread[index].EventType != 3) { // arrival
          delta = GetConfiguredTimeInterval(station, 1, thread[index])
            + GetConfiguredTimeInterval(station, 2, thread[index])
            + GetConfiguredTimeInterval(station, 5, thread[index]);
        } else { // departure
          if (index != 0) {
            var diff = thread[index].PlannedTime - thread[index - 1].PlannedTime;
            delta = (diff > new TimeSpan(0, 10, 0))
              ? GetConfiguredTimeInterval(station, 3, thread[index]) + _reserveTime
              : GetConfiguredTimeInterval(station, 2, thread[index]) + _reserveTime;
          } else {
            delta = GetConfiguredTimeInterval(station, 3, thread[index]) + _reserveTime;
          }
        }
        if (delta == 0) {
          delta = CalculateDefaultDelta(thread, index);
        }
      }
      return thread[index].PlannedTime.AddMinutes(-delta) < currentTime;
    }


    private int CalculateDefaultDelta(PlannedTrainRecord[] thread, int index) {
      int delta = 0;
      // defaults: 1:5; 2:1; 3:2; 4:3; 5:1;
      if (thread[index].EventType != 3) { // arrival
        delta = 5 + 1 + 1 + _reserveTime;
      } else { // departure
        if (index == 0) {
          delta = 2 + _reserveTime;
        } else {
          var diff = thread[index].PlannedTime - thread[index - 1].PlannedTime;
          delta = (diff > new TimeSpan(0, 10, 0))
            ? 2 + _reserveTime
            : 1 + _reserveTime;
        }
      }
      return delta;
    }


    private int GetConfiguredTimeInterval(ControlledStation station, int intervalType, PlannedTrainRecord trainRecord) {
      foreach (var timeRecord in station.StationTimeRecords) {
        if (timeRecord.TimeType != intervalType) continue;
        string objStart;
        string objEnd;
        if (trainRecord.EventType != 3) { // arrival
          objStart = trainRecord.Ndo;
          objEnd = trainRecord.Axis;
        } else {
          objStart = trainRecord.Axis;
          objEnd = trainRecord.Ndo;
        }
        switch (timeRecord.TimeType) {
          case 1:
          case 4:
            if (timeRecord.StartObjectName == objStart) {
              return timeRecord.TimeValue;
            }
            break;
          case 2:
          case 3:
          case 5:
            if (timeRecord.StartObjectName == objStart
                && timeRecord.EndObjectName == objEnd) {
              return timeRecord.TimeValue;
            }
            break;
          default: _logger.Error($"Unknown time type found ({timeRecord.TimeType}) for station {station.StationCode}");
            break;
        }
      }
      return 0;
    }

    private TtTaskRecord FindIssuedTask(int recId, List<TtTaskRecord> tasks) {
      foreach (var task in tasks) {
        if (recId == task.PlannedEventReference) return task;
      }
      return null;
    }
  }
}
