using BCh.KTC.TttDal.Interfaces;
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
        private readonly string _patternAxis = @"^([0-9]+)(.*)$";


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
            plannedThreads.ForEach(x => { ClearFalseMove(x); });
            foreach (var thread in plannedThreads)
            {
                int i = GetIndexOfLastNotConfirmedRecord(thread);
                if (i != thread.Length)
                {
                    var delElexistingTask = ReasonDeleteCommand.none;

                    ProcessThread(plannedThreads, thread, i, issuedTasks, currentTime, ref delElexistingTask);
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
            return  i;
        }

        private void ClearFalseMove(PlannedTrainRecord[] thread)
        {
            int i = thread.Length - 1;
            while (i >= 0)
            {
                if (_controlledStations.ContainsKey(thread[i].Station))
                {
                    if (thread[i].AckEventFlag != -1)
                    {
                        if (thread[i].EventType == 3 && thread[i].NeighbourStationCode != thread[i].Station)
                        {
                            var passedId = _trainHeadersRepo.GetPassedIdByPlannedId(thread[i].TrainId);
                            if (passedId != null)
                            {
                                var lastRecordsPassed = _passedRepo.GetLastTrainRecordsForStation((int)passedId);
                                if (lastRecordsPassed != null && lastRecordsPassed.Count > 0 && lastRecordsPassed[0].Station == thread[i].Station)
                                {
                                    var isFindEvent = false;
                                    foreach (var recordPassed in lastRecordsPassed)
                                    {
                                        if (((recordPassed.EventType == thread[i].EventType || (recordPassed.EventType != 3 && thread[i].EventType != 3)) && recordPassed.Ndo == thread[i].Ndo))
                                        {
                                            isFindEvent = true;
                                            break;
                                        }
                                    }
                                    //
                                    if (!isFindEvent)
                                    {
                                        _logger.Info($"Wrong move. {thread[i].ToString(_trainHeadersRepo.GetTrainNumberByTrainId(thread[i].TrainId))} !!!!");
                                        lastRecordsPassed.ForEach(recordPassed => { _logger.Info($"Last event passed {recordPassed.ToString()}."); });
                                        _plannedRepo.SetAckEventFlag(thread[i].RecId, null);
                                        thread[i].AckEventFlag = -1;
                                    }
                                }
                            }
                        }
                        break;
                    }
                }
                i--;
            }
        }


        private void ProcessThread(List<PlannedTrainRecord[]> allThreads, PlannedTrainRecord[] thread, int index, List<TtTaskRecord> tasks, DateTime currentTime, ref ReasonDeleteCommand delElexistingTask)
        {
            // -2 checking whether the station is controlled
            if (!_controlledStations.ContainsKey(thread[index].Station))
            {
                if (++index < thread.Length)
                {
                    ProcessThread(allThreads, thread, index, tasks, currentTime, ref delElexistingTask);
                }
                return;
            }
            //
            //if(thread[index].EventType == 3 && _controlledStations[thread[index].Station].ListStNotDep.Contains(thread[index].NeighbourStationCode))
            //{
            //    if (++index < thread.Length)
            //    {
            //        ProcessThread(allThreads, thread, index, tasks, currentTime, ref delElexistingTask);
            //    }
            //    return;
            //}
            // -1 checking already issued tasks
            var existingTask = FindIssuedTask(thread[index].RecId, tasks);
            DateTime executionTime, lastAckEventOrBeginning;
            TimeSpan deltaPlanExecuted;
            if (existingTask != null)
            {
                PlannedTrainRecord dependencyEventReferenceBuff = null;
                bool arrivalToCrossingBuff;
                if (delElexistingTask != ReasonDeleteCommand.none || !HaveOtherTrainDependenciesBeenPasssed(allThreads, thread, index, out dependencyEventReferenceBuff, out arrivalToCrossingBuff))
                {
                    _taskRepo.RemoveTtTask(existingTask.RecId);
                    if (delElexistingTask == ReasonDeleteCommand.none)
                        delElexistingTask = ReasonDeleteCommand.changePlan;
                    switch (delElexistingTask)
                    {
                       
                        case ReasonDeleteCommand.changePlan:
                            {
                                _logger.Info($"Task removed: {existingTask.ToString()}, because the order of passage has changed.");
                            }
                            break;
                        case ReasonDeleteCommand.breakCommand:
                            {
                                _logger.Info($"Task removed: {existingTask.ToString()}, because command break according to information from ARM.");
                            }
                            break;
                    }    
                }
                else
                {
                    if (existingTask.SentFlag == 3)
                    {
                        delElexistingTask = ReasonDeleteCommand.breakCommand;
                        _logger.Debug($"Not processing -0- command break according to information from ARM.  " + thread[index].ToString(_trainHeadersRepo.GetTrainNumberByTrainId(thread[index].TrainId)));// /*+ $"{((existingTask.SentFlag == 7) ? " command for autonom station - without doing." : string.Empty)}")*/;
                    }
                    else if (existingTask.SentFlag != 4)
                    {
                        _logger.Debug("Not processing -0- command already issued. " + thread[index].ToString(_trainHeadersRepo.GetTrainNumberByTrainId(thread[index].TrainId)));// /*+ $"{((existingTask.SentFlag == 7) ? " command for autonom station - without doing." : string.Empty)}")*/;
                        //conversion execTime
                        if (existingTask.SentFlag == 2)
                        {
                            Are4ThereAnyAckEvents(thread, out lastAckEventOrBeginning, out deltaPlanExecuted);
                            _timeConstraintCalculator.HaveTimeConstraintsBeenPassed(thread, index, currentTime, out executionTime, deltaPlanExecuted);
                            if (IsTimeDiffWithinDelta(executionTime, existingTask.ExecutionTime))
                            {
                                _taskRepo.UpdateExecTimeTask(executionTime, existingTask.RecId);
                                _logger.Debug($"Update executionTime. New time - {executionTime.ToShortDateString()} {executionTime.ToShortTimeString()} - " + thread[index].ToString());
                            }
                        }
                        //
                        return;
                    }
                }
                //
                if (++index < thread.Length)
                {
                    ProcessThread(allThreads, thread, index, tasks, currentTime, ref delElexistingTask);
                }
                return;
            }
            //
            if (delElexistingTask != ReasonDeleteCommand.none)
                return;
            // 0 (4)- is the thread identified (bound; are there any ack events)
 
            bool are4ThereAnyAckEvents = Are4ThereAnyAckEvents(thread, out lastAckEventOrBeginning, out deltaPlanExecuted).Item1;

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
              //  bool isSetNullAckEventFlag;
                has12PrevBeenEventExecuted = HaveSelfDependenciesBeenPassed(thread, index, deltaPlanExecuted/*, out isSetNullAckEventFlag*/);
                //if (isSetNullAckEventFlag)
                //{
                //    //if (--index >= 0)
                //    //{
                //    //    ProcessThread(allThreads, thread, index, tasks, currentTime/*, ref delElexistingTask*/);
                //    //}
                //    return;
                //}
            }
            // 2 time constraints
            bool has2TimeConstraintsPassed = _timeConstraintCalculator.HaveTimeConstraintsBeenPassed(thread, index, currentTime, out executionTime, deltaPlanExecuted);

            if (lastAckEventOrBeginning + _prevAckPeriod < executionTime || !has2TimeConstraintsPassed)
            {
                if (has2TimeConstraintsPassed)
                {
                    _logger.Info($"Task - {thread[index].ToString(_trainHeadersRepo.GetTrainNumberByTrainId(thread[index].TrainId))} not write, does not fall into the capture area. {lastAckEventOrBeginning} + {_prevAckPeriod.Hours}:{_prevAckPeriod.Minutes} < {executionTime.ToLongTimeString()}");
                }
                //
                return;
            }
            // 3 other train dependencies constraints
            PlannedTrainRecord dependencyEventReference = null;
            bool arrivalToCrossing;
            bool has3OtherTrainDependenciesPassed = HaveOtherTrainDependenciesBeenPasssed(allThreads, thread, index, out dependencyEventReference, out arrivalToCrossing);
            if (!has3OtherTrainDependenciesPassed)
                return;
            // 6 is task generation allowed for this station / event (arr, dep)
            bool is6TaskGenAllowedForStationEvent = IsTaskGenAllowedForStationEvent(thread, index);

            if ((has11PrevTaskBeenExecuted || has12PrevBeenEventExecuted)
                && has2TimeConstraintsPassed
                && has3OtherTrainDependenciesPassed
                && are4ThereAnyAckEvents
                || (has2TimeConstraintsPassed
                    && has3OtherTrainDependenciesPassed
                    && is6TaskGenAllowedForStationEvent && !_commandRepo.IsCommandBindPlanToTrain(thread[index].TrainId) /* && CheckPlannedTimeWithCurTime(thread[index], dependencyEventReference)*/))
            {
                TtTaskRecord task = CreateTask(thread[index], (index > 0) ? thread[index - 1] : null, dependencyEventReference, executionTime);
                //
                //if (IsRepeatCompletedTask(task, tasks))
                //    task.SentFlag = 4;
                var isAutonomous = IsAutonomousForStationEvent(thread, index);
                if (arrivalToCrossing || isAutonomous ||
                    (thread[index].EventType == 3 && _controlledStations[thread[index].Station].ListStNotDep.Contains(thread[index].NeighbourStationCode)))
                    task.SentFlag = 4;
                else
                {
                    //only ron
                    if (IsOnlyRonForStationEvent(thread[index]))
                        task.SentFlag = (thread[index].EventType != 3) ? 4 : 7;
                    else if (thread[index].EventType == 3 && index == 0)
                        task.SentFlag = 8;
                }
                //
                _logger.Info($"Task created for tr:'{task.TrainNumber}': {task.PlannedEventReference} - {task.Station}, {task.RouteStartObjectType}:{task.RouteStartObjectName}, {task.RouteEndObjectType}:{task.RouteEndObjectName}" + $"{((isAutonomous) ? " command for autonom station - without doing." : string.Empty)}");
                _taskRepo.InsertTtTask(task);
                _logger.Info("The task has been written to the database.");
            }
        }

        //private bool CheckPlannedTimeWithCurTime(PlannedTrainRecord plannedRecord, PlannedTrainRecord dependencyEvent)
        //{
        //    var addTime = new TimeSpan();
        //    if(dependencyEvent != null)
        //    {
        //        var resDelta = CheckDeltaPlanExecuted(dependencyEvent);
        //        if (resDelta.Item1 && resDelta.Item2.TotalSeconds > 0)
        //            addTime = resDelta.Item2;
        //    }
        //    var plannedTimePlus = plannedRecord.PlannedTime.Add(addTime);
        //    var result = plannedTimePlus > DateTime.Now;
        //    if(!result)
        //        _logger.Info($"Task -  {plannedRecord.ToString(_trainHeadersRepo.GetTrainNumberByTrainId(plannedRecord.TrainId))}+add time ({addTime.Hours}:{addTime.Minutes}:{addTime.Seconds})={plannedTimePlus.ToLongTimeString()}  not write, because rope not tied and planned time < currentTime.");
        //    return result;
        //}

        private Tuple<bool, TimeSpan> CheckDeltaPlanExecuted(PlannedTrainRecord record)
        {
            if (_controlledStations.ContainsKey(record.Station))
            {
                if (!_controlledStations[record.Station].IsComparePlanWithPassed)
                {
                    if (!(record.EventType == 3 && record.NeighbourStationCode != record.Station))
                        return new Tuple<bool, TimeSpan>(false, new TimeSpan());
                }
                //
                 return new Tuple<bool, TimeSpan>(true, (record.ForecastTime - record.PlannedTime));
            }
            //
            return new Tuple<bool, TimeSpan>(false, new TimeSpan());
        }

        private Tuple<bool, bool> Are4ThereAnyAckEvents(PlannedTrainRecord[] thread, out DateTime lastAckEventOrBeginning, out TimeSpan deltaPlanExecuted)
        {
            lastAckEventOrBeginning = new DateTime();
            deltaPlanExecuted = new TimeSpan();
            bool result = false, isLastEventAck = false;
            int numberFirstControlledStations = -1;
            for (int i = thread.Length - 1; i >= 0; --i)
            {
                if (!result)
                    lastAckEventOrBeginning = thread[i].ForecastTime;
                //
                if (numberFirstControlledStations == -1 && _controlledStations.ContainsKey(thread[i].Station))
                    numberFirstControlledStations = i;
                //
                if (thread[i].AckEventFlag == 2)
                {
                    if (!result)
                    {
                        result = true;
                        isLastEventAck = (i == thread.Length - 1) || (i == numberFirstControlledStations);
                    }
                    //
                    var resDelta = CheckDeltaPlanExecuted(thread[i]);
                    if(resDelta.Item1)
                    {
                        deltaPlanExecuted = resDelta.Item2;
                        break;
                    }
                }
            }
            //
            return new Tuple<bool, bool>(result, isLastEventAck);
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

        private bool IsOnlyRonForStationEvent(PlannedTrainRecord plannedRecord)
        {
            if (!_controlledStations.ContainsKey(plannedRecord.Station))
                return false;
            //
            var onlyRonAll = _controlledStations[plannedRecord.Station].OnlyRon;
            if (onlyRonAll)
                return true;
            else
            {
                if (plannedRecord.EventType == 3 && _controlledStations[plannedRecord.Station].OnlyRonStations.Contains(plannedRecord.NeighbourStationCode))
                    return true;
            }
            //
            return false;
        }

        //private bool IsEventWithinPrevAckPeriodFromBeninning(PlannedTrainRecord[] threads, int index)
        //{
        //    return threads[index].ForecastTime - threads[0].ForecastTime < _prevAckPeriod;
        //}

        private TtTaskRecord CreateTask(PlannedTrainRecord plannedTrainRecord, PlannedTrainRecord prevPlannedTrainRecord,
            PlannedTrainRecord dependecyEventReference,
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
                    plannedTrainRecord.Station == prevPlannedTrainRecord.Station /*&& plannedTrainRecord.Axis != prevPlannedTrainRecord.Axis*/ && !EqualsAxis(plannedTrainRecord.Axis, prevPlannedTrainRecord.Axis, plannedTrainRecord.Station))
                {
                    if (!IsOnlyRonForStationEvent(plannedTrainRecord))
                    {
                        task.RouteStartObjectName = prevPlannedTrainRecord.Axis;
                        _logger.Info($"Task -  {plannedTrainRecord.ToString(_trainHeadersRepo.GetTrainNumberByTrainId(plannedTrainRecord.TrainId))} replace path departure with {plannedTrainRecord.Axis} on {prevPlannedTrainRecord.Axis}.");
                         task.SentFlag = 6;
                    }
                }
            }
            //
            task.CreationTime = DateTime.Now;
            task.ExecutionTime = executionTime;
            task.PlannedEventReference = plannedTrainRecord.RecId;
            task.DependencyEventReference = dependecyEventReference != null ? dependecyEventReference.RecId : -1;
            task.TrainNumber = string.IsNullOrEmpty(plannedTrainRecord.TrainNumber)?_trainHeadersRepo.GetTrainNumberByTrainId(plannedTrainRecord.TrainId): plannedTrainRecord.TrainNumber;
            return task;
        }

        private bool EqualsAxis(string axis1, string axis2, string station)
        {
            if (axis1 != axis2)
            {
                var match1 = Regex.Match(axis1, _patternAxis);
                if (match1.Success)
                {
                    var match2 = Regex.Match(axis2, _patternAxis);
                    if (match2.Success)
                    {
                        if (match1.Groups.Count > 1 && match2.Groups.Count > 1)
                            return (match1.Groups[1].Value == match2.Groups[1].Value && !_controlledStations[station].ListAxisEqualsForNumberAndDifDir.Contains(axis1) && !_controlledStations[station].ListAxisEqualsForNumberAndDifDir.Contains(axis2));
                    }
                }
            }
            //
            return (axis1 == axis2);
        }

        private ViewParity GetViewParityObject(string name1, bool isPath, string name2 = null)
        {
            if (isPath)
            {
                var match1 = Regex.Match(name1, _patternAxis);
                if (match1.Success)
                    return (int.Parse(match1.Groups[1].Value) % 2 == 0) ? ViewParity.even : ViewParity.odd;
            }
            else
            {
                if (name1.ToUpper().IndexOf("Ч") != -1)
                {
                    if (!string.IsNullOrEmpty(name2) && name2.ToUpper().IndexOf("Н") == -1 && Regex.IsMatch(name1.ToUpper(), @"Ч[А-Я]"))
                        return ViewParity.odd;
                    else
                        return ViewParity.even;
                }

                //
                if (name1.ToUpper().IndexOf("Н") != -1)
                {
                    if (!string.IsNullOrEmpty(name2) && name2.ToUpper().IndexOf("Ч") == -1 && Regex.IsMatch(name1.ToUpper(), @"Н[А-Я]"))
                        return ViewParity.even;
                    else
                        return ViewParity.odd;
                }
                //

            }
            //
            return ViewParity.none;
        }

        private bool HaveOtherTrainDependenciesBeenPasssed(List<PlannedTrainRecord[]> allThreads,
            PlannedTrainRecord[] thread, int index, out PlannedTrainRecord dependencyEventReference, out bool arrivalToCrossing)
        {
            dependencyEventReference = null;
            PlannedTrainRecord previousEvent = null;
            PlannedTrainRecord[] previousRope = null;
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
                            && ((aThread[i].EventType == 2 && aThread[i].Ndo == thread[index].Ndo)
                            || IsСrossingTwoPaths(thread[index], aThread[i])
                              ||
                             (aThread[i].EventType == 3 && aThread[i].Ndo == thread[index].Ndo && ((index == 0) || (index > 0 && (!_controlledStations.ContainsKey(thread[index-1].Station)))))
                             /* || (aThread[i].EventType == 3 && aThread[i].Axis == thread[index].Axis)*/))
                        {
                            eventFound = true;
                        }
                    }
                    else
                    { // Depature
                        if (aThread[i].Station == thread[index].Station
                            && (aThread[i].Ndo == thread[index].Ndo ||
                            (!isCrossing && IsСrossingTwoPaths(thread[index], aThread[i])) ||
                            (isCrossing && i > 0 && aThread[i - 1].EventType == 2 && aThread[i - 1].Station == thread[index].Station && aThread[i - 1].Ndo == thread[index].Ndo)))
                        {
                            eventFound = true;
                        }
                    }
                    if (eventFound)
                    {
                        if (previousEvent == null)
                        {
                            previousEvent = aThread[i];
                            previousRope = aThread;
                        }
                        else if (aThread[i].PlannedTime > previousEvent.PlannedTime)
                        {
                            previousEvent = aThread[i];
                            previousRope = aThread;
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
                dependencyEventReference = previousEvent;
                return true;
            }
            else if (previousEvent.AckEventFlag == -1)
            {
                if (IsOldDependenciesRope(previousRope))
                {
                    dependencyEventReference = null;
                    if (_trainHeadersRepo.DeletePlanRope(previousEvent.TrainId))
                        _logger.Info($"Task -  {thread[index].ToString(_trainHeadersRepo.GetTrainNumberByTrainId(thread[index].TrainId))} delete old rope {previousEvent.TrainId} tr:'{_trainHeadersRepo.GetTrainNumberByTrainId(previousEvent.TrainId)}'");
                    return true;
                }
                else
                    _logger.Info($"Task -  {thread[index].ToString(_trainHeadersRepo.GetTrainNumberByTrainId(thread[index].TrainId))} not write, because not done event - {previousEvent.ToString(_trainHeadersRepo.GetTrainNumberByTrainId(previousEvent.TrainId))}");
            }
            //
            return false;
        }

        private bool IsOldDependenciesRope(PlannedTrainRecord[] rope)
        {
            DateTime lastAckEventOrBeginning;
            TimeSpan deltaPlanExecuted;
            //
            var res = Are4ThereAnyAckEvents(rope, out lastAckEventOrBeginning, out deltaPlanExecuted);
            return  res.Item1 ? (res.Item2 && DateTime.Now >= rope[rope.Length - 1].GetForecastTime2(deltaPlanExecuted)) : DateTime.Now >= rope[rope.Length - 1].PlannedTime;
        }

        private bool IsСrossingTwoPaths(PlannedTrainRecord сurEvent, PlannedTrainRecord checkEvent)
        {
            if(сurEvent.Ndo != checkEvent.Ndo && сurEvent.NeighbourStationCode == checkEvent.NeighbourStationCode)
            {
                if (GetViewParityObject(сurEvent.Axis, true) != GetViewParityObject(сurEvent.Ndo, false, checkEvent.Ndo) || GetViewParityObject(checkEvent.Axis, true) != GetViewParityObject(checkEvent.Ndo, false, сurEvent.Ndo))
                {
                    return !_controlledStations[сurEvent.Station].ListAxisEqualsForNumberAndDifDir.Contains(сurEvent.Axis) && !_controlledStations[checkEvent.Station].ListAxisEqualsForNumberAndDifDir.Contains(checkEvent.Axis);
                }
            }
            //
            return false;
        }

        private bool HaveSelfDependenciesBeenPassed(PlannedTrainRecord[] thread, int index, TimeSpan deltaPlanExecuted)
        {
            int i = index;
            while (--i >= 0)
            {
                if (thread[index].GetForecastTime2(deltaPlanExecuted) - thread[i].GetForecastTime2(deltaPlanExecuted) < _prevAckPeriod)
                {
                    if (thread[i].AckEventFlag != -1 || i == 0)
                    {
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
