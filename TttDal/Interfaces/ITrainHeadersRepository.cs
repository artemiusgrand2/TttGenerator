using System.Collections.Generic;
using BCh.KTC.TttEntities;

namespace BCh.KTC.TttDal.Interfaces {
  public interface ITrainHeadersRepository {
    bool IsTrainThreadBound(int trainId);
    string GetTrainNumberByTrainId(int trainId);

        int? GetPassedIdByPlannedId(int trainId);

        List<TrainHeaderRecord> RetrieveNotBoundHeaders();

        bool SetStateFlag(int plannedId, int statFlag);

        bool DeletePlanRope(int trainId);
    }
}
