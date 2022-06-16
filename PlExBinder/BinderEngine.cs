using BCh.KTC.PlExBinder.Config;
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
        private readonly IList<string> _trainsNumber;
        private readonly IList<string> _stationNotBinding;

        public BinderEngine(IPlannedThreadsRepository plannedThreadsRepository,
            IPassedThreadsRepository passedThreadsRepository,
            ITrainHeadersRepository trainHeadersRepository,
            IStoredProcExecutor storedProceduresExecutor,
            IDeferredTaskStorage deferredTaskStorage,
            BinderConfigDto config,
            IList<string> trainsNumber,
            IList<string> stationNotBinding
            )
        {
            _plannedThreadsRepository = plannedThreadsRepository;
            _passedThreadsRepository = passedThreadsRepository;
            _trainHeadersRepository = trainHeadersRepository;
            _storedProceduresExecutor = storedProceduresExecutor;
            _deferredTaskStorage = deferredTaskStorage;
            _config = config;
            _trainsNumber = trainsNumber;
            _stationNotBinding = stationNotBinding;
        }

        public void ExecuteBindingCycle(DateTime executionTime)
        {
            ExecuteDeferredTasks(executionTime);
            List<PlannedTrainRecord> plannedRecords = _plannedThreadsRepository.RetrievePlannedThreads(executionTime);
            //фильтр по номера поезда
            if (_trainsNumber.Count > 0)
                plannedRecords = plannedRecords.Where(x => _trainsNumber.Contains(x.TrainNumber)).ToList();
            //фильтр по станциям
            plannedRecords = plannedRecords.Where(x => !_stationNotBinding.Contains(x.StationShort)).ToList();
            plannedRecords = FilterOutAlreadyDefinedInDeferredTasks(plannedRecords, executionTime.AddMinutes(-_config.DeferredTimeLifespan));
            FormDeferredTasks(plannedRecords);
        }

        private void ExecuteDeferredTasks(DateTime executionTime)
        {
            foreach(var message in _deferredTaskStorage.CleanUpOldTask(executionTime.AddMinutes(-_config.DeferredTimeLifespan)))
                _logger.Info(message);
            //
            CleanUpBoundTasks();

            // execution itself
            var tasks = _deferredTaskStorage.GetDeferredTasks();
            foreach (var task in tasks)
            {
                List<PassedTrainRecord> candidates = _passedThreadsRepository.RetrievePassedTrainRecords(
                  task.EventStation, task.NeighbourStationCode, task.EventType == 2,
                  task.PlannedTime.AddMinutes(-_config.SearchThresholdBeforePlannedTask),
                  executionTime.AddMinutes(-_config.SearchThresholdBeforeCurrentTime));
                if (candidates.Count > 0)
                {
                    var lastCandidateId = candidates.Last().TrainId;
                    var trainNumber = _trainHeadersRepository.GetTrainNumberByTrainId(task.TrainId);
                    _storedProceduresExecutor.BindPlannedAndPassedTrains(task.TrainId, lastCandidateId, trainNumber, 50);
                    task.HasBindingCmdBeenGenerated = true;
                    _logger.Info($"Binding - planned: {task.TrainId} and passed: {lastCandidateId}. TrainNumber - {trainNumber}");
                }
            }
        }


        private void CleanUpBoundTasks()
        {
            List<DeferredTask> tasks = _deferredTaskStorage.GetDeferredTasks();
            foreach (var task in tasks)
            {
                //if (task.HasBindingCmdBeenGenerated) {
                bool isBound = _trainHeadersRepository.IsTrainThreadBound(task.TrainId);
                if (isBound)
                {
                    _deferredTaskStorage.DeleteAllTasksWithTrainId(task.TrainId);
                    _logger.InfoFormat("Cleaned up tasks for the bound thread (TrainId: {0})", task.TrainId);
                }
                //  }
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
                    NeighbourStationCode = record.NeighbourStationCode,
                    EventAxis = record.Axis,
                    EventNdoObject = record.Ndo,
                    CreationTime = DateTime.Now,
                    HasBindingCmdBeenGenerated = false
                };
                _deferredTaskStorage.AddTask(deferredTask);
                _logger.InfoFormat("A deferred task created (id: {0}; trId: {1}; type: {2}; station: {3};  trainNumber {4})",
                  record.RecId, record.TrainId, record.EventType, record.Station, record.TrainNumber);
            }
        }

        private List<PlannedTrainRecord> FilterOutAlreadyDefinedInDeferredTasks(List<PlannedTrainRecord> records, DateTime untilTime)
        {
            var result = new List<PlannedTrainRecord>();
            foreach (var record in records)
            {
                if (!_deferredTaskStorage.DoesTaskExistForEventId(record.RecId) && (record.PlannedTime >= untilTime))
                {
                    result.Add(record);
                }
            }
            return result;
        }
    }
}
