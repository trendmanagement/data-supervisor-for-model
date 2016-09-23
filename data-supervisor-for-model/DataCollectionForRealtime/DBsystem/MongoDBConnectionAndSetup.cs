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

        //private static string mongoDataCollection;

        static MongoDBConnectionAndSetup()
        {
            _client = new MongoClient(System.Configuration.ConfigurationManager.ConnectionStrings["DefaultMongoConnection"].ConnectionString);

            _database = _client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["MongoDbName"]);

            //mongoDataCollection = System.Configuration.ConfigurationManager.AppSettings["MongoCollection"];

            _contractCollection = _database.GetCollection<Contract>(
                System.Configuration.ConfigurationManager.AppSettings["MongoContractCollection"]);

            _futureBarCollection = _database.GetCollection<OHLCData>(
                System.Configuration.ConfigurationManager.AppSettings["MongoFutureBarCollection"]);
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

        internal static async Task UpdateBardataToMongo(OptionSpreadExpression ose, int indexToUpdate)
        {
            //Mongo_OptionSpreadExpression osefdb = new Mongo_OptionSpreadExpression();

            //osefdb.contract.cqgsymbol = "test";

            //foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            {
                //Mongo_OptionSpreadExpression osefdb = (Mongo_OptionSpreadExpression)ose;


                //Mongo_OptionSpreadExpression mongoOse = (Mongo_OptionSpreadExpression)ose;

                try
                {
                    //Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                    //Mongo_OptionSpreadExpression mongoOse = Mapper.Map<Mongo_OptionSpreadExpression>(ose);


                    //string json = Newtonsoft.Json.JsonConvert.SerializeObject(mongoOse);
                    //Bsondo

                    //MongoDB.Bson.BsonDocument document
                    //    = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

                    //var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
                    //await collection.InsertOneAsync(mongoOse);

                    var builder = Builders<OHLCData>.Filter;

                    var filterForUpdate = builder.And(builder.Eq("idcontract", ose.contract.idcontract),
                            builder.Eq("bartime", ose.futureBarData[indexToUpdate].bartime));

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update..Eq("_id", mongoOse.contract.idcontract);

                    var update = Builders<OHLCData>.Update
                        .Set("open", ose.futureBarData[indexToUpdate].open)
                        .Set("high", ose.futureBarData[indexToUpdate].high)
                        .Set("low", ose.futureBarData[indexToUpdate].low)
                        .Set("close", ose.futureBarData[indexToUpdate].close)
                        .Set("volume", ose.futureBarData[indexToUpdate].volume)
                        .Set("errorbar", ose.futureBarData[indexToUpdate].errorbar);


                    await _futureBarCollection.UpdateOneAsync(filterForUpdate, update);

                }
                catch (Exception e)
                {
                    TSErrorCatch.debugWriteOut(e.ToString());
                }

                //await collection.UpdateOneAsync(filter,Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
                //    new UpdateOptions { IsUpsert = true });

                
            }
        }

        internal static async Task AddDataMongo(OptionSpreadExpression ose, int indexToAdd)
        {
            //Mongo_OptionSpreadExpression osefdb = new Mongo_OptionSpreadExpression();

            //osefdb.contract.cqgsymbol = "test";

            List<OHLCData> barsToAdd = new List<OHLCData>();

            for(;indexToAdd < ose.futureBarData.Count(); indexToAdd++)
            {
                barsToAdd.Add(ose.futureBarData[indexToAdd]);
            }

            
            

            //foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            {
                //Mongo_OptionSpreadExpression osefdb = (Mongo_OptionSpreadExpression)ose;


                //Mongo_OptionSpreadExpression mongoOse = (Mongo_OptionSpreadExpression)ose;

                try
                {
                    //var filter = Builders<OHLCData>.Filter.Eq("idcontract", ose.contract.idcontract);

                    await _futureBarCollection.InsertManyAsync(barsToAdd);

                    //Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                    //Mongo_OptionSpreadExpression mongoOse = Mapper.Map<Mongo_OptionSpreadExpression>(ose);


                    //string json = Newtonsoft.Json.JsonConvert.SerializeObject(mongoOse);
                    //Bsondo

                    //MongoDB.Bson.BsonDocument document
                    //    = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

                    //var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
                    //await collection.InsertOneAsync(mongoOse);

                    //var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Where(x => x._id == mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update.Push("futureBarData", mongoOse.futureBarData[indexToAdd]);

                    //var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Where(x => x._id == mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update..Eq("_id", mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update
                    //    .Set("futureBarData", mongoOse.futureBarData);


                    //await _optionSpreadExpressionCollection.ReplaceOneAsync(filter, mongoOse);

                    //await _optionSpreadExpressionCollection.UpdateOneAsync(filter, update);


                }
                catch (Exception e)
                {
                    TSErrorCatch.debugWriteOut(e.ToString());
                }

                //await collection.UpdateOneAsync(filter,Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
                //    new UpdateOptions { IsUpsert = true });


            }
        }


        internal static Dictionary<long, Contract> getContractListFromMongo()
        {
            List<Contract> mongoContractList = _contractCollection.Find(_ => true).ToList();

            var mongoContractDictionary = mongoContractList.ToDictionary(x => x.idcontract, x => x);

            return mongoContractDictionary;
        }

        internal static void removeExtraContracts(Dictionary<long, Contract> contractListFromMongo)
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
            //else
            //{
            //    //var builder = Builders<OHLCData>.Filter;

            //    //var filterForBars = builder.Eq("idcontract", contract.idcontract) & builder.AnyGte("bartime", contract.previousDateTimeBoundaryStart);

            //    ////await collection.UpdateOneAsync(filter, Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
            //    ////    new UpdateOptions { IsUpsert = true });

            //    //ose.futureBarData = _futureBarCollection.Find(filterForBars).ToList<OHLCData>();

            //}

            var builder = Builders<OHLCData>.Filter;

            var filterForBars = builder.And(builder.Eq("idcontract", contract.idcontract) ,
                    builder.Gte("bartime", contract.previousDateTimeBoundaryStart));

            //_futureBarCollection.DeleteMany(filterForBars);



            ose.futureBarData = _futureBarCollection.Find(filterForBars).ToList<OHLCData>();


            //fill the following 2 variables for normal functioning
            ose.normalSubscriptionRequest = true;

            ose.instrument = instrument;

            return ose;

        }


        internal static async Task createDocument()
        {
            var document = new BsonDocument
            {
                { "address" , new BsonDocument
                    {
                        { "street", "2 Avenue" },
                        { "zipcode", "10075" },
                        { "building", "1480" },
                        { "coord", new BsonArray { 73.9557413, 40.7720266 } }
                    }
                },
                { "borough", "Manhattan" },
                { "cuisine", "Italian" },
                { "grades", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "date", new DateTime(2014, 10, 1, 0, 0, 0, DateTimeKind.Utc) },
                            { "grade", "A" },
                            { "score", 11 }
                        },
                        new BsonDocument
                        {
                            { "date", new DateTime(2014, 1, 6, 0, 0, 0, DateTimeKind.Utc) },
                            { "grade", "B" },
                            { "score", 17 }
                        }
                    }
                },
                { "name", "Vella" },
                { "restaurant_id", "41704620" }
            };

            var collection = _database.GetCollection<BsonDocument>("restaurants");
            await collection.InsertOneAsync(document);
        }

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
