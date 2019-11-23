using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttEntities;
using BCh.KTC.TttEntities.Enums;
using System.Collections.Generic;

namespace BCh.KTC.TttDal {
  public abstract class BaseUpdatingRepository<T> : BaseRepository<T>, IUpdatingRepository<T> where T : BaseRecord {
    protected FbCommand UpdatedCmd { get; private set; }

    public BaseUpdatingRepository(string conString, string selectCmdText, string updateCmdText)
      : base(conString, selectCmdText) {
      UpdatedCmd = new FbCommand(updateCmdText);
    }


    public void UpdateModifiedRecords(List<T> records) {
      using (var con = new FbConnection(ConnectionString)) {
        UpdatedCmd.Connection = con;
        foreach (var record in records) {
          if (record.RecordState == RecordState.Modified) {
            UpdateRecord(record);
          }
        }
      }
    }

    protected abstract void UpdateRecord(T record);

  }
}