using System;
using System.Data;

namespace BCh.KTC.TttDal {
  public static class RepositoryUtils {
    public static string GetStringSafely(this IDataReader dataReader, int columnIndex) {
      if (dataReader.IsDBNull(columnIndex)) {
        return string.Empty;
      }
      return dataReader.GetString(columnIndex);
    }

    public static int GetInt16Safely(this IDataReader dataReader, int columnIndex) {
      if (dataReader.IsDBNull(columnIndex)) {
        return -1;
      }
      return dataReader.GetInt16(columnIndex);
    }

    public static int GetInt32Safely(this IDataReader dataReader, int columnIndex) {
      if (dataReader.IsDBNull(columnIndex)) {
        return -1;
      }
      return dataReader.GetInt32(columnIndex);
    }

    public static int GetByteSafely(this IDataReader dataReader, int columnIndex) {
      if (dataReader.IsDBNull(columnIndex)) {
        return -1;
      }
      return dataReader.GetByte(columnIndex);
    }

    public static DateTime GetMinDateTimeIfNull(this IDataReader dataReader, int columnIndex) {
      if (dataReader.IsDBNull(columnIndex)) {
        return DateTime.MinValue;
      }
      return dataReader.GetDateTime(columnIndex);
    }
  }
}
