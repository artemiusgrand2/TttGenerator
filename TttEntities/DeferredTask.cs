using System;

namespace BCh.KTC.TttEntities {
    public class DeferredTask
    {
        public int EventId { get; set; }                // EV_REC_IDN
        public int TrainId { get; set; }                // TRAIN_IDN
        public int EventType { get; set; }              // EV_TYPE
        public string EventStation { get; set; }        // EV_STATION
        public DateTime PlannedTime { get; set; }       // EV_TIME_P
        public string EventAxis { get; set; }           // EV_AXIS
        public string EventNdoObject { get; set; }      // EV_NDO
        public DateTime CreationTime { get; set; }
        public bool HasBindingCmdBeenGenerated { get; set; }
        public string NeighbourStationCode { get; set; } // ev_ne_station

    }
}
