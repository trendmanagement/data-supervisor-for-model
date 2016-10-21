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
        private static IMongoClient _client;
        private static IMongoDatabase _database;

        private static IMongoCollection<Contract> _contractCollection;
        private static IMongoCollection<OHLCData> _futureBarCollection;
        //private static IMongoCollection<OHLCData_localtime> _futureBarCollection_localtime;





        //private static string mongoDataCollection;

        static MongoDBConnectionAndSetup()
        {
            _client = new MongoClient(
                System.Configuration.ConfigurationManager.ConnectionStrings["DefaultMongoConnection"].ConnectionString);

            _database = _client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["MongoDbName"]);


            _contractCollection = _database.GetCollection<Contract>(
                System.Configuration.ConfigurationManager.AppSettings["MongoContractCollection"]);

            _futureBarCollection = _database.GetCollection<OHLCData>(
                System.Configuration.ConfigurationManager.AppSettings["MongoFutureBarCollection"]);

            var keys = Builders<OHLCData>.IndexKeys.Ascending("idcontract").Descending("bartime");
            _futureBarCollection.Indexes.CreateOneAsync(keys);


            //_futureBarCollection_localtime = _database.GetCollection<OHLCData_localtime>(
            //    System.Configuration.ConfigurationManager.AppSettings["MongoFutureBarCollection"]);
        }

        

        //public static IMongoCollection<Mongo_OptionSpreadExpression> MongoDataCollection
        //{
        //    get { return _database.GetCollection<Mongo_OptionSpreadExpression>(
        //        System.Configuration.ConfigurationManager.AppSettings["MongoCollection"]); }
        //}

        internal static async Task dropCollection()
        {
            await _database.DropCollectionAsync(
                System.Configuration.ConfigurationManager.AppSettings["MongoContractCollection"]);

            await _database.DropCollectionAsync(
                System.Configuration.ConfigurationManager.AppSettings["MongoFutureBarCollection"]);
        }

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


        internal static Dictionary<long, Contract> GetContractListFromMongo()
        {
            List<Contract> mongoContractList = _contractCollection.Find(_ => true).ToList();

            var mongoContractDictionary = mongoContractList.ToDictionary(x => x.idcontract, x => x);

            return mongoContractDictionary;
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

        internal static OptionSpreadExpression GetContractFromMongo(Contract contract, Instrument instrument)
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


        //internal static async Task createDocument()
        //{
        //    var document = new BsonDocument
        //    {
        //        { "address" , new BsonDocument
        //            {
        //                { "street", "2 Avenue" },
        //                { "zipcode", "10075" },
        //                { "building", "1480" },
        //                { "coord", new BsonArray { 73.9557413, 40.7720266 } }
        //            }
        //        },
        //        { "borough", "Manhattan" },
        //        { "cuisine", "Italian" },
        //        { "grades", new BsonArray
        //            {
        //                new BsonDocument
        //                {
        //                    { "date", new DateTime(2014, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
        //                    { "grade", "A" },
        //                    { "score", 11 }
        //                },
        //                new BsonDocument
        //                {
        //                    { "date", new DateTime(2014, 1, 6, 0, 0, 0, DateTimeKind.Utc) },
        //                    { "grade", "B" },
        //                    { "score", 17 }
        //                }
        //            }
        //        },
        //        { "name", "Vella" },
        //        { "restaurant_id", "41704620" }
        //    };

        //    var collection = _database.GetCollection<BsonDocument>("restaurants");
        //    await collection.InsertOneAsync(document);
        //}

        internal static async Task getDocument()
        {
            var collection = _database.GetCollection<BsonDocument>("restaurants");

            //var docs = collection.Find(new BsonDocument()).ToListAsync().GetAwaiter().GetResult();

            //foreach(var x in docs)
            //{
            //    Console.WriteLine(x.cqgSymbol);
            //}


            var filter = new BsonDocument();
            var count = 0;
            using (var cursor = await collection.FindAsync(filter))
            {
                while (await cursor.MoveNextAsync())
                {
                    var batch = cursor.Current;
                    foreach (var document in batch)
                    {
                        // process document
                        Console.WriteLine(document);



                        //var x = BsonSerializer.Deserialize<(document);

                        //var x = MongoDB.Bson.BsonDocument.

                        //JsonSerializer serializer = new JsonSerializer();

                        //TestExpression x = serializer.Deserialize<TestExpression>(document);

                        //Console.WriteLine(document["cqgSymbol"]);

                        count++;
                    }
                }
            }
        }
    }
}
