﻿using BCh.KTC.TttGenerator.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TttConfigHarness {
  class Program {
    static void Main(string[] args) {
      try {
        Configuration config =
          ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        EngineSection engineSection = new EngineSection();
        var st1 = new ControlledStationElement();
        st1.Id = "te123456";
        var col = new ControlledStationCollection();
        col.Add(st1);
        st1 = new ControlledStationElement();
        st1.Id = "te654321";
        col.Add(st1);
        engineSection.ControlledStations = col;

        config.Sections.Remove("engine");
        config.Sections.Add("engine", engineSection);
        config.ConnectionStrings.ConnectionStrings.Clear();
        var connStringSettings = new ConnectionStringSettings("gidDb",
          @"Dialect=3;Database=10.20.47.69:C:\Неман\Минск-Узел\Events\GID.GDB;User Id=NEMAN;Password=NEMAN");
        config.ConnectionStrings.ConnectionStrings.Add(connStringSettings);
        string templateConfPath = Path.ChangeExtension(config.FilePath, ".~config");
        config.SaveAs(templateConfPath, ConfigurationSaveMode.Full);
      }
      catch (Exception ex) {
        int i = 10;
      }
    }
  }
}
