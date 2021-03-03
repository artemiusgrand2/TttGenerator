using System;

namespace BCh.KTC.TttEntities
{
    public class PlannedTrainRecord : BaseRecord
    { // TGraphicPL
      // public int RecId { get; set; } // ev_rec_idn
        public int TrainId { get; set; } // train_idn; ref to TrainHeaders
        public int EventType { get; set; } // ev_type: 1,2 - arrival; 3 - depature
        public DateTime ForecastTime { get; set; } // ev_time
        public DateTime PlannedTime { get; set; } // ev_time_p
        public string Station { get; set; } // ev_station
        public string Axis { get; set; } // ev_axis
        public string Ndo { get; set; } // ev_ndo
        public string NeighbourStationCode { get; set; } // ev_ne_station
                                                         //? ev_flag
        public int AckEventFlag { get; set; } // ev_cnfm: 2 - the event has been acknoledged
        public int PlannedEventReference { get; set; } // lnke_rec_idn
        public int AutopilotState { get; set; } // fl_def
        public string TrainNumber { get; set; }

        public override string ToString()
        {
            return string.Format($"{TrainId} st:{Station} a:{Axis} ndo:{Ndo} pt:{PlannedTime.ToShortTimeString()}");
        }

        public DateTime GetForecastTime2(TimeSpan deltaPlanExecuted)
        {
            var forecastTime = PlannedTime.Add(deltaPlanExecuted);
            if (ForecastTime < forecastTime)
                forecastTime = ForecastTime;
            //
            return forecastTime;
        }
    }
}
