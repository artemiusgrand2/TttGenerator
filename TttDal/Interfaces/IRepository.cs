using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttDal
{
  public interface IRepository<T> where T : BaseRecord {
    List<T> RetrieveRecords();
  }
}
