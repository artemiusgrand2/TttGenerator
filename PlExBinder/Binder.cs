using BCh.KTC.PlExBinder.Config;
using BCh.KTC.PlExBinder.Interfaces;
using BCh.KTC.TttDal;
using log4net;
using System;
using System.Timers;

namespace BCh.KTC.PlExBinder {
  public class Binder {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(Binder));

    private string _gidDbConString;
    private BinderEngine _engine;

    private Timer _timer;

    public Binder() {
      Initialize();
    }

    private void Initialize() {
      _logger.Info("Initializing PlExBinder...");
      _gidDbConString = BinderConfig.GetGidDbConnectionString();
      _logger.Info($"Configured GidDb conString: {_gidDbConString}");

      var plannedThreadsRepository = new PlannedThreadsRepository(_gidDbConString);
      var passedThreadsRepository = new PassedThreadsRepository(_gidDbConString);
      var trainHeadersRepository = new TrainHeadersRepository(_gidDbConString);
      var storedProcExecutor = new StoredProcExecutor(_gidDbConString);
      IDeferredTaskStorage deferredTaskStorage = new DeferredTaskStorage();
      BinderConfigDto config = BinderConfig.GetBinderConfig();
      _engine = new BinderEngine(plannedThreadsRepository, passedThreadsRepository,
        trainHeadersRepository, storedProcExecutor, deferredTaskStorage, config);


      int cycleTime = BinderConfig.GetCycleTime();
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
      _logger.Info("PlExBinder started.");
    }

    public void Stop() {
      _timer.Enabled = false;
      _logger.Info("PlExBinder stopped.");
    }

    private void TimerElapsed(object sender, ElapsedEventArgs e) {
      _timer.Enabled = false;
      try {
        PerformMainCycle();
      }
      catch (Exception ex) {
        _logger.Error("An exception occurred while performing the main cycle", ex);
      }
      _timer.Enabled = true;
    }


    public void PerformMainCycle() {
      _engine.ExecuteBindingCycle(DateTime.Now);
    }

  }
}
