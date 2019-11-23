using System.Configuration;

namespace BCh.KTC.TttGenerator.Config {
  public class ControlledStationElement : ConfigurationElement {
    private static readonly ConfigurationProperty _id;
    private static readonly ConfigurationPropertyCollection _properties;

    static ControlledStationElement() {
      _id = new ConfigurationProperty("id", typeof(string), "", 
        ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsKey);
      _properties = new ConfigurationPropertyCollection();
      _properties.Add(_id);
    }

    public string Id {
      get { return base[_id] as string; }
      set { base[_id] = value; }
    }

    protected override ConfigurationPropertyCollection Properties {
      get { return _properties; }
    }
  }
}
