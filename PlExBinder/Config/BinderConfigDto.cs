namespace BCh.KTC.PlExBinder.Config {
  public class BinderConfigDto {
    public int SearchThresholdBeforePlannedTask { get; set; }
    public int SearchThresholdBeforeCurrentTime { get; set; }
    public int DeferredTimeLifespan { get; set; }
  }
}
