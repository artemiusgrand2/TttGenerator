using BCh.KTC.TttGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace _3TGeneratorService {
  public partial class Service1 : ServiceBase {
    private readonly Generator _tttGenerator;

    public Service1() {
      InitializeComponent();
      _tttGenerator = new Generator();
    }

    protected override void OnStart(string[] args) {
      _tttGenerator.Start();
    }

    protected override void OnStop() {
      _tttGenerator.Stop();
    }
  }
}
