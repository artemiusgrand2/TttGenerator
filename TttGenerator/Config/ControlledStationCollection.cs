using System.Configuration;

namespace BCh.KTC.TttGenerator.Config {
  [ConfigurationCollection(typeof(ControlledStationElement),
     CollectionType = ConfigurationElementCollectionType.BasicMap,
     AddItemName = "station")]
  public class ControlledStationCollection : ConfigurationElementCollection {
    private static readonly ConfigurationPropertyCollection _properties;

    static ControlledStationCollection() {
      _properties = new ConfigurationPropertyCollection();
    }

    public override ConfigurationElementCollectionType CollectionType
    {
      get { return ConfigurationElementCollectionType.BasicMap; }
    }

    protected override string ElementName
    {
      get { return "station"; }
    }

    protected override ConfigurationElement CreateNewElement() {
      return new ControlledStationElement();
    }

    protected override object GetElementKey(ConfigurationElement element) {
      return (element as ControlledStationElement).Id;
    }

    protected override ConfigurationPropertyCollection Properties
    {
      get { return _properties; }
    }

    public void Add(ControlledStationElement element) {
      base.BaseAdd(element);
    }
  }
}
