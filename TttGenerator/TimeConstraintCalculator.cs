﻿using System;
using System.Collections.Generic;
using BCh.KTC.TttEntities;
using BCh.KTC.TttGenerator.Config;
using log4net;

namespace BCh.KTC.TttGenerator {
  public class TimeConstraintCalculator {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(TimeConstraintCalculator));

    private readonly Dictionary<string, ControlledStation> _controlledStations;
    private readonly int _reserveTime; // 1 - 2 minutes
    private readonly int _advanceCmdExePeriod;

    public TimeConstraintCalculator(Dictionary<string, ControlledStation> controlledStations,
        int reserveTime,
        int advanceCmdExePeriod) {
      _controlledStations = controlledStations;
      _reserveTime = reserveTime;
      _advanceCmdExePeriod = advanceCmdExePeriod;
    }

    public bool HaveTimeConstraintsBeenPassed(PlannedTrainRecord[] threads,
        int index, DateTime currentTime, out DateTime executionTime) {
      int delta = 0;
            if (!_controlledStations.ContainsKey(threads[index].Station))
            {
                _logger.Error($"Configuration for station {threads[index].Station} not found!");
                delta = CalculateDefaultDelta(threads, index);
            }
            else
            {
                var station = _controlledStations[threads[index].Station];
                if (threads[index].EventType != 3)
                { // arrival
                    delta = GetConfiguredTimeInterval(station, 1, threads[index])
                      + GetConfiguredTimeInterval(station, 2, threads[index])
                      + GetConfiguredTimeInterval(station, 5, threads[index]);
                }
                else
                { // departure
                    if (index != 0)
                    {
                        var diff = threads[index].ForecastTime - threads[index - 1].ForecastTime;
                        delta = (diff > new TimeSpan(0, 10, 0))
                          ? GetConfiguredTimeInterval(station, 3, threads[index]) + _reserveTime
                          : GetConfiguredTimeInterval(station, 2, threads[index]) + _reserveTime;
                    }
                    else
                    {
                        delta = (GetConfiguredTimeInterval(station, 3, threads[index]) + _reserveTime) / 3;
                    }
                }
                if (delta == 0)
                {
                    delta = CalculateDefaultDelta(threads, index);
                }
            }
      executionTime = threads[index].ForecastTime.AddMinutes(-delta);
      return executionTime.AddMinutes(-_advanceCmdExePeriod) < currentTime;
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
          default:
            _logger.Error($"Unknown time type found ({timeRecord.TimeType}) for station {station.StationCode}");
            break;
        }
      }
      _logger.Warn($"No time record is found for {station} and interval type: {intervalType}");
      return 0;
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
          var diff = thread[index].ForecastTime - thread[index - 1].ForecastTime;
          delta = (diff > new TimeSpan(0, 10, 0))
            ? 2 + _reserveTime
            : 1 + _reserveTime;
        }
      }
      return delta;
    }
  }
}
