using System;
using System.Linq;
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

        public void ExecuteBindingCycle(DateTime now)
        {
            var notBoundHeaders = _trainHeadersRepository.RetrieveNotBoundHeaders();
            var executedAndPlanned = SplitIntoExecutedAndPlanned(notBoundHeaders);
            foreach (var executedHeader in executedAndPlanned.Item1)
            {

                ProcessExecutedHeader(executedHeader, executedAndPlanned.Item2, executedAndPlanned.Item1);
            }
        }

        private void ProcessExecutedHeader(TrainHeaderRecord executedHeader, List<TrainHeaderRecord> plannedHeaders, List<TrainHeaderRecord> executedHeaders)
        {
            int? beforeBindPlanedId = null;
            var reasonReBinding = ReasonReBinding.none;
            if (executedHeader.PlannedTrainThreadId != 0)
            {
                var findPlanHeader = plannedHeaders.Where(x => {return x.RecId == executedHeader.PlannedTrainThreadId; }).FirstOrDefault();
                if (findPlanHeader == null || (findPlanHeader != null && findPlanHeader.StateFlag == 1))
                {
                    if (findPlanHeader == null)
                        reasonReBinding = ReasonReBinding.deletePlan;
                    else
                        reasonReBinding = ReasonReBinding.flag1;
                    beforeBindPlanedId = executedHeader.PlannedTrainThreadId;
                }
                else
                    return;
                //if (plannedHeaders.Where(x => x.RecId == executedHeader.PlannedTrainThreadId && x.StateFlag == 1).FirstOrDefault() == null)
                //    return;
                //else
                //    beforeBindPlanedId = executedHeader.PlannedTrainThreadId;
            }
            //
            foreach (var plannedHeader in plannedHeaders.Where(x=>x.StateFlag == 1 || x.StateFlag == 2))
            {
                if (beforeBindPlanedId != null && beforeBindPlanedId == plannedHeader.RecId)
                    continue;
                //
                if (executedHeader.TrainNumber == plannedHeader.TrainNumber)
                {
                    bool requestedBinding = TryRequestingBinding(executedHeader, plannedHeader, beforeBindPlanedId, reasonReBinding, executedHeaders);
                    if (requestedBinding) continue;
                }
            }
        }

        private bool TryRequestingBinding(TrainHeaderRecord executedHeader, TrainHeaderRecord plannedHeader, int? beforeBindPlanedId, ReasonReBinding reasonReBinding, List<TrainHeaderRecord> executedHeaders)
        {
            var executedRecords = _passedThreadsRepository.RetrieveByHeader(executedHeader.RecId);
            var plannedRecords = _plannedThreadsRepository.RetrieveByHeader(plannedHeader.RecId);
            bool found = false;
            PassedTrainRecord passedRecordBinding = null;
            foreach (var executed in executedRecords)
            {
                if (found) break;
                foreach (var planned in plannedRecords)
                {
                    if (executed.Station == planned.Station
                        && (((executed.EventType == 1 || executed.EventType == 2) && planned.EventType == 2)
                          || executed.EventType == 3 && planned.EventType == 3)
                        && executed.Ndo == planned.Ndo
                        && IsTimeDiffWithinDelta(executed.EventTime, planned.ForecastTime))
                    {
                        found = true;
                        passedRecordBinding = executed;
                        break;
                    }
                }
            }
            if (found)
            {
                var logStr = $"Binding - planned: {plannedHeader.RecId} and passed: {executedHeader.RecId}. TrainNumber - {executedHeader.TrainNumber}.";
                if (plannedHeader.StateFlag == 2)
                {
                    found = false;
                    var currentExecutedHeader = executedHeaders.Where(x => { return x.PlannedTrainThreadId == plannedHeader.RecId; }).FirstOrDefault();
                    if (currentExecutedHeader != null)
                    {
                        var executedCurrentRecords = _passedThreadsRepository.RetrieveByHeader(currentExecutedHeader.RecId);
                        if(executedCurrentRecords.Count > 0)
                        {
                            if (executedCurrentRecords.Last().EventTime < executedRecords.First().EventTime)
                            {
                                if (_trainHeadersRepository.SetStateFlag(plannedHeader.RecId, 1))
                                {
                                    found = true;
                                    logStr = logStr + $" Before planned was binding: {currentExecutedHeader.RecId}. {GetStrReasonReBinding(ReasonReBinding.thread_break)}";
                                }
                            }
                        }
                    }
                    //
                    if (!found)
                        return found;
                }
                //
                if (beforeBindPlanedId != null)
                    logStr = logStr + $" Before passed was binding: {beforeBindPlanedId}. {GetStrReasonReBinding(reasonReBinding)}";
                _logger.Info(logStr);
                _storedProceduresExecutor.BindPlannedAndPassedTrains(plannedHeader.RecId, executedHeader.RecId, executedHeader.TrainNumber, 51);
            }
            return found;
        }

        private string GetStrReasonReBinding(ReasonReBinding reasonReBinding)
        {
            switch (reasonReBinding)
            {
                case ReasonReBinding.deletePlan:
                    return "Delete planHeader.";
                case ReasonReBinding.flag1:
                    return "In planHeader flag = 1";
                case ReasonReBinding.thread_break:
                    return "Thread break";
                default:
                    return string.Empty;
            }
        }

    private bool IsTimeDiffWithinDelta(DateTime time1, DateTime time2) {
      TimeSpan delta = (time1 > time2) ? time1 - time2 : time2 - time1;
      return delta <= _maxBindDelta;
    }


    private Tuple<List<TrainHeaderRecord>, List<TrainHeaderRecord>> SplitIntoExecutedAndPlanned(List<TrainHeaderRecord> headers) {
      var executed = new List<TrainHeaderRecord>();
      var planned = new List<TrainHeaderRecord>();
      foreach (var header in headers) {
        if (header.StateFlag == 0 /*&& header.PlannedTrainThreadId == 0*/) {
          executed.Add(header);
        } else {
          planned.Add(header);
        }
      }
      //
      return new Tuple<List<TrainHeaderRecord>, List<TrainHeaderRecord>>(executed, planned);
    }

  }
}