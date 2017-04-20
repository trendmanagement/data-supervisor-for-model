using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization;
using AutoMapper;


namespace DataSupervisorForModel
{
    static class MongoDBConnectionAndSetup
    {
        private static IMongoClient _realtime_client;
        private static IMongoDatabase _realtime_database;

        private static IMongoCollection<Contract> _contractCollection;
        private static IMongoCollection<OHLCData> _futureBarCollection;


        private static IMongoClient _tmldb_v2_client;
        private static IMongoDatabase _tmldb_v2_database;

        private static IMongoCollection<Instrument_mongo> _instrumentCollection_tmldb_v2;
        private static IMongoCollection<Contract_mongo_tmldb> _contractCollection_tmldb_v2;
        private static IMongoCollection<Futures_contract_settlements_tmldb> _futureContractSettlements_tmldb_v2;


        static MongoDBConnectionAndSetup()
        {
            _realtime_client = new MongoClient(
                System.Configuration.ConfigurationManager.ConnectionStrings["Mongo_Realtime_MinuteBar_Connection"].ConnectionString);

            _realtime_database = _realtime_client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["Mongo_Realtime_DbName"]);

            _contractCollection = _realtime_database.GetCollection<Contract>(
                System.Configuration.ConfigurationManager.AppSettings["Mongo_Realtime_ContractCollection"]);

            _futureBarCollection = _realtime_database.GetCollection<OHLCData>(
                System.Configuration.ConfigurationManager.AppSettings["Mongo_Realtime_FutureBarCollection"]);

            var keys = Builders<OHLCData>.IndexKeys.Ascending("idcontract").Descending("bartime");
            _futureBarCollection.Indexes.CreateOneAsync(keys);


            _tmldb_v2_client = new MongoClient(
                System.Configuration.ConfigurationManager.ConnectionStrings["Mongo_tmldb_v2"].ConnectionString);

            _tmldb_v2_database = _tmldb_v2_client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["Mongo_tmldb_v2_DbName"]);

            _instrumentCollection_tmldb_v2 = _tmldb_v2_database.GetCollection<Instrument_mongo>(
                System.Configuration.ConfigurationManager.AppSettings["Mongo_tmldb_v2_InstrumentCollection"]);

            _contractCollection_tmldb_v2 = _tmldb_v2_database.GetCollection<Contract_mongo_tmldb>(
                System.Configuration.ConfigurationManager.AppSettings["Mongo_tmldb_v2_ContractCollection"]);

            _futureContractSettlements_tmldb_v2 = _tmldb_v2_database.GetCollection<Futures_contract_settlements_tmldb>(
                System.Configuration.ConfigurationManager.AppSettings["Mongo_tmldb_v2_FutureContractSettlementsCollection"]);


            //_futureBarCollection_localtime = _database.GetCollection<OHLCData_localtime>(
            //    System.Configuration.ConfigurationManager.AppSettings["MongoFutureBarCollection"]);
        }

        

        //public static IMongoCollection<Mongo_OptionSpreadExpression> MongoDataCollection
        //{
        //    get { return _database.GetCollection<Mongo_OptionSpreadExpression>(
        //        System.Configuration.ConfigurationManager.AppSettings["MongoCollection"]); }
        //}

        //internal static async Task dropCollection()
        //{
        //    await _realtime_database.DropCollectionAsync(
        //        System.Configuration.ConfigurationManager.AppSettings["MongoContractCollection"]);

        //    await _realtime_database.DropCollectionAsync(
        //        System.Configuration.ConfigurationManager.AppSettings["MongoFutureBarCollection"]);
        //}

        internal static async Task UpdateBardataToMongo(OHLCData barToUpsert)
        {

            try
            {

                var builder = Builders<OHLCData>.Filter;

                var filterForUpdate = builder.And(builder.Eq("idcontract", barToUpsert.idcontract),
                        builder.Eq("bartime", barToUpsert.bartime));

                var update = Builders<OHLCData>.Update
                            .Set("open", barToUpsert.open)
                            .Set("high", barToUpsert.high)
                            .Set("low", barToUpsert.low)
                            .Set("close", barToUpsert.close)
                            .Set("volume", barToUpsert.volume)
                            .Set("errorbar", barToUpsert.errorbar);

                await _futureBarCollection.UpdateOneAsync(filterForUpdate, update);

            }
            catch (Exception e)
            {
                MongoFailureMethod(e.ToString());

                //AsyncTaskListener.LogMessageAsync(e.ToString());

                //AsyncTaskListener.UpdateCQGDataManagementAsync();
            }

        }

        internal static async Task UpsertBardataToMongo(OHLCData barToUpsert)
        {
            try
            {
                var builder = Builders<OHLCData>.Filter;

                var filterForUpdate = builder.And(builder.Eq("idcontract", barToUpsert.idcontract),
                        builder.Eq("bartime", barToUpsert.bartime));

                var update = Builders<OHLCData>.Update
                            //.SetOnInsert("_id", barToUpsert._id)
                            .SetOnInsert("idcontract", barToUpsert.idcontract)
                            .SetOnInsert("bartime", barToUpsert.bartime)
                            .Set("open", barToUpsert.open)
                            .Set("high", barToUpsert.high)
                            .Set("low", barToUpsert.low)
                            .Set("close", barToUpsert.close)
                            .Set("volume", barToUpsert.volume)
                            .Set("errorbar", barToUpsert.errorbar);

                //await _futureBarCollection.ReplaceOne<OHLCData>(filterForUpdate, barToUpsert);
                await _futureBarCollection.UpdateOneAsync(filterForUpdate, update,
                        new UpdateOptions { IsUpsert = true });
            }
            catch (Exception e)
            {
                MongoFailureMethod(e.ToString());

                //AsyncTaskListener.LogMessageAsync(e.ToString());

                //AsyncTaskListener.UpdateCQGDataManagementAsync();
            }
        }

        internal static async Task AddDataMongo(List<OHLCData> barsToAdd)
        {
            try
            {

                await _futureBarCollection.InsertManyAsync(barsToAdd);

            }
            catch (Exception e)
            {
                MongoFailureMethod(e.ToString());

                //AsyncTaskListener.LogMessageAsync(e.ToString());

                //AsyncTaskListener.UpdateCQGDataManagementAsync();

                //AsyncTaskListener.LogMessageAsync("CQG API Connection has been Stopped");
            }
            
        }

        internal static void MongoFailureMethod(string errorMessage)
        {
            lock (AsyncTaskListener._InSetupAndConnectionMode)
            {
                if (!AsyncTaskListener._InSetupAndConnectionMode.value)
                {
                    AsyncTaskListener.Set_InSetupAndConnectionMode(true);

                    AsyncTaskListener.LogMessageAsync(errorMessage);

                    AsyncTaskListener.UpdateCQGDataManagementAsync();

                    AsyncTaskListener.LogMessageAsync("CQG API Connection has been Stopped. \nThere has been an error connecting to the MongoDB. \nThe program is attempting a complete recycle.");
                }
            }
        }


        internal static Dictionary<long, List<Contract>> GetContractListFromMongo_tmldb_v2(List<Instrument_mongo> instrumentList,
            //ref Dictionary<long, List<Contract>> contractHashTableByInstId,
            DateTime todaysDate)
        {
            //List<Contract> mongoContractList = _contractCollection_tmldb_v2.Find(_ => true).ToList();

            //var mongoContractDictionary = mongoContractList.ToDictionary(x => x.idcontract, x => x);

            //return mongoContractDictionary;

            Dictionary<long, List<Contract>> contractHashTableByInstId = new Dictionary<long, List<Contract>>();

            Mapper.Initialize(cfg => cfg.CreateMap<Contract_mongo_tmldb, Contract>());

            foreach (Instrument_mongo instrument in instrumentList)
            {
                //_contractCollection_tmldb_v2

                var builder = Builders<Contract_mongo_tmldb>.Filter;
                var filter = builder.And(
                        builder.Eq("idinstrument", instrument.idinstrument),
                        builder.Gte("expirationdate", todaysDate)
                    );

                List<Contract_mongo_tmldb> contracts = _contractCollection_tmldb_v2.Find(filter)
                    .Sort(Builders<Contract_mongo_tmldb>
                        .Sort.Ascending("expirationdate")).Limit(4).ToList<Contract_mongo_tmldb>();

                foreach (Contract_mongo_tmldb contractFromDb in contracts)
                {
                    Contract contract = Mapper.Map<Contract>(contractFromDb);

                    Console.WriteLine(contract.idcontract + " " + contract.idinstrument + " " + contract.contractname
                        + " " + contract.expirationdate);

                    if (contractHashTableByInstId.ContainsKey(contract.idinstrument))
                    {
                        contractHashTableByInstId[contract.idinstrument].Add(contract);
                    }
                    else
                    {
                        List<Contract> contractList = new List<Contract>();

                        contractList.Add(contract);

                        contractHashTableByInstId.Add(contract.idinstrument, contractList);
                    }


                }




            }

            return contractHashTableByInstId;
        }

        internal static void RemoveExtraContracts(Dictionary<long, Contract> contractListFromMongo)
        {
            foreach (KeyValuePair<long, Contract> contractInMongoHashEntry in contractListFromMongo)
            {
                var filterContracts = Builders<Contract>.Filter.Eq("idcontract", contractInMongoHashEntry.Key);

                _contractCollection.DeleteMany(filterContracts);

                var filterForBars = Builders<OHLCData>.Filter.Eq("idcontract", contractInMongoHashEntry.Key);

                _futureBarCollection.DeleteMany(filterForBars);
                
            }
        }

        /// <summary>
        /// This gets the contract from the mongodb
        /// it will run sychronously
        /// </summary>
        /// <param name="previousDateCollectionStart"></param>
        /// <param name="contract"></param>
        /// <param name="instrument"></param>
        /// <returns>OptionSpreadExpression</returns>

        internal static OptionSpreadExpression GetContractFromMongo(Contract contract, Instrument_mongo instrument)
        {

            OptionSpreadExpression ose = new OptionSpreadExpression();

            try
            { 
                var filter = Builders<Contract>.Filter.Eq("idcontract", contract.idcontract);

                Contract mongoContract = _contractCollection.Find(filter).SingleOrDefault();

                ose.contract = contract;

                if (mongoContract == null ||
                    mongoContract.previousDateTimeBoundaryStart.CompareTo(contract.previousDateTimeBoundaryStart) != 0)
                {
                

                    _contractCollection.ReplaceOne(filter, contract, new UpdateOptions { IsUpsert = true });

                    //var builder = Builders<OHLCData>.Filter;

                    //var filterForBars = builder.Eq("idcontract", contract.idcontract) & builder.AnyGte("bartime", contract.previousDateTimeBoundaryStart);

                    ////_futureBarCollection.DeleteMany(filterForBars);



                    //ose.futureBarData = _futureBarCollection.Find(filterForBars).ToList<OHLCData>();
                }

                ose.normalSubscriptionRequest = true;

                ose.instrument = instrument;

            }
            catch (Exception e)
            {
                MongoFailureMethod(e.ToString());
                //TSErrorCatch.debugWriteOut(e.ToString());
                //AsyncTaskListener.LogMessage(e.ToString());

                //cQGDataManagement.shutDownCQGConn();

                return null;
            }

            return ose;

        }

        internal static void GetFutureBarsFromMongo(OptionSpreadExpression ose)
        {

            var builder = Builders<OHLCData>.Filter;

            var filterForBars = builder.And(builder.Eq("idcontract", ose.contract.idcontract),
                    builder.Gte("bartime", ose.contract.previousDateTimeBoundaryStart));

            List<OHLCData> lastBarIn = _futureBarCollection.Find(filterForBars)
                .Sort(Builders<OHLCData>.Sort.Descending("bartime"))
                .Limit(1)    
                .ToList<OHLCData>();

            if(lastBarIn.Count > 0)
            {
                if (lastBarIn[0].bartime.CompareTo(ose.contract.previousDateTimeBoundaryStart) > 0)
                {
                    ose.CQGBarQueryStart = lastBarIn[0].bartime;
                }
                else
                {
                    ose.CQGBarQueryStart = ose.contract.previousDateTimeBoundaryStart;
                }
            }
            else
            {
                ose.CQGBarQueryStart = ose.contract.previousDateTimeBoundaryStart;
            }

        }


        internal static List<Instrument_mongo> GetInstrumentInfoListFromMongo()
        {
            try
            {
                var builder = Builders<Instrument_mongo>.Filter;
                var filter = builder.And(
                    builder.Or(
                        builder.Eq("optionenabled",2),
                        builder.Eq("optionenabled", 4),
                        builder.Eq("optionenabled", 8)
                        ),
                    builder.Ne("idinstrument",1022)
                    );

                // x => x.idinstrument, instrumentIdList

                return _instrumentCollection_tmldb_v2.Find(filter).ToList();
            }
            catch
            {
                return new List<Instrument_mongo>();
            }
        }

        internal static DateTime GetContractPreviousDateTimeFromMongo(long idcontract)
        {
            try
            {
                var builder = Builders<Futures_contract_settlements_tmldb>.Filter;
                var filter = builder.Eq("idcontract", idcontract);

                Futures_contract_settlements_tmldb settlement = _futureContractSettlements_tmldb_v2.Find(filter)
                        .Sort(Builders<Futures_contract_settlements_tmldb>
                            .Sort.Descending("date")).First();

                if (settlement != null)
                {
                    return settlement.date;
                }
            }
            catch
            {
                return DateTime.Today.AddDays(-1);
            }

            return DateTime.Today.AddDays(-1);
        }


    }
}
