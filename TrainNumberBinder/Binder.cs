using System;
using System.Timers;
using BCh.KTC.TrainNumberBinder.Config;
using BCh.KTC.TttDal;
using log4net;

namespace BCh.KTC.TrainNumberBinder {
  public class Binder {
    private static readonly ILog _logger = LogManager.GetLogger(typeof(Binder));

    private BinderEngine _engine;

    private Timer _timer;

    public Binder() {
      Initialize();
    }

    private void Initialize() {
      _logger.Info("Initializing TrainNumberBinder...");
      string gidDbConString = BinderConfig.GetGidDbConnectionString();
      _logger.Info($"Configured GidDb conString: {gidDbConString}");

      var plannedThreadsRepository = new PlannedThreadsRepository(gidDbConString);
      var passedThreadsRepository = new PassedThreadsRepository(gidDbConString);
      var trainHeadersRepository = new TrainHeadersRepository(gidDbConString);
      var storedProcExecutor = new StoredProcExecutor(gidDbConString);

      var maxBindDelta = BinderConfig.GetMaxBindDelta();
      if (maxBindDelta < 0) {
        _logger.WarnFormat("MaxBindDelta set in configuration is: {0}. MaxBindDelta re-set is: 30", maxBindDelta);
        maxBindDelta = 30;
      } else {
        _logger.InfoFormat("MaxBindDelta set is: {0}", maxBindDelta);
      }

      _engine = new BinderEngine(trainHeadersRepository,
        plannedThreadsRepository, passedThreadsRepository, storedProcExecutor,
        maxBindDelta);

      int cycleTime = BinderConfig.GetCycleTime();
      if (cycleTime < 10) {
        _logger.WarnFormat("CycleTime set in configuration is: {0}. CycleTime re-set is: 10", cycleTime);
      } else {
        _logger.InfoFormat("CycleTime set is: {0}", cycleTime);
      }
      _timer = new Timer(cycleTime * 1000);
      _timer.Elapsed += TimerElapsed;
    }

    public void Start() {
      _timer.Enabled = true;
      _logger.Info("TrainNumberBinder started.");
    }

    public void Stop() {
      _timer.Enabled = false;
      _logger.Info("TrainNumber Binder stopped.");
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


    public void PerformMainCycle() {
      _engine.ExecuteBindingCycle(DateTime.Now);
    }
  }
}
