namespace BCh.KTC.TttDal.Interfaces {
  public interface ITrainHeadersRepository {
    bool IsTrainThreadBound(int trainId);
    string GetTrainNumberByTrainId(int trainId);
  }
}
