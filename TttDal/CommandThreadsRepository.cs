using BCh.KTC.TttDal.Interfaces;
using System;
using System.Collections.Generic;
using FirebirdSql.Data.FirebirdClient;

namespace BCh.KTC.TttDal
{
    public class CommandThreadsRepository : ICommandThreadsRepository
    {

        private const string ВindingCmdText
        = "SELECT 1 FROM TGRFCOMM"
        + " WHERE TRAIN_IDNH1 = @planId AND COMMAND = 35 AND TRAIN_IDNH2 <> 0";

        private readonly string _connectionString;
        private readonly FbCommand _bindingCmd;
        private readonly FbParameter _parPlanId;


        public CommandThreadsRepository(string conString)
        {
            _connectionString = conString;
            _bindingCmd = new FbCommand(ВindingCmdText);
            _parPlanId = new FbParameter("@planId", FbDbType.Integer);
            _bindingCmd.Parameters.Add(_parPlanId);
        }

        public bool IsCommandBindPlanToTrain(int plandId)
        {
            using (var con = new FbConnection(_connectionString))
            {
                _bindingCmd.Connection = con;
                _parPlanId.Value = plandId;
                con.Open();
                using (var dbReader = _bindingCmd.ExecuteReader())
                {
                    return dbReader.Read();
                }
            }
        }
    }
}
