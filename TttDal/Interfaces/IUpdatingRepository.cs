using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttDal
{
  public interface IUpdatingRepository<T> : IRepository<T> where T : BaseRecord {
    void UpdateModifiedRecords(List<T> records);
  }
}
