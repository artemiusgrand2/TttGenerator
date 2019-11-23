using BCh.KTC.TttEntities.Enums;

namespace BCh.KTC.TttEntities
{
  public abstract class BaseRecord {
    public int RecId { get; set; }
    public RecordState RecordState { get; set; }
  }
}
