﻿using System;
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
    class MongoDBConnectionAndSetup
    {
        protected static IMongoClient _client;
        protected static IMongoDatabase _database;
        private static string mongoDataCollection;

        static MongoDBConnectionAndSetup()
        {
            _client = new MongoClient(System.Configuration.ConfigurationManager.ConnectionStrings["DefaultMongoConnection"].ConnectionString);

            _database = _client.GetDatabase(System.Configuration.ConfigurationManager.AppSettings["MongoDbName"]);

            mongoDataCollection = System.Configuration.ConfigurationManager.AppSettings["MongoCollection"];
        }

        public IMongoCollection<Mongo_OptionSpreadExpression> MongoDataCollection
        {
            get { return _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection); }
        }

        internal async Task dropCollection()
        {
            await _database.DropCollectionAsync(mongoDataCollection);
        }

        internal async Task createDoc()
        {
            //Mongo_OptionSpreadExpression osefdb = new Mongo_OptionSpreadExpression();

            //osefdb.contract.cqgsymbol = "test";

            foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            {
                //Mongo_OptionSpreadExpression osefdb = (Mongo_OptionSpreadExpression)ose;


                //Mongo_OptionSpreadExpression mongoOse = (Mongo_OptionSpreadExpression)ose;

                Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                Mongo_OptionSpreadExpression mongoOse = Mapper.Map<Mongo_OptionSpreadExpression>(ose);

                //string json = Newtonsoft.Json.JsonConvert.SerializeObject(mongoOse);
                //Bsondo

                //MongoDB.Bson.BsonDocument document
                //    = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);

                var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
                //await collection.InsertOneAsync(mongoOse);

                var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Eq("_id", mongoOse.contract.idcontract);

                await collection.UpdateOneAsync(filter,Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
                    new UpdateOptions { IsUpsert = true });

                //result = collection.Find(filter).SingleOrDefault();

                //using (var cursor = await collection.FindAsync(filter))
                //{
                //    while (await cursor.MoveNextAsync())
                //    {
                //        var batch = cursor.Current;
                //        foreach (var document in batch)
                //        {
                //            // process document
                //            Console.WriteLine(document);



                //            //var x = BsonSerializer.Deserialize<(document);

                //            //var x = MongoDB.Bson.BsonDocument.

                //            //JsonSerializer serializer = new JsonSerializer();

                //            //TestExpression x = serializer.Deserialize<TestExpression>(document);

                //            //Console.WriteLine(document["cqgSymbol"]);

                //            //count++;
                //        }
                //    }
                //}

                return;
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

        internal OptionSpreadExpression GetContractFromMongo(DateTime previousDateCollectionStart, Contract contract, Instrument instrument)
        {

            OptionSpreadExpression ose;

            var collection = _database.GetCollection<Mongo_OptionSpreadExpression>(mongoDataCollection);
            //await collection.InsertOneAsync(mongoOse);

            var filter = Builders<Mongo_OptionSpreadExpression>.Filter.Eq("_id", contract.idcontract);

            //await collection.UpdateOneAsync(filter, Builders<Mongo_OptionSpreadExpression>.Update.Set("futureBarData", mongoOse.futureBarData),
            //    new UpdateOptions { IsUpsert = true });

            Mongo_OptionSpreadExpression mongoOse = collection.Find(filter).SingleOrDefault();
            // Mongo_OptionSpreadExpression mongoOse = collection.find();


            if (mongoOse == null ||
                //mongoOse.previousDateTimeBoundaryStart.CompareTo(previousDateCollectionStart) == 0)
                mongoOse.futureBarData[0].barTime.CompareTo(previousDateCollectionStart) <= 0)
            {
                //replace object in mongodb
                ose = new OptionSpreadExpression();

                ose.normalSubscriptionRequest = true;

                ose._id = contract.idcontract;

                ose.contract = contract;

                ose.instrument = instrument;

                ose.previousDateTimeBoundaryStart = previousDateCollectionStart;

                Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                Mongo_OptionSpreadExpression mongoOseToReplace = Mapper.Map<Mongo_OptionSpreadExpression>(ose);

                //put replace code here

                collection.FindOneAndReplace(filter, mongoOseToReplace);
            }
            else
            {
                Mapper.Initialize(cfg => cfg.CreateMap<OptionSpreadExpression, Mongo_OptionSpreadExpression>());

                ose = Mapper.Map<OptionSpreadExpression>(mongoOse);
            }

            return ose;

        }


        internal async Task createDocument()
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

        internal async Task getDocument()
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
