using BCh.KTC.TttDal;
using BCh.KTC.TttGenerator.Config;
using log4net;
using System;
using System.Timers;

namespace BCh.KTC.TttGenerator {
  public class Generator
  {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(Generator));

    private string _gidDbConString;
    private GeneratorEngine _engine;

    private Timer _timer;

    public Generator() {
      try {
        Initialize();
      } catch (Exception ex) {
        _logger.Fatal("An exception occurred during initialization", ex);
        throw;
      }
    }

    private void Initialize() {
      _logger.Info("Initializing TttGenerator...");
      _gidDbConString = GeneratorConfig.GetGidDbConnectionString();
      _logger.Info($"Configured GidDb conString: {_gidDbConString}");

      var controlledStations = GeneratorConfig.GetControlledStations();
      var plannedRepo = new PlannedThreadsRepository(_gidDbConString);
      var taskRepo = new TtTaskRepository(_gidDbConString);
      var trainHeadersRepo = new TrainHeadersRepository(_gidDbConString);
            var commandRepo = new CommandThreadsRepository(_gidDbConString);
            var timeConstraintCalculator = new TimeConstraintCalculator(controlledStations,
        GeneratorConfig.GetReserveTime(),
        GeneratorConfig.GetAdvanceCommandExecutionPeriod());
        _engine = new GeneratorEngine(timeConstraintCalculator,
        controlledStations,
        plannedRepo, taskRepo, trainHeadersRepo, commandRepo,
        GeneratorConfig.GetPrevAckTime());

      int cycleTime = GeneratorConfig.GetCycleTime();
      if (cycleTime < 10) {
        _logger.WarnFormat("CycleTime set in configuration is: {0}. CycleTime re-set is: 10", cycleTime);
      }
      else {
        _logger.InfoFormat("CycleTime set is: {0}", cycleTime);
      }
      _timer = new Timer(cycleTime * 1000);
      _timer.Elapsed += TimerElapsed;
    }

    public void Start() {
      _timer.Enabled = true;
      _logger.Info("TttGenerator started.");
    }

    public void Stop() {
      _timer.Enabled = false;
      _logger.Info("TttGenerator stopped.");
    }

    private void TimerElapsed(object sender, ElapsedEventArgs e) {
      _timer.Enabled = false;
      try {
        PerformWorkingCycle();
      }
      catch (Exception ex) {
        _logger.Error("An exception occurred while performing the main cycle", ex);
      }
      _timer.Enabled = true;
    }


    public void PerformWorkingCycle() {
      var nowTime = DateTime.Now;
      _engine.PerformWorkingCycle(nowTime);
    }

  }
}
