using System.Collections.Generic;
using System.Configuration;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStationElement : ConfigurationElement {
    private static readonly ConfigurationProperty _id;
    private static readonly ConfigurationProperty _genNotCfmArr;
    private static readonly ConfigurationProperty _genNotCfmDep;
    private static readonly ConfigurationProperty _isCrossing;
    private static readonly ConfigurationProperty _listStNotDep;
    private static readonly ConfigurationPropertyCollection _properties; 
    private static readonly ConfigurationProperty _autonomous;

        static ControlledStationElement()
        {
            _id = new ConfigurationProperty("id", typeof(string), "",
              ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
            _genNotCfmArr = new ConfigurationProperty("genNotCfmArr", typeof(bool), false);
            _genNotCfmDep = new ConfigurationProperty("genNotCfmDep", typeof(bool), false);
            _isCrossing = new ConfigurationProperty("isCrossing", typeof(bool), false);
            _autonomous = new ConfigurationProperty("Autonomous", typeof(bool), false);
            _listStNotDep =  new ConfigurationProperty("ListStNotDep", typeof(string), string.Empty);
            _properties = new ConfigurationPropertyCollection { _id, _genNotCfmArr, _genNotCfmDep, _isCrossing,  _listStNotDep, _autonomous };
        }

    public string Id {
      get { return base[_id] as string; }
      set { base[_id] = value; }
    }

    public bool AllowGeneratingNotCfmArrival {
      get { return (bool)base[_genNotCfmArr]; }
      set { base[_genNotCfmArr] = value; }
    }

    public bool AllowGeneratingNotCfmDeparture {
      get { return (bool)base[_genNotCfmDep]; }
      set { base[_genNotCfmDep] = value; }
    }

        public bool IsCrossing
        {
            get { return (bool)base[_isCrossing]; }
            set { base[_isCrossing] = value; }
        }

        public string ListStNotDep
        {
            get { return base[_listStNotDep] as string; }
            set { base[_listStNotDep] = value; }
        }

        public bool Autonomous
        {
            get { return (bool)base[_autonomous]; }
            set { base[_autonomous] = value; }
        }

        protected override ConfigurationPropertyCollection Properties {
      get { return _properties; }
    }
  }
}
