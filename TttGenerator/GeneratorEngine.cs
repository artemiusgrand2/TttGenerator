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

    private readonly TimeConstraintCalculator _timeConstraintCalculator;
    private readonly Dictionary<string, ControlledStation> _controlledStations;
    private readonly IPlannedThreadsRepository _plannedRepo;
    private readonly ITtTaskRepository _taskRepo;
    private readonly ITrainHeadersRepository _trainHeadersRepo;
    private readonly TimeSpan _prevAckPeriod; // 15 - 20 minutes


    public GeneratorEngine(TimeConstraintCalculator timeConstraintCalculator,
        Dictionary<string, ControlledStation> controlledStations,
        IPlannedThreadsRepository plannedRepo,
        ITtTaskRepository taskRepo,
        ITrainHeadersRepository trainHeadersRepo,
        int prevAckPeriod) {
      _timeConstraintCalculator = timeConstraintCalculator;
      _controlledStations = controlledStations;
      _plannedRepo = plannedRepo;
      _taskRepo = taskRepo;
      _trainHeadersRepo = trainHeadersRepo;
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


    private void ProcessThread(List<PlannedTrainRecord[]> allThreads, PlannedTrainRecord[] threads, int index, List<TtTaskRecord> tasks, DateTime currentTime) {
      // -1 checking whether the station is controlled
      if (!_controlledStations.ContainsKey(threads[index].Station)) {
        _logger.Debug("Not processing - station not controlled. " + threads[index].ToString());
        return;
      }

      // 0 checking already issued tasks
      var existingTask = FindIssuedTask(threads[index].RecId, tasks);
      if (existingTask != null) {
        if (existingTask.SentFlag != 4) {
          _logger.Debug("Not processing -0- command already issued. " + threads[index].ToString());
          return;
        }
        if (++index < threads.Length) {
          ProcessThread(allThreads, threads, index, tasks, currentTime);
        }
        return;
      }

      // 1 self dependency constraints
      bool passed = HaveSelfDependenciesBeenPassed(threads, index, currentTime);
      if (!passed) {
        _logger.Debug("Not processing -1- self-dependecy not passed. " + threads[index].ToString());
        return;
      }

      // 2 other train dependencies constraints
      int dependencyEventReference;
      passed = HaveOtherTrainDependenciesBeenPasssed(allThreads, threads, index, out dependencyEventReference);
      if (!passed) {
        _logger.Debug("Not processing -2- other train dependecies not passed. " + threads[index].ToString());
        return;
      }

      // 3 time constraints
      DateTime executionTime;
      passed = _timeConstraintCalculator.HaveTimeConstraintsBeenPassed(threads, index, currentTime, out executionTime);
      if (!passed) {
        _logger.Debug("Not processing -3- time constraints not passed. " + threads[index].ToString());
        return;
      }

      TtTaskRecord task = CreateTask(threads[index], dependencyEventReference, executionTime);
      _logger.Info($"Task created: {task.PlannedEventReference} - {task.Station}, {task.RouteStartObjectType}:{task.RouteStartObjectName}, {task.RouteEndObjectType}:{task.RouteEndObjectName}");
      _taskRepo.InsertTtTask(task);
      _logger.Info("The task has been written to the database.");

    }

    private TtTaskRecord CreateTask(PlannedTrainRecord plannedTrainRecord,
        int dependecyEventReference,
        DateTime executionTime) {
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
      task.CreationTime = DateTime.Now;
      task.ExecutionTime = executionTime;
      task.PlannedEventReference = plannedTrainRecord.RecId;
      task.DependencyEventReference = dependecyEventReference;
      task.TrainNumber = _trainHeadersRepo.GetTrainNumberByTrainId(plannedTrainRecord.TrainId);
      return task;
    }

    private bool HaveOtherTrainDependenciesBeenPasssed(List<PlannedTrainRecord[]> allThreads,
        PlannedTrainRecord[] thread, int index, out int dependencyEventReference) {
      dependencyEventReference = -1;
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
            break;
          }
        }
      }

      if (previousEvent == null) {
        return true;
      } else if (previousEvent.AckEventFlag != -1) {
        dependencyEventReference = previousEvent.RecId;
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

    private TtTaskRecord FindIssuedTask(int recId, List<TtTaskRecord> tasks) {
      foreach (var task in tasks) {
        if (recId == task.PlannedEventReference) return task;
      }
      return null;
    }
  }
}
