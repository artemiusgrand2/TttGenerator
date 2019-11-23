using KTC.BCh.TttDal;
using KTC.BCh.TttEntities;
using KTC.BCh.TttGeneratorEngine.Config;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace KTC.BCh.TttGeneratorEngine {
  public class Engine {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(Engine));

    private string _gidDbConString;
    private PlannedTrainRepository _plannedTrainRepository;
    private Timer _timer;


    public Engine() {
      Initialize();
    }


    private void Initialize() {
      _logger.Info("Starting TttGenerator...");
      _gidDbConString = EngineConfig.GetGidDbConnectionString();
      _logger.Info($"Configured GidDb conString: {_gidDbConString}");
      _plannedTrainRepository = new PlannedTrainRepository(_gidDbConString);
      int cycleTime = EngineConfig.GetCycleTime();
      if (cycleTime < 10) {
        _logger.WarnFormat("CycleTime set in configuration is: {0}. CycleTime re-set is: 10", cycleTime);
      } else {
        _logger.InfoFormat("CycleTime set is: {0}", cycleTime);
      }
      _timer = new Timer(cycleTime * 1000);
      _timer.Elapsed += TimerElapsed;
      _timer.Enabled = true;
    }


    private void TimerElapsed(object sender, ElapsedEventArgs e) {
      _timer.Enabled = false;
      try {
        PerformMainCycle();
      } catch (Exception ex) {
        _logger.Error("An exception occurred while performing the main cycle", ex);
      }
      _timer.Enabled = true;
    }


    private void PerformMainCycle() {

    }


    private void IdenfityDependencies() {
      var plannedTrainRecords = _plannedTrainRepository.RetrieveRecords();
      List<List<PlannedTrainRecord>> trainThreads = SplitPlannedTrainRecordsIntoThreads(plannedTrainRecords);

    }


    private List<List<PlannedTrainRecord>> SplitPlannedTrainRecordsIntoThreads(List<PlannedTrainRecord> plannedTrainRecords) {
      var trainThreads = new List<List<PlannedTrainRecord>>();
      int prevTrainRef = -1;
      List<PlannedTrainRecord> currentTrainThread = null;
      foreach (var record in plannedTrainRecords) {
        if (prevTrainRef != record.TrainId) {
          prevTrainRef = record.TrainId;
          currentTrainThread = new List<PlannedTrainRecord>();
          currentTrainThread.Add(record);
          trainThreads.Add(currentTrainThread);
        } else {
          currentTrainThread.Add(record);
        }
      }
      return trainThreads;
    }


  }
}
