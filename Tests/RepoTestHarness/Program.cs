using BCh.KTC.TttDal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepoTestHarness {
  class Program {
    static void Main(string[] args) {

      var passedTrainRepo = new PassedTrainRepository(ConfigurationManager.ConnectionStrings["testDb"].ConnectionString);
      var recs2 = passedTrainRepo.RetrieveRecords();
      Console.WriteLine($"PassedTrainRepository - retrieved {recs2.Count} records");

      var plannedTrainRepo = new PlannedThreadsRepository(ConfigurationManager.ConnectionStrings["testDb"].ConnectionString);
      var recs3 = plannedTrainRepo.RetrieveThreadsForTttGenerator(DateTime.Now.AddYears(-5));
      Console.WriteLine($"PlannedTrainRepository - retrieved {recs3.Count} records");

      var routeControlRepo = new RouteControlRepository(ConfigurationManager.ConnectionStrings["testDb"].ConnectionString);
      var recs4 = routeControlRepo.RetrieveRecords();
      Console.WriteLine($"RouteControlRepository - retrieved {recs4.Count} records");

      var taskRepo = new TtTaskRepository(ConfigurationManager.ConnectionStrings["testDb"].ConnectionString);
      var recs5 = taskRepo.GetTtTasks();
      Console.WriteLine($"Task Repo - retrieved {recs5.Count} records");

      Console.Read();
    }
  }
}
