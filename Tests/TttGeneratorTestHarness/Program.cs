using BCh.KTC.TttGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TttGeneratorTestHarness {
  class Program {
    static void Main(string[] args) {
      var generator = new Generator();
      generator.Start();
      Console.ReadLine();
    }
  }
}
