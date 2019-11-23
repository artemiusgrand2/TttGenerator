﻿using BCh.KTC.PlExBinder.Config;
using BCh.KTC.PlExBinder.Interfaces;
using BCh.KTC.TttDal.Interfaces;
using BCh.KTC.TttEntities;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BCh.KTC.PlExBinder {
  public class BinderEngine {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(BinderEngine));

    private readonly IPlannedThreadsRepository _plannedThreadsRepository;
    private readonly IPassedThreadsRepository _passedThreadsRepository;
    private readonly ITrainHeadersRepository _trainHeadersRepository;
    private readonly IStoredProcExecutor _storedProceduresExecutor;
    private readonly IDeferredTaskStorage _deferredTaskStorage;
    private readonly BinderConfigDto _config;


    public BinderEngine(IPlannedThreadsRepository plannedThreadsRepository,
        IPassedThreadsRepository passedThreadsRepository,
        ITrainHeadersRepository trainHeadersRepository,
        IStoredProcExecutor storedProceduresExecutor,
        IDeferredTaskStorage deferredTaskStorage,
        BinderConfigDto config) {
      _plannedThreadsRepository = plannedThreadsRepository;
      _passedThreadsRepository = passedThreadsRepository;
      _trainHeadersRepository = trainHeadersRepository;
      _storedProceduresExecutor = storedProceduresExecutor;
      _deferredTaskStorage = deferredTaskStorage;
      _config = config;
    }

    public void ExecuteBindingCycle(DateTime executionTime) {
      ExecuteDeferredTasks(executionTime);
      List<PlannedTrainRecord> plannedRecords = _plannedThreadsRepository.RetrievePlannedThreads(executionTime);
      plannedRecords = FilterOutAlreadyDefinedInDeferredTasks(plannedRecords);
      FormDeferredTasks(plannedRecords);
    }

    private void ExecuteDeferredTasks(DateTime executionTime) {
      _deferredTaskStorage.CleanUpOldTask(DateTime.Now.AddMinutes(-_config.DeferredTimeLifespan));
      CleanUpBoundTasks();

      // execution itself
      var tasks = _deferredTaskStorage.GetDeferredTasks();
      foreach (var task in tasks) {
        List<PassedTrainRecord> candidates = _passedThreadsRepository.RetrievePassedTrainRecords(
          task.EventStation, task.EventNdoObject, task.EventType == 2,
          task.PlannedTime.AddMinutes(-_config.SearchThresholdBeforePlannedTask),
          executionTime.AddMinutes(-_config.SearchThresholdBeforeCurrentTime));
        if (candidates.Count > 0) {
          var lastCandidateId = candidates.Last().TrainId;
          _storedProceduresExecutor.BindPlannedAndPassedTrains(task.TrainId, lastCandidateId);
          task.HasBindingCmdBeenGenerated = true;
          _logger.Info(string.Format("Binding - planned: {0} and passed: {1}", task.TrainId, lastCandidateId));
        }
      }
    }


    private void CleanUpBoundTasks() {
      List<DeferredTask> tasks = _deferredTaskStorage.GetDeferredTasks();
      foreach (var task in tasks) {
        if (task.HasBindingCmdBeenGenerated) {
          bool isBound = _trainHeadersRepository.IsTrainThreadBound(task.TrainId);
          if (isBound) {
            _deferredTaskStorage.DeleteAllTasksWithTrainId(task.TrainId);
            _logger.InfoFormat("Cleaned up tasks for the bound thread (TrainId: {0})", task.TrainId);
          }
        }
      }
    }

    private void FormDeferredTasks(List<PlannedTrainRecord> plannedRecords) {
      foreach (var record in plannedRecords) {
        bool doesSimilarOneExist = _deferredTaskStorage.DoesSimilarOneExist(
          record.Station, record.EventType, record.Axis, record.Ndo);
        if (doesSimilarOneExist) continue;
        var deferredTask = new DeferredTask {
          EventId = record.RecId,
          TrainId = record.TrainId,
          EventType = record.EventType,
          EventStation = record.Station,
          PlannedTime = record.PlannedTime,
          EventAxis = record.Axis,
          EventNdoObject = record.Ndo,
          CreationTime = DateTime.Now,
          HasBindingCmdBeenGenerated = false
        };
        _deferredTaskStorage.AddTask(deferredTask);
        _logger.InfoFormat("A deferred task created (id: {0}; trId: {1}; type: {2}; station: {3}",
          record.RecId, record.TrainId, record.EventType, record.Station);
      }
    }

    private List<PlannedTrainRecord> FilterOutAlreadyDefinedInDeferredTasks(List<PlannedTrainRecord> records) {
      var result = new List<PlannedTrainRecord>();
      foreach (var record in records) {
        if (!_deferredTaskStorage.DoesTaskExistForEventId(record.RecId)) {
          result.Add(record);
        }
      }
      return result;
    }
  }
}
