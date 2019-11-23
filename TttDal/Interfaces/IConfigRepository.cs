using BCh.KTC.TttEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCh.KTC.TttDal.Interfaces {
  public interface IConfigRepository {
    List<StationTimeRecord> GetStationTimeRecords();
  }
}
