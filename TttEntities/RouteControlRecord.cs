using System;

namespace BCh.KTC.TttEntities
{
  public class RouteControlRecord : BaseRecord { // TRoutCont
    //public int RecId { get; set; } // recordid
    public string StationCode { get; set; } // station
    public string BaseObjectName { get; set; } // bo_name
    public int BaseObjectType { get; set; } // bo_type
    public string AddObjectName { get; set; } // do_name
    public int AddObjectType { get; set; } // do_type
    public int MessageType { get; set; } // ms_type
    public int MessageId { get; set; } // ms_idn
    public DateTime EventTime { get; set; } // ev_time
  }
}
