using System;

namespace BCh.KTC.TttEntities
{
  public class PassedTrainRecord : BaseRecord { // TGraphicID
    // public int RecId { get; set; } // ev_rec_idn
    public int TrainId { get; set; } // train_idn; ref to TrainHeaders
    public int EventType { get; set; } // ev_type: 1,2 - arrvial; 3 - departure
    public DateTime EventTime { get; set; } // ev_time
    public string Station { get; set; } // ev_station
    public string Axis { get; set; } // ev_axis
    public string Ndo { get; set; } // ev_ndo
    public int NdoType { get; set; } // ev_dop: 3 - track, 5 - b/u
    public string NeighbourStationCode { get; set; } // ev_ne_station
    public DateTime PlannedTime { get; set; } // ev_time_p
  }
}
