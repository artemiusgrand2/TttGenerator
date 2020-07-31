using System;
using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttDal.Interfaces {
  public interface ITtTaskRepository {
    List<TtTaskRecord> GetTtTasks();
    void InsertTtTask(TtTaskRecord task);
    void UpdateExecTimeTask(DateTime execTime, int defIdn);
    }
}
