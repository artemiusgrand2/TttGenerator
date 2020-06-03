using System.Collections.Generic;

namespace BCh.KTC.TttDal.Interfaces
{
    public interface ICommandThreadsRepository
    {
        bool IsCommandBindPlanToTrain(int plandId);
    }
}