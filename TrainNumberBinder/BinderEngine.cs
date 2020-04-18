using System;
using System.Collections.Generic;
using BCh.KTC.TttDal.Interfaces;
using BCh.KTC.TttEntities;
using log4net;

namespace BCh.KTC.TrainNumberBinder {
  public class BinderEngine {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(BinderEngine));

    private readonly ITrainHeadersRepository _trainHeadersRepository;
    private readonly IPlannedThreadsRepository _plannedThreadsRepository;
    private readonly IPassedThreadsRepository _passedThreadsRepository;
    private readonly IStoredProcExecutor _storedProceduresExecutor;
    private readonly TimeSpan _maxBindDelta;

    public BinderEngine(ITrainHeadersRepository trainHeadersRepository,
        IPlannedThreadsRepository plannedThreadsRepository,
        IPassedThreadsRepository passedThreadsRepository,
        IStoredProcExecutor storedProceduresExecutor,
        int maxBindDelta) {
      _trainHeadersRepository = trainHeadersRepository;
      _plannedThreadsRepository = plannedThreadsRepository;
      _passedThreadsRepository = passedThreadsRepository;
      _storedProceduresExecutor = storedProceduresExecutor;
      _maxBindDelta = new TimeSpan(0, maxBindDelta, 0);
    }

    public void ExecuteBindingCycle(DateTime now) {
      var notBoundHeaders = _trainHeadersRepository.RetrieveNotBoundHeaders();
      var executedAndPlanned = SplitIntoExecutedAndPlanned(notBoundHeaders);
      foreach (var executedHeader in executedAndPlanned.Item1) {
        ProcessExecutedHeader(executedHeader, executedAndPlanned.Item2);
      }
    }

    private void ProcessExecutedHeader(TrainHeaderRecord executedHeader, List<TrainHeaderRecord> plannedHeaders) {
      foreach (var plannedHeader in plannedHeaders) {
        if (executedHeader.TrainNumber == plannedHeader.TrainNumber) {
          bool requestedBinding = TryRequestingBinding(executedHeader, plannedHeader);
          if (requestedBinding) continue;
        }
      }
    }

    private bool TryRequestingBinding(TrainHeaderRecord executedHeader, TrainHeaderRecord plannedHeader) {
      var executedRecords = _passedThreadsRepository.RetrieveByHeader(executedHeader.RecId);
      var plannedRecords = _plannedThreadsRepository.RetrieveByHeader(plannedHeader.RecId);
      bool found = false;
      foreach (var executed in executedRecords) {
        if (found) break;
        foreach (var planned in plannedRecords) {
          if (executed.Station == planned.Station
              && (((executed.EventType == 1 || executed.EventType == 2) && planned.EventType == 2)
                || executed.EventType == 3 && planned.EventType == 3)
              && executed.Ndo == planned.Ndo
              && IsTimeDiffWithinDelta(executed.EventTime, planned.ForecastTime)) {
            found = true;
            break;
          }
        }
      }
      if (found) {
        _logger.Info($"Binding - planned: {plannedHeader.RecId} and passed: {executedHeader.RecId}");
        _storedProceduresExecutor.BindPlannedAndPassedTrains(plannedHeader.RecId, executedHeader.RecId);
      }
      return found;
    }

    private bool IsTimeDiffWithinDelta(DateTime time1, DateTime time2) {
      TimeSpan delta = (time1 > time2) ? time1 - time2 : time2 - time1;
      return delta <= _maxBindDelta;
    }


    private Tuple<List<TrainHeaderRecord>, List<TrainHeaderRecord>> SplitIntoExecutedAndPlanned(List<TrainHeaderRecord> headers) {
      var executed = new List<TrainHeaderRecord>();
      var planned = new List<TrainHeaderRecord>();
      foreach (var header in headers) {
        if (header.StateFlag == 0 && header.PlannedTrainThreadId == 0) {
          executed.Add(header);
        } else {
          planned.Add(header);
        }
      }
      return new Tuple<List<TrainHeaderRecord>, List<TrainHeaderRecord>>(executed, planned);
    }

  }
}