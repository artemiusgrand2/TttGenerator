using FirebirdSql.Data.FirebirdClient;
using BCh.KTC.TttDal.Interfaces;

namespace BCh.KTC.TttDal {
  public class StoredProcExecutor : IStoredProcExecutor {
    private const string SpText = "EXECUTE PROCEDURE ADD_NEW_Command"
      + " @Command,@COM_TIME"
      + ",@Train_IdnH1,@NORM_IDNH1,@Train_NumH1,@I_ST_FORMH1,@I_ST_DESTH1,@I_SOST_NUMH1"
      + ",@DOP_PROPH1,@REZERVH1,@ST_IN_ZONEH1,@ST_OUT_ZONEH1"
      + ",@Train_IdnH2,@NORM_IDNH2,@Train_NumH2,@I_ST_FORMH2,@I_ST_DESTH2,@I_SOST_NUMH2"
      + ",@DOP_PROPH2,@REZERVH2,@ST_IN_ZONEH2,@ST_OUT_ZONEH2"
      + ",@TRAIN_IDNE1,@EV_TYPEE1,@EV_TIMEE1,@Ev_StationE1,@EV_AXISE1,@Ev_NDOE1,@EV_NE_STATIONE1,@EV_REC_IDNE1,@EV_FLAGE1"
      + ",@TRAIN_IDNE2,@EV_TYPEE2,@EV_TIMEE2,@Ev_StationE2,@EV_AXISE2,@Ev_NDOE2,@EV_NE_STATIONE2,@EV_REC_IDNE2,@EV_FLAGE2";
    private readonly FbCommand _command2;
    private readonly FbParameter _parCommand2;
    private readonly FbParameter _parCOM_TIME2;
    private readonly FbParameter _parTrain_IdnH12;
    private readonly FbParameter _parNORM_IDNH12;
    private readonly FbParameter _parTrain_NumH12;
    private readonly FbParameter _parI_ST_FORMH12;
    private readonly FbParameter _parI_ST_DESTH12;
    private readonly FbParameter _parI_SOST_NUMH12;
    private readonly FbParameter _parDOP_PROPH12;
    private readonly FbParameter _parREZERVH12;
    private readonly FbParameter _parST_IN_ZONEH12;
    private readonly FbParameter _parST_OUT_ZONEH12;
    private readonly FbParameter _parTrain_IdnH22;
    private readonly FbParameter _parNORM_IDNH22;
    private readonly FbParameter _parTrain_NumH22;
    private readonly FbParameter _parI_ST_FORMH22;
    private readonly FbParameter _parI_ST_DESTH22;
    private readonly FbParameter _parI_SOST_NUMH22;
    private readonly FbParameter _parDOP_PROPH22;
    private readonly FbParameter _parREZERVH22;
    private readonly FbParameter _parST_IN_ZONEH22;
    private readonly FbParameter _parST_OUT_ZONEH22;
    private readonly FbParameter _parTRAIN_IDNE12;
    private readonly FbParameter _parEV_TYPEE12;
    private readonly FbParameter _parEV_TIMEE12;
    private readonly FbParameter _parEv_StationE12;
    private readonly FbParameter _parEV_AXISE12;
    private readonly FbParameter _parEv_NDOE12;
    private readonly FbParameter _parEV_NE_STATIONE12;
    private readonly FbParameter _parEV_REC_IDNE12;
    private readonly FbParameter _parEV_FLAGE12;
    private readonly FbParameter _parTRAIN_IDNE22;
    private readonly FbParameter _parEV_TYPEE22;
    private readonly FbParameter _parEV_TIMEE22;
    private readonly FbParameter _parEv_StationE22;
    private readonly FbParameter _parEV_AXISE22;
    private readonly FbParameter _parEv_NDOE22;
    private readonly FbParameter _parEV_NE_STATIONE22;
    private readonly FbParameter _parEV_REC_IDNE22;
    private readonly FbParameter _parEV_FLAGE22;


    private readonly string _conString;


    public StoredProcExecutor(string conString) {
      _conString = conString;
      _command2 = new FbCommand(SpText);
      _parCommand2 = new FbParameter("@Command", FbDbType.Integer);
      _parCOM_TIME2 = new FbParameter("@COM_TIME", FbDbType.TimeStamp);
      _parTrain_IdnH12 = new FbParameter("@Train_IdnH1", FbDbType.Integer);
      _parNORM_IDNH12 = new FbParameter("@NORM_IDNH1", FbDbType.Integer);
      _parTrain_NumH12 = new FbParameter("@Train_NumH1", FbDbType.VarChar);
      _parI_ST_FORMH12 = new FbParameter("@I_ST_FORMH1", FbDbType.VarChar);
      _parI_ST_DESTH12 = new FbParameter("@I_ST_DESTH1", FbDbType.VarChar);
      _parI_SOST_NUMH12 = new FbParameter("@I_SOST_NUMH1", FbDbType.Integer);
      _parDOP_PROPH12 = new FbParameter("@DOP_PROPH1", FbDbType.VarChar);
      _parREZERVH12 = new FbParameter("@REZERVH1", FbDbType.Integer);
      _parST_IN_ZONEH12 = new FbParameter("@ST_IN_ZONEH1", FbDbType.VarChar);
      _parST_OUT_ZONEH12 = new FbParameter("@ST_OUT_ZONEH1", FbDbType.VarChar);
      _parTrain_IdnH22 = new FbParameter("@Train_IdnH2", FbDbType.Integer);
      _parNORM_IDNH22 = new FbParameter("@NORM_IDNH2", FbDbType.Integer);
      _parTrain_NumH22 = new FbParameter("@Train_NumH2", FbDbType.VarChar);
      _parI_ST_FORMH22 = new FbParameter("@I_ST_FORMH2", FbDbType.VarChar);
      _parI_ST_DESTH22 = new FbParameter("@I_ST_DESTH2", FbDbType.VarChar);
      _parI_SOST_NUMH22 = new FbParameter("@I_SOST_NUMH2", FbDbType.Integer);
      _parDOP_PROPH22 = new FbParameter("@DOP_PROPH2", FbDbType.VarChar);
      _parREZERVH22 = new FbParameter("@REZERVH2", FbDbType.Integer);
      _parST_IN_ZONEH22 = new FbParameter("@ST_IN_ZONEH2", FbDbType.VarChar);
      _parST_OUT_ZONEH22 = new FbParameter("@ST_OUT_ZONEH2", FbDbType.VarChar);
      _parTRAIN_IDNE12 = new FbParameter("@TRAIN_IDNE1", FbDbType.Integer);
      _parEV_TYPEE12 = new FbParameter("@EV_TYPEE1", FbDbType.Integer);
      _parEV_TIMEE12 = new FbParameter("@EV_TIMEE1", FbDbType.TimeStamp);
      _parEv_StationE12 = new FbParameter("@Ev_StationE1", FbDbType.VarChar);
      _parEV_AXISE12 = new FbParameter("@EV_AXISE1", FbDbType.VarChar);
      _parEv_NDOE12 = new FbParameter("@Ev_NDOE1", FbDbType.VarChar);
      _parEV_NE_STATIONE12 = new FbParameter("@EV_NE_STATIONE1", FbDbType.VarChar);
      _parEV_REC_IDNE12 = new FbParameter("@EV_REC_IDNE1", FbDbType.Integer);
      _parEV_FLAGE12 = new FbParameter("@EV_FLAGE1", FbDbType.Integer);
      _parTRAIN_IDNE22 = new FbParameter("@TRAIN_IDNE2", FbDbType.Integer);
      _parEV_TYPEE22 = new FbParameter("@EV_TYPEE2", FbDbType.Integer);
      _parEV_TIMEE22 = new FbParameter("@EV_TIMEE2", FbDbType.TimeStamp);
      _parEv_StationE22 = new FbParameter("@Ev_StationE2", FbDbType.VarChar);
      _parEV_AXISE22 = new FbParameter("@EV_AXISE2", FbDbType.VarChar);
      _parEv_NDOE22 = new FbParameter("@Ev_NDOE2", FbDbType.VarChar);
      _parEV_NE_STATIONE22 = new FbParameter("@EV_NE_STATIONE2", FbDbType.VarChar);
      _parEV_REC_IDNE22 = new FbParameter("@EV_REC_IDNE2", FbDbType.Integer);
      _parEV_FLAGE22 = new FbParameter("@EV_FLAGE2", FbDbType.Integer);

      _command2.Parameters.Add(_parCommand2);
      _command2.Parameters.Add(_parCOM_TIME2);
      _command2.Parameters.Add(_parTrain_IdnH12);
      _command2.Parameters.Add(_parNORM_IDNH12);
      _command2.Parameters.Add(_parTrain_NumH12);
      _command2.Parameters.Add(_parI_ST_FORMH12);
      _command2.Parameters.Add(_parI_ST_DESTH12);
      _command2.Parameters.Add(_parI_SOST_NUMH12);
      _command2.Parameters.Add(_parDOP_PROPH12);
      _command2.Parameters.Add(_parREZERVH12);
      _command2.Parameters.Add(_parST_IN_ZONEH12);
      _command2.Parameters.Add(_parST_OUT_ZONEH12);
      _command2.Parameters.Add(_parTrain_IdnH22);
      _command2.Parameters.Add(_parNORM_IDNH22);
      _command2.Parameters.Add(_parTrain_NumH22);
      _command2.Parameters.Add(_parI_ST_FORMH22);
      _command2.Parameters.Add(_parI_ST_DESTH22);
      _command2.Parameters.Add(_parI_SOST_NUMH22);
      _command2.Parameters.Add(_parDOP_PROPH22);
      _command2.Parameters.Add(_parREZERVH22);
      _command2.Parameters.Add(_parST_IN_ZONEH22);
      _command2.Parameters.Add(_parST_OUT_ZONEH22);
      _command2.Parameters.Add(_parTRAIN_IDNE12);
      _command2.Parameters.Add(_parEV_TYPEE12);
      _command2.Parameters.Add(_parEV_TIMEE12);
      _command2.Parameters.Add(_parEv_StationE12);
      _command2.Parameters.Add(_parEV_AXISE12);
      _command2.Parameters.Add(_parEv_NDOE12);
      _command2.Parameters.Add(_parEV_NE_STATIONE12);
      _command2.Parameters.Add(_parEV_REC_IDNE12);
      _command2.Parameters.Add(_parEV_FLAGE12);
      _command2.Parameters.Add(_parTRAIN_IDNE22);
      _command2.Parameters.Add(_parEV_TYPEE22);
      _command2.Parameters.Add(_parEV_TIMEE22);
      _command2.Parameters.Add(_parEv_StationE22);
      _command2.Parameters.Add(_parEV_AXISE22);
      _command2.Parameters.Add(_parEv_NDOE22);
      _command2.Parameters.Add(_parEV_NE_STATIONE22);
      _command2.Parameters.Add(_parEV_REC_IDNE22);
      _command2.Parameters.Add(_parEV_FLAGE22);

    }


    public void BindPlannedAndPassedTrains(int plannedTrainId, int passedTrainId, string trainNumber, int flagPro) {
            using (var con = new FbConnection(_conString))
            {
                _command2.Connection = con;
                _parCommand2.Value = 35;
                _parTrain_IdnH12.Value = plannedTrainId;
                _parTrain_IdnH22.Value = passedTrainId;
                _parTrain_NumH12.Value = trainNumber;
                _parEV_FLAGE22.Value = flagPro;
                con.Open();
                _command2.ExecuteNonQuery();
            }
    }
  }
}
