using BCh.KTC.PlExBinder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PlExBinderService {
  public partial class Service1 : ServiceBase {
    private readonly Binder _binder;

    public Service1() {
      InitializeComponent();
      _binder = new Binder();
    }

    protected override void OnStart(string[] args) {
      _binder.Start();
    }

    protected override void OnStop() {
      _binder.Stop();
    }
  }
}
