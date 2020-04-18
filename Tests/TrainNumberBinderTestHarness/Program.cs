using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCh.KTC.TrainNumberBinder;

namespace TrainNumberBinderTestHarness {
  class Program {
    static void Main(string[] args) {
      Console.WriteLine("Starting the Binder...");

      var binder = new Binder();
      binder.Start();

      Console.ReadLine();
    }
  }
}
