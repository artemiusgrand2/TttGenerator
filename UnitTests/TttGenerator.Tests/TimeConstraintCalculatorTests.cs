using System;
using System.Collections.Generic;
using BCh.KTC.TttEntities;
using BCh.KTC.TttGenerator;
using BCh.KTC.TttGenerator.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TttGenerator.Tests {
  [TestClass]
  public class TimeConstraintCalculatorTests {
    private TimeConstraintCalculator _sut;

    private Dictionary<string, ControlledStation> GetContolledStations() {
      var controlledStations = new Dictionary<string, ControlledStation>();
      var controlledStation = new ControlledStation("123456");
      controlledStation.StationTimeRecords.Add(
        new StationTimeRecord {
          StationCode = "123456",
          StartObjectType = 1,
          StartObjectName = "1",
          EndObjectType = 1,
          EndObjectName = "1",
          TimeType = 1,
          TimeValue = 1
        }
      );
      controlledStations["123456"] = controlledStation;
      return controlledStations;
    }

    private PlannedTrainRecord[] GetOnePlannedTrainRecords() {
      var plannedTrainRecords = new PlannedTrainRecord[] {
        new PlannedTrainRecord { Station = "234567" },
      };
      return plannedTrainRecords;
    }


    [TestInitialize]
    public void BeforeTest() {
      _sut = new TimeConstraintCalculator(GetContolledStations(), 5, 5);
    }

    [TestMethod]
    public void StationNotFound_DefaultDeltaReturned() {
      DateTime outTime;
      //int result = _sut.HaveTimeConstraintsBeenPassed(GetOnePlannedTrainRecords(), 0, DateTime.Now, out outTime);
      //Assert.
    }
  }
}
