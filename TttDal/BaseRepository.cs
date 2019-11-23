using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttEntities;
using System.Collections.Generic;

namespace BCh.KTC.TttDal {
  public abstract class BaseRepository<T> : IRepository<T> where T : BaseRecord {
    protected readonly string ConnectionString;
    protected FbCommand SelectCmd { get; private set; }

    public BaseRepository(string conString, string selectCmdText) {
      ConnectionString = conString;
      SelectCmd = new FbCommand(selectCmdText);
    }

    public List<T> RetrieveRecords() {
      List<T> retRecords = new List<T>();
      using (var con = new FbConnection(ConnectionString)) {
        con.Open();
        SelectCmd.Connection = con;
        using (var dr = SelectCmd.ExecuteReader()) {
          while (dr.Read()) {
            var record = RetrieveRecord(dr);
            retRecords.Add(record);
          }
        }
      }
      return retRecords;
    }

    protected abstract T RetrieveRecord(FbDataReader dr);
  }
}
