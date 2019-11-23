using System.Configuration;

namespace BCh.KTC.TttGenerator.Config {
  public class EngineSection : ConfigurationSection {
    private static readonly ConfigurationProperty _controlledCollection;
    private static readonly ConfigurationPropertyCollection _properties;

    static EngineSection() {
      _controlledCollection = new ConfigurationProperty("controlledStations", typeof(ControlledStationCollection));
      _properties = new ConfigurationPropertyCollection { _controlledCollection};
    }

    public ControlledStationCollection ControlledStations {
      get { return base[_controlledCollection] as ControlledStationCollection; }
      set { base[_controlledCollection] = value; }
    }

    protected override ConfigurationPropertyCollection Properties {
      get { return _properties; }
    }
  }

}
