namespace BCh.KTC.TttEntities {
  public class StationTimeRecord {
    public int TimeType { get; set; }
    public string StationCode { get; set; }
    public int StartObjectType { get; set; }
    public string StartObjectName { get; set; }
    public int EndObjectType { get; set; }
    public string EndObjectName { get; set; }
    public int TimeValue { get; set; }
  }
}
