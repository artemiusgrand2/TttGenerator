using System.Collections.Generic;
using System.Configuration;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStationElement : ConfigurationElement {
    private static readonly ConfigurationProperty _id;
    private static readonly ConfigurationProperty _genNotCfmArr;
    private static readonly ConfigurationProperty _genNotCfmDep;
    private static readonly ConfigurationProperty _isCrossing;
    private static readonly ConfigurationProperty _isGidControl;
    private static readonly ConfigurationProperty _isControl;
    private static readonly ConfigurationProperty _listStNotDep;
    private static readonly ConfigurationPropertyCollection _properties;

        static ControlledStationElement()
        {
            _id = new ConfigurationProperty("id", typeof(string), "",
              ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
            _genNotCfmArr = new ConfigurationProperty("genNotCfmArr", typeof(bool), false);
            _genNotCfmDep = new ConfigurationProperty("genNotCfmDep", typeof(bool), false);
            _isCrossing = new ConfigurationProperty("isCrossing", typeof(bool), false);
            _isGidControl = new ConfigurationProperty("isGidControl", typeof(bool), true);
            _isControl = new ConfigurationProperty("isControl", typeof(bool), true);
            _listStNotDep =  new ConfigurationProperty("ListStNotDep", typeof(string), string.Empty);
            _properties = new ConfigurationPropertyCollection { _id, _genNotCfmArr, _genNotCfmDep, _isCrossing, _isGidControl, _isControl, _listStNotDep };
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

        public bool IsGidControl
        {
            get { return (bool)base[_isGidControl]; }
            set { base[_isGidControl] = value; }
        }

        public bool IsControl
        {
            get { return (bool)base[_isControl]; }
            set { base[_isControl] = value; }
        }

        public string ListStNotDep
        {
            get { return base[_listStNotDep] as string; }
            set { base[_listStNotDep] = value; }
        }

        protected override ConfigurationPropertyCollection Properties {
      get { return _properties; }
    }
  }
}
