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
        private static IMongoCollection<Mongo_OptionSpreadExpression> _optionSpreadExpressionCollection;
        //private static string mongoDataCollection;

        static MongoDBConnectionAndSetup()
        {
            _client = new MongoClient(System.Configuration.ConfigurationManager.ConnectionStrings["DefaultMongoConnection"].ConnectionString);

            _database = _client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["MongoDbName"]);

            //mongoDataCollection = System.Configuration.ConfigurationManager.AppSettings["MongoCollection"];

            _optionSpreadExpressionCollection = _database.GetCollection<Mongo_OptionSpreadExpression>(
                System.Configuration.ConfigurationManager.AppSettings["MongoCollection"]);
        }

        public static IMongoCollection<Mongo_OptionSpreadExpression> MongoDataCollection
        {
            get { return _database.GetCollection<Mongo_OptionSpreadExpression>(
                System.Configuration.ConfigurationManager.AppSettings["MongoCollection"]); }
        }

        internal static async Task dropCollection()
        {
            await _database.DropCollectionAsync(
                System.Configuration.ConfigurationManager.AppSettings["MongoCollection"]);
        }

        internal static async Task UpdateBardataToMongo()
        {
            //Mongo_OptionSpreadExpression osefdb = new Mongo_OptionSpreadExpression();

            //osefdb.contract.cqgsymbol = "test";

            foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            {
                //Mongo_OptionSpreadExpression osefdb = (Mongo_OptionSpreadExpression)ose;


                //Mongo_OptionSpreadExpression mongoOse = (Mongo_OptionSpreadExpression)ose;

                try
                {
                    Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                    Mongo_OptionSpreadExpression mongoOse = Mapper.Map<Mongo_OptionSpreadExpression>(ose);
                

                //string json = Newtonsoft.Json.JsonConvert.SerializeObject(mongoOse);
                //Bsondo

                //MongoDB.Bson.BsonDocument document
                //    = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

                //var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
                //await collection.InsertOneAsync(mongoOse);

                    var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Where(x => x._id == mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update..Eq("_id", mongoOse.contract.idcontract);

                    var update = Builders<Mongo_OptionSpreadExpression>.Update
                        .Set("futureBarData", ose.futureBarData);


                    await _optionSpreadExpressionCollection.UpdateOneAsync(filter, update);

                }
                catch (Exception e)
                {
                    TSErrorCatch.debugWriteOut(e.ToString());
                }

                //await collection.UpdateOneAsync(filter,Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
                //    new UpdateOptions { IsUpsert = true });

                
            }
        }

        internal static async Task AddDataMongo(Mongo_OptionSpreadExpression ose, int indexToAdd)
        {
            //Mongo_OptionSpreadExpression osefdb = new Mongo_OptionSpreadExpression();

            //osefdb.contract.cqgsymbol = "test";

            //foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            {
                //Mongo_OptionSpreadExpression osefdb = (Mongo_OptionSpreadExpression)ose;


                //Mongo_OptionSpreadExpression mongoOse = (Mongo_OptionSpreadExpression)ose;

                try
                {
                    Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                    Mongo_OptionSpreadExpression mongoOse = Mapper.Map<Mongo_OptionSpreadExpression>(ose);


                    //string json = Newtonsoft.Json.JsonConvert.SerializeObject(mongoOse);
                    //Bsondo

                    //MongoDB.Bson.BsonDocument document
                    //    = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

                    //var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
                    //await collection.InsertOneAsync(mongoOse);

                    //var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Where(x => x._id == mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update.Push("futureBarData", mongoOse.futureBarData[indexToAdd]);

                    var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Where(x => x._id == mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update..Eq("_id", mongoOse.contract.idcontract);

                    //var update = Builders<Mongo_OptionSpreadExpression>.Update
                    //    .Set("futureBarData", mongoOse.futureBarData);


                    await _optionSpreadExpressionCollection.ReplaceOneAsync(filter, mongoOse);

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


        /// <summary>
        /// This gets the contract from the mongodb
        /// it will run sychronously
        /// </summary>
        /// <param name="previousDateCollectionStart"></param>
        /// <param name="contract"></param>
        /// <param name="instrument"></param>
        /// <returns>OptionSpreadExpression</returns>

        internal static OptionSpreadExpression GetContractFromMongo(DateTime previousDateCollectionStart, Contract contract, Instrument instrument)
        {

            OptionSpreadExpression ose;

            //var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
            //await collection.InsertOneAsync(mongoOse);

            var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Eq("_id", contract.idcontract);

            //await collection.UpdateOneAsync(filter, Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
            //    new UpdateOptions { IsUpsert = true });

            Mongo_OptionSpreadExpression mongoOse = _optionSpreadExpressionCollection.Find(filter).SingleOrDefault();
            // Mongo_OptionSpreadExpression mongoOse = collection.find();


            if (mongoOse == null ||
                mongoOse.previousDateTimeBoundaryStart.CompareTo(previousDateCollectionStart) != 0)
                //mongoOse.futureBarData[0].barTime.CompareTo(previousDateCollectionStart) <= 0)
            {
                //replace object in mongodb
                ose = new OptionSpreadExpression();

                ose._id = contract.idcontract;

                ose.contract = contract;

                ose.previousDateTimeBoundaryStart = previousDateCollectionStart;

                ose.futureBarData = new List<OHLCData>();

                Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                Mongo_OptionSpreadExpression mongoOseToReplace = Mapper.Map<Mongo_OptionSpreadExpression>(ose);

                //put replace code here

                _optionSpreadExpressionCollection.ReplaceOne(filter, mongoOseToReplace,
                    new UpdateOptions { IsUpsert = true });
            }
            else
            {
                Mapper.Initialize(cfg => cfg.CreateMap<Mongo_OptionSpreadExpression, OptionSpreadExpression>());

                ose = Mapper.Map<OptionSpreadExpression>(mongoOse);               
            }

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
