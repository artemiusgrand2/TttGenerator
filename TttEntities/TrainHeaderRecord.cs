namespace BCh.KTC.TttEntities
{
  public class TrainHeaderRecord : BaseRecord { // TTrainHeaders
    // public int RecId { get; set; } // train_idn
    public int PlannedTrainThreadId { get; set; } // norm_idn
    public string TrainNumber { get; set; } // train_num
    public int StateFlag { get; set; } // fl_sost: null - passed, 1 - planned NOT bound with passed, 2 - planned bound with passed
    public string Index1 { get; set; } // i_st_form
    public string Index2 { get; set; } // i_st_num
    public string Index3 { get; set; } // i_st_dest
  }
}
