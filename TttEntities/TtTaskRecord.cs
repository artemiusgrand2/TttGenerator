using System;

namespace BCh.KTC.TttEntities
{
    public class TtTaskRecord : BaseRecord
    { // TComDefinitions
      //public int RecId { get; set; } // def_idn
        public string Station { get; set; } // st_code
        public string TrainPrefix { get; set; } // tr_num_p
        public string TrainNumber { get; set; } // tr_num
        public string TrainSuffix { get; set; } // tr_num_s
        public int RouteStartObjectType { get; set; } // ob_stt_type
        public string RouteStartObjectName { get; set; } // ob_stt_name
        public int RouteEndObjectType { get; set; } // ob_end_type
        public string RouteEndObjectName { get; set; } // ob_end_name
        public int StopFlag { get; set; } // stay_fnd
                                          //? might not needed: lnk_def_idn_n
        public int DependencyEventReference { get; set; } // lnk_def_idn_e
        public int PlannedEventReference { get; set; } // ev_idn_pln
        public DateTime ExecutionTime { get; set; } // tm_def_start
        public DateTime CreationTime { get; set; } // tm_def_creat
        public int FormationFlag { get; set; } // std_form
        public int SentFlag { get; set; } // fl_snd
        public string ExecutionCode { get; set; } // run_code
                                                  // ? chk_sm

        public override string ToString()
        {
            return $"{PlannedEventReference}, tr:'{TrainNumber}' st:{Station}, from {RouteStartObjectType}:{RouteStartObjectName} to {RouteEndObjectType}:{RouteEndObjectName}";
        }

    }
}
