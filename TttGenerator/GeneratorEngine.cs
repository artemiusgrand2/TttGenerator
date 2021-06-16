﻿using BCh.KTC.TttDal.Interfaces;
using BCh.KTC.TttEntities;
using BCh.KTC.TttGenerator.Config;
using BCh.KTC.TttGenerator.Services;
using log4net;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BCh.KTC.TttGenerator
{
    public class GeneratorEngine
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GeneratorEngine));

        private readonly TimeConstraintCalculator _timeConstraintCalculator;
        private readonly Dictionary<string, ControlledStation> _controlledStations;
        private readonly IPlannedThreadsRepository _plannedRepo;
        private readonly IPassedThreadsRepository _passedRepo;
        private readonly ITtTaskRepository _taskRepo;
        private readonly ITrainHeadersRepository _trainHeadersRepo;
        private readonly ICommandThreadsRepository _commandRepo;
        private readonly TimeSpan _prevAckPeriod; // 15 - 20 minutes
        private readonly TimeSpan _periodConversionExecTime; //in minutes


        public GeneratorEngine(TimeConstraintCalculator timeConstraintCalculator,
            Dictionary<string, ControlledStation> controlledStations,
            IPlannedThreadsRepository plannedRepo,
            ITtTaskRepository taskRepo,
            ITrainHeadersRepository trainHeadersRepo,
            ICommandThreadsRepository commandRepo, IPassedThreadsRepository passedRepo,
            int prevAckPeriod, int periodConversionExecTime)
        {
            _timeConstraintCalculator = timeConstraintCalculator;
            _controlledStations = controlledStations;
            _plannedRepo = plannedRepo;
            _taskRepo = taskRepo;
            _trainHeadersRepo = trainHeadersRepo;
            _commandRepo = commandRepo;
            _passedRepo = passedRepo;
            _prevAckPeriod = new TimeSpan(0, prevAckPeriod, 0);
            _periodConversionExecTime = new TimeSpan(0, periodConversionExecTime, 0);
        }

        private bool IsTimeDiffWithinDelta(DateTime time1, DateTime time2)
        {
            TimeSpan delta = (time1 > time2) ? time1 - time2 : time2 - time1;
            return delta > _periodConversionExecTime;
        }

        public void PerformWorkingCycle(DateTime currentTime)
        {
            var plannedRecords = _plannedRepo.RetrieveThreadsForTttGenerator(currentTime);
            var plannedThreads = PlannedThreadsProcessor.GetPlannedThreads(plannedRecords);
            var issuedTasks = _taskRepo.GetTtTasks();
            foreach (var thread in plannedThreads)
            {
                int i = GetIndexOfLastNotConfirmedRecord(thread);
                if (i != thread.Length)
                {
                   // var delElexistingTask = false;
                    ProcessThread(plannedThreads, thread, i, issuedTasks, currentTime/*, ref delElexistingTask*/);
                }
            }
        }


        private int GetIndexOfLastNotConfirmedRecord(PlannedTrainRecord[] thread)
        {
            int i = thread.Length;
            while (i > 0)
            {
                if (thread[i - 1].AckEventFlag == -1)
                {
                    --i;
                    if (i == 0) break;
                }
                else
                {
                    break;
                }
            }
            return i;
        }


        private void ProcessThread(List<PlannedTrainRecord[]> allThreads, PlannedTrainRecord[] thread, int index, List<TtTaskRecord> tasks, DateTime currentTime/*,ref bool delElexistingTask*/)
        {
            // -2 checking whether the station is controlled
            if (!_controlledStations.ContainsKey(thread[index].Station))
            {
                if (++index < thread.Length)
                {
                    ProcessThread(allThreads, thread, index, tasks, currentTime/*, ref delElexistingTask*/);
                }
                return;
            }
            //
            if(thread[index].EventType == 3 && _controlledStations[thread[index].Station].ListStNotDep.Contains(thread[index].NeighbourStationCode))
            {
                if (++index < thread.Length)
                {
                    ProcessThread(allThreads, thread, index, tasks, currentTime/*, ref delElexistingTask*/);
                }
                return;
            }
            // -1 checking already issued tasks
            var existingTask = FindIssuedTask(thread[index].RecId, tasks);
            DateTime executionTime;
            if (existingTask != null)
            {
                //int dependencyEventReferenceBuff = -1;
                //bool arrivalToCrossingBuff;
                //if (delElexistingTask || !HaveOtherTrainDependenciesBeenPasssed(allThreads, thread, index, out dependencyEventReferenceBuff, out arrivalToCrossingBuff))
                //{
                //    _taskRepo.RemoveTtTask(existingTask.RecId);
                //    _logger.Info($"Task removed: : {existingTask.PlannedEventReference} - {existingTask.Station}, {existingTask.RouteStartObjectType}:{existingTask.RouteStartObjectName}, " +
                //        $"{existingTask.RouteEndObjectType}:{existingTask.RouteEndObjectName}, " +
                //        $" because not done event with id - {dependencyEventReferenceBuff}.");
                //    delElexistingTask = true;
                //}
                //else
                //{
                    if (existingTask.SentFlag != 4)
                    {
                    _logger.Debug("Not processing -0- command already issued. " + thread[index].ToString());// /*+ $"{((existingTask.SentFlag == 7) ? " command for autonom station - without doing." : string.Empty)}")*/;
                        //conversion execTime
                        //if(existingTask.SentFlag != 5 && existingTask.SentFlag != 6)
                        //{
                        //    _timeConstraintCalculator.HaveTimeConstraintsBeenPassed(thread, index, currentTime, out executionTime);
                        //    if (IsTimeDiffWithinDelta(executionTime, existingTask.ExecutionTime))
                        //    {
                        //        _taskRepo.UpdateExecTimeTask(executionTime, existingTask.RecId);
                        //        _logger.Debug($"Update executionTime. New time - {executionTime.ToShortDateString()} {executionTime.ToShortTimeString()} - " + thread[index].ToString());
                        //    }
                        //}
                        //
                        return;
                    }
             //   }
                //
                if (++index < thread.Length)
                {
                    ProcessThread(allThreads, thread, index, tasks, currentTime/*, ref delElexistingTask*/);
                }
                return;
            }
            // 0 (4)- is the thread identified (bound; are there any ack events)
            DateTime lastAckEventOrBeginning;
            TimeSpan deltaPlanExecuted;
            bool are4ThereAnyAckEvents = Are4ThereAnyAckEvents(thread, out lastAckEventOrBeginning, out deltaPlanExecuted);

            // 1.1 has prev task been executed?
            bool has11PrevTaskBeenExecuted = false;
            if (index > 0)
            {
                var prevTask = FindIssuedTask(thread[index - 1].RecId, tasks);
                if (prevTask != null && prevTask.SentFlag == 4)
                {
                    has11PrevTaskBeenExecuted = true;
                }
            }
            // 1.2 self dependency constraints - has prev event been executed?
            bool has12PrevBeenEventExecuted = false;
            if (!has11PrevTaskBeenExecuted)
            {
                bool isSetNullAckEventFlag;
                has12PrevBeenEventExecuted = HaveSelfDependenciesBeenPassed(thread, index, deltaPlanExecuted, out isSetNullAckEventFlag);
                if (isSetNullAckEventFlag)
                {
                    //if (--index >= 0)
                    //{
                    //    ProcessThread(allThreads, thread, index, tasks, currentTime/*, ref delElexistingTask*/);
                    //}
                    return;
                }
            }

            // 2 time constraints
            bool has2TimeConstraintsPassed = _timeConstraintCalculator.HaveTimeConstraintsBeenPassed(thread, index, currentTime, out executionTime, deltaPlanExecuted);

            if (lastAckEventOrBeginning + _prevAckPeriod < executionTime)
            {
                return;
            }
            // 3 other train dependencies constraints
            int dependencyEventReference;
            bool arrivalToCrossing;
            bool has3OtherTrainDependenciesPassed = HaveOtherTrainDependenciesBeenPasssed(allThreads, thread, index, out dependencyEventReference, out arrivalToCrossing);

            // 6 is task generation allowed for this station / event (arr, dep)
            bool is6TaskGenAllowedForStationEvent = IsTaskGenAllowedForStationEvent(thread, index);

            if ((has11PrevTaskBeenExecuted || has12PrevBeenEventExecuted)
                && has2TimeConstraintsPassed
                && has3OtherTrainDependenciesPassed
                && are4ThereAnyAckEvents
                || (has2TimeConstraintsPassed
                    && has3OtherTrainDependenciesPassed
                    && is6TaskGenAllowedForStationEvent && !_commandRepo.IsCommandBindPlanToTrain(thread[index].TrainId)))
            {
                TtTaskRecord task = CreateTask(thread[index], (index > 0) ? thread[index - 1] : null, dependencyEventReference, executionTime);
                //
                //if (IsRepeatCompletedTask(task, tasks))
                //    task.SentFlag = 4;
                if (arrivalToCrossing)
                    task.SentFlag = 4;
                //autonom station
                var isAutonomous = IsAutonomousForStationEvent(thread, index);
                if (isAutonomous)
                    task.SentFlag = 4;
                _logger.Info($"Task created: {task.PlannedEventReference} - {task.Station}, {task.RouteStartObjectType}:{task.RouteStartObjectName}, {task.RouteEndObjectType}:{task.RouteEndObjectName}" + $"{((isAutonomous) ? " command for autonom station - without doing." : string.Empty)}");
                _taskRepo.InsertTtTask(task);
                _logger.Info("The task has been written to the database.");
            }
        }

        private bool Are4ThereAnyAckEvents(PlannedTrainRecord[] thread, out DateTime lastAckEventOrBeginning, out TimeSpan deltaPlanExecuted)
        {
            lastAckEventOrBeginning = new DateTime();
            deltaPlanExecuted = new TimeSpan();
            for (int i = thread.Length - 1; i >= 0; --i)
            {
                lastAckEventOrBeginning = thread[i].ForecastTime;
                if (thread[i].AckEventFlag == 2)
                {
                    deltaPlanExecuted = (thread[i].ForecastTime - thread[i].PlannedTime);
                    return true;
                }
            }
            return false;
        }

        private bool IsTaskGenAllowedForStationEvent(PlannedTrainRecord[] threads, int index)
        {
            if (!_controlledStations.ContainsKey(threads[index].Station))
            {
                return false;
            }
            // 3 - depart
            if (threads[index].EventType == 3)
            {
                return _controlledStations[threads[index].Station].AllowGeneratingNotCfmDeparture;
            }
            return _controlledStations[threads[index].Station].AllowGeneratingNotCfmArrival;
        }

        private bool IsAutonomousForStationEvent(PlannedTrainRecord[] threads, int index)
        {
            if (!_controlledStations.ContainsKey(threads[index].Station))
            {
                return false;
            }
            //
            return _controlledStations[threads[index].Station].Autonomous;
        }

        //private bool IsEventWithinPrevAckPeriodFromBeninning(PlannedTrainRecord[] threads, int index)
        //{
        //    return threads[index].ForecastTime - threads[0].ForecastTime < _prevAckPeriod;
        //}

        private TtTaskRecord CreateTask(PlannedTrainRecord plannedTrainRecord, PlannedTrainRecord prevPlannedTrainRecord,
            int dependecyEventReference,
            DateTime executionTime)
        {
            var task = new TtTaskRecord();
            task.Station = plannedTrainRecord.Station;
            // ?!!! there might be situations of moving from a track to another track
            if (plannedTrainRecord.EventType != 3)
            { // Arrival
                task.RouteStartObjectType = 5;
                task.RouteStartObjectName = plannedTrainRecord.Ndo;
                task.RouteEndObjectType = 3;
                task.RouteEndObjectName = plannedTrainRecord.Axis;
            }
            else
            { // Departure
                task.RouteStartObjectType = 3;
                task.RouteStartObjectName = plannedTrainRecord.Axis;
                task.RouteEndObjectType = 5;
                task.RouteEndObjectName = plannedTrainRecord.Ndo;
                //
                if (prevPlannedTrainRecord != null &&
                    plannedTrainRecord.Station == prevPlannedTrainRecord.Station && !EqualsAxis(plannedTrainRecord.Axis, prevPlannedTrainRecord.Axis))
                    task.SentFlag = 6;
            }
            //
            task.CreationTime = DateTime.Now;
            task.ExecutionTime = executionTime;
            task.PlannedEventReference = plannedTrainRecord.RecId;
            task.DependencyEventReference = dependecyEventReference;
            task.TrainNumber = _trainHeadersRepo.GetTrainNumberByTrainId(plannedTrainRecord.TrainId);
            return task;
        }

        private bool EqualsAxis(string axis1, string axis2)
        {
            var patternAxis = @"^([0-9]+)(.*)$";
            var match1 = Regex.Match(axis1, patternAxis);
            if (match1.Success)
            {
                var match2 = Regex.Match(axis2, patternAxis);
                if (match2.Success)
                {
                    if (match1.Groups.Count > 1 && match2.Groups.Count > 1)
                        return (match1.Groups[1].Value == match2.Groups[1].Value);
                }
            }
            //
            return (axis1 == axis2);
        }

        private bool HaveOtherTrainDependenciesBeenPasssed(List<PlannedTrainRecord[]> allThreads,
            PlannedTrainRecord[] thread, int index, out int dependencyEventReference, out bool arrivalToCrossing)
        {
            dependencyEventReference = -1;
            PlannedTrainRecord previousEvent = null;
            var isCrossing = (_controlledStations.ContainsKey(thread[index].Station)) ? _controlledStations[thread[index].Station].IsCrossing : false;
            arrivalToCrossing = (isCrossing && thread[index].EventType != 3) ? true : false;
            if (arrivalToCrossing)
            {
                //если происходит прибытие на разъезд
                return true;
            }
            //
            foreach (PlannedTrainRecord[] aThread in allThreads)
            {
                if (aThread == thread) continue;
                for (int i = aThread.Length - 1; i >= 0; --i)
                {
                    if (aThread[i].PlannedTime > thread[index].PlannedTime) continue;

                    bool eventFound = false;
                    if (thread[index].EventType != 3)
                    { // Arrival
                        if (aThread[i].Station == thread[index].Station
                            && ((aThread[i].EventType == 2 && aThread[i].Ndo == thread[index].Ndo)/* ||
                            (aThread[i].EventType == 3 && aThread[i].Ndo == thread[index].Ndo && ((index == 0) || (index > 0 && (!_controlledStations.ContainsKey(thread[index-1].Station)))))*/
                             /* || (aThread[i].EventType == 3 && aThread[i].Axis == thread[index].Axis)*/))
                        {
                            eventFound = true;
                        }
                    }
                    else
                    { // Depature
                        if (aThread[i].Station == thread[index].Station
                            && (aThread[i].Ndo == thread[index].Ndo ||(isCrossing && i > 0 && aThread[i - 1].EventType == 2 && aThread[i - 1].Station == thread[index].Station && aThread[i - 1].Ndo == thread[index].Ndo)))
                        {
                            eventFound = true;
                        }
                    }
                    if (eventFound)
                    {
                        if (previousEvent == null)
                        {
                            previousEvent = aThread[i];
                        }
                        else if (aThread[i].PlannedTime > previousEvent.PlannedTime)
                        {
                            previousEvent = aThread[i];
                        }
                        break;
                    }
                }
            }

            if (previousEvent == null)
            {
                return true;
            }
            else if (previousEvent.AckEventFlag != -1)
            {
                dependencyEventReference = previousEvent.RecId;
                return true;
            }
            else if (previousEvent.AckEventFlag == -1)
            {
                _logger.Info($"Task -  {thread[index].ToString()} not write, because not done event - {previousEvent.ToString()}");
            }
            return false;
        }


        private bool HaveSelfDependenciesBeenPassed(PlannedTrainRecord[] thread, int index, TimeSpan deltaPlanExecuted, out bool isSetNullAckEventFlag)
        {
            isSetNullAckEventFlag = false;
            int i = index;
            while (--i >= 0)
            {
                if (thread[index].GetForecastTime2(deltaPlanExecuted) - thread[i].GetForecastTime2(deltaPlanExecuted) < _prevAckPeriod)
                {
                    if (thread[i].AckEventFlag != -1 || i == 0)
                    {
                        if (thread[i].AckEventFlag != -1)
                        {
                            var passedId = _trainHeadersRepo.GetPassedIdByPlannedId(thread[i].TrainId);
                            if(passedId != null)
                            {
                                var lastRecordPassed= _passedRepo.GetLastTrainRecord((int)passedId);
                                if(lastRecordPassed != null)
                                {
                                    if (lastRecordPassed.Station == thread[i].Station && (lastRecordPassed.EventType == thread[i].EventType || (lastRecordPassed.EventType != 3 && thread[i].EventType != 3)))
                                    {
                                        if (lastRecordPassed.Ndo != thread[i].Ndo)
                                        {
                                            _logger.Info($"Task -  {thread[index].ToString()} false move event. In passed event - {lastRecordPassed.ToString()}, in planned event -  {thread[i].ToString()} !!!!");
                                            isSetNullAckEventFlag = true;
                                            _plannedRepo.SetAckEventFlag(thread[i].RecId, null);
                                        }
                                    }
                                }
                            }
                        }
                        //
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
            return false;
        }

        //private bool HaveSelfDependenciesBeenPassed(PlannedTrainRecord[] thread, int index, DateTime currentTime) {
        //  int i = index;
        //  while (--i >= 0) {
        //    if (thread[index].ForecastTime - thread[i].ForecastTime < _prevAckPeriod) {
        //      if (thread[i].AckEventFlag != -1) {
        //        return true;
        //      }
        //    } else {
        //      if (index - i == 1
        //          && thread[i].AckEventFlag != -1
        //          && thread[index].ForecastTime - currentTime < _prevAckPeriod) {
        //        return true;
        //      }
        //      return false;
        //    }
        //  }
        //  return true;
        //}

        private TtTaskRecord FindIssuedTask(int recId, List<TtTaskRecord> tasks)
        {
            foreach (var task in tasks)
            {
                if (recId == task.PlannedEventReference) return task;
            }
            return null;
        }

        private bool EquallyPlanAndExucTaskEvent(TtTaskRecord task, PlannedTrainRecord plan)
        {
            return (plan.Station == task.Station && ((plan.EventType == 3 && plan.Axis == task.RouteStartObjectName && plan.Ndo == task.RouteEndObjectName) ||
                (plan.EventType == 2 && plan.Axis == task.RouteEndObjectName && plan.Ndo == task.RouteStartObjectName)));
        }

        private bool IsRepeatCompletedTask(TtTaskRecord task, List<TtTaskRecord> tasks)
        {
            return tasks.Where(x => x.TrainNumber == task.TrainNumber && x.Station == task.Station && x.SentFlag == 4 && (task.CreationTime - x.CreationTime).TotalHours <= 1
                                     && x.RouteStartObjectType == task.RouteStartObjectType && x.RouteStartObjectName == task.RouteStartObjectName &&
                                     x.RouteEndObjectType == task.RouteEndObjectType && x.RouteEndObjectName == task.RouteEndObjectName).FirstOrDefault() != null;
        }
    }
}
