using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Net;
using System.Net.Sockets;

namespace DataSupervisorForModel
{
    public partial class RealtimeDataManagement : Form
    {
        private CQGDataManagement cqgDataManagement;

        //MongoDBConnectionAndSetup mongoDBConnectionAndSetup;

        private static System.Timers.Timer aTimer;

        public RealtimeDataManagement()
        {
            InitializeComponent();

            AsyncTaskListener.Updated += AsyncTaskListener_Updated;

            AsyncTaskListener.UpdatedStatus += AsyncTaskListener_UpdatedStatus;


            //DataCollectionLibrary DataCollectionLibrary = new DataCollectionLibrary();

            //mongoDBConnectionAndSetup = new MongoDBConnectionAndSetup();


            SetupInstrumentAndContractListToCollect();

            //SetupMongoUpdateTimerThread();

           
        }

        private void SetupInstrumentAndContractListToCollect()
        {

            //MongoDBConnectionAndSetup mongoDBConnectionAndSetup = new MongoDBConnectionAndSetup();
            //mongoDBConnectionAndSetup.connectToMongoDB();
            //mongoDBConnectionAndSetup.createDocument();
            //mongoDBConnectionAndSetup.dropCollection();

            var contextTMLDB = new DataClassesTMLDBDataContext(
                System.Configuration.ConfigurationManager.ConnectionStrings["TMLDBConnectionString"].ConnectionString);

            TMLDBReader TMLDBReader = new TMLDBReader(contextTMLDB);



            bool gotInstrumentList = TMLDBReader.GetTblInstruments(ref DataCollectionLibrary.instrumentHashTable,
                    ref DataCollectionLibrary.instrumentList);

            bool gotContractList = TMLDBReader.GetContracts(ref DataCollectionLibrary.instrumentList,
                ref DataCollectionLibrary.contractHashTableByInstId, DateTime.Today);


            //Dictionary<long, Contract> contractListFromMongo = MongoDBConnectionAndSetup.getContractListFromMongo();

            foreach (KeyValuePair<long, List<Contract>> contractHashEntry in DataCollectionLibrary.contractHashTableByInstId)
            {

                foreach (Contract contract in contractHashEntry.Value)
                {
                   // if (contractListFromMongo.ContainsKey(contract.idcontract))
                   // {
                   //     contractListFromMongo.Remove(contract.idcontract);
                   // }

                    Instrument instrument = DataCollectionLibrary.instrumentHashTable[contract.idinstrument];

                    DateTime previousDateCollectionStart = TMLDBReader.GetContractPreviousDateTime(contract.idcontract)
                        .AddHours(
                            instrument.customdayboundarytime.Hour)
                        .AddMinutes(
                            instrument.customdayboundarytime.Minute)
                        .AddMinutes(1);

                    contract.previousDateTimeBoundaryStart = previousDateCollectionStart;

                    //now get the ose from mongo and see if it has the correct data in the future bar data field
                    OptionSpreadExpression ose = 
                        MongoDBConnectionAndSetup.GetContractFromMongo(contract, instrument);


                    DataCollectionLibrary.optionSpreadExpressionList.Add(ose);

                    DataCollectionLibrary.optionSpreadExpressionHashTable_keycontractId.Add(contract.idcontract, ose);

                    //break;
                }

                //break;
            }

            //MongoDBConnectionAndSetup.removeExtraContracts(contractListFromMongo);



        }

        private void SetupMongoUpdateTimerThread()
        {
            aTimer = new System.Timers.Timer();
            aTimer.Interval = 2000;

            // Alternate method: create a Timer with an interval argument to the constructor.
            //aTimer = new System.Timers.Timer(2000);

            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(30000);

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;

            // Have the timer fire repeated events (true is the default)
            aTimer.AutoReset = true;

            // Start the timer
            aTimer.Enabled = true;

        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);

            //Task t = MongoDBConnectionAndSetup.UpdateBardataToMongo();
        }

        //private void SetupExpressionList()
        //{
        //    //KeyValuePair<long,List<Contract>>
        //    foreach (KeyValuePair<long, List<Contract>> contractHashEntry in cqgDataManagement.contractHashTableByInstId)
        //    {
        //        foreach(Contract contract in contractHashEntry.Value)
        //        {
        //            OptionSpreadExpression ose = new OptionSpreadExpression();

        //            ose.contract = contract;
        //            ose.instrument = cqgDataManagement.instrumentHashTable[contract.idinstrument];

        //            cqgDataManagement.optionSpreadExpressionList.Add(ose);
        //        }
        //    }
        //    //cqgDataManagement.optionSpreadExpressionList
        //}

        //private void testLoadIn()
        //{
        //    //MongoDBConnectionAndSetup mongoDBConnectionAndSetup = new MongoDBConnectionAndSetup();

        //    Mongo_OptionSpreadExpression osefdb = new Mongo_OptionSpreadExpression();


        //    osefdb.contract.cqgsymbol = "F.EPU16";
        //    //osefdb.instrument = DataCollectionLibrary.instrumentHashTable[11];

        //    //mongoDBConnectionAndSetup.MongoDataCollection.ReplaceOne(
        //    //    item => item.cqgSymbol == osefdb.cqgSymbol,
        //    //    osefdb,
        //    //    new UpdateOptions { IsUpsert = true });

        //    MongoDBConnectionAndSetup.MongoDataCollection.InsertOne(osefdb);


        //}

        //private void testGetData()
        //{
        //    //MongoDBConnectionAndSetup mongoDBConnectionAndSetup = new MongoDBConnectionAndSetup();

        //    var filterBuilder = Builders<Mongo_OptionSpreadExpression>.Filter;
        //    var filter = filterBuilder.Ne("Id", "barf");

        //    var testExpression = MongoDBConnectionAndSetup.MongoDataCollection.Find(filter).SingleOrDefault();

        //    Console.WriteLine(testExpression.contract.cqgsymbol);


        //}

        private void btnCallAllInstruments_Click(object sender, EventArgs e)
        {
            cqgDataManagement.sendSubscribeRequest(false);
        }


        void StartListerning()
        {
            int port = 8005;
            string address = "127.0.0.1";

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), port);

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listenSocket.Bind(ipPoint);

                listenSocket.Listen(10);

                AsyncTaskListener.LogMessage("Start listerning");

                while (true)
                {
                    Socket handler = listenSocket.Accept();
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    byte[] data = new byte[256];

                    do
                    {
                        bytes = handler.Receive(data);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (handler.Available > 0);

                    AsyncTaskListener.LogMessage(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());

                    string message = "Your query is successfully added";
                    data = Encoding.Unicode.GetBytes(message);
                    handler.Send(data);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                AsyncTaskListener.LogMessage("Error: " + ex.Message);
            }
        }

        private void AsyncTaskListener_Updated(
            string message = null,
            int progress = -1,
            double rps = double.NaN)
        {
            Action action = new Action(
                () =>
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        richTextBoxLog.Text += message + "\n";
                        richTextBoxLog.Select(richTextBoxLog.Text.Length, richTextBoxLog.Text.Length);
                        richTextBoxLog.ScrollToCaret();
                    }
                });

            try
            {
                Invoke(action);
            }
            catch (ObjectDisposedException)
            {
                // User closed the form
            }
        }


        private void AsyncTaskListener_UpdatedStatus(
            string msg = null,
            STATUS_FORMAT statusFormat = STATUS_FORMAT.DEFAULT,
            STATUS_TYPE connStatus = STATUS_TYPE.NO_STATUS)
        {
            //*******************
            Action action = new Action(
                () =>
                {

                    Color foreColor = Color.Black;
                    Color backColor = Color.LightGreen;

                    switch (statusFormat)
                    {
                        case STATUS_FORMAT.CAUTION:
                            foreColor = Color.Black;
                            backColor = Color.Yellow;
                            break;

                        case STATUS_FORMAT.ALARM:
                            foreColor = Color.Black;
                            backColor = Color.Red;
                            break;

                    }

                    switch (connStatus)
                    {
                        case STATUS_TYPE.CQG_CONNECTION_STATUS:
                            ConnectionStatus.Text = msg;
                            ConnectionStatus.ForeColor = ForeColor;
                            ConnectionStatus.BackColor = backColor;
                            break;

                        case STATUS_TYPE.DATA_STATUS:
                            DataStatus.Text = msg;
                            DataStatus.ForeColor = ForeColor;
                            DataStatus.BackColor = backColor;
                            break;

                        case STATUS_TYPE.DATA_SUBSCRIPTION_STATUS:
                            StatusSubscribeData.Text = msg;
                            StatusSubscribeData.ForeColor = ForeColor;
                            StatusSubscribeData.BackColor = backColor;
                            break;
                    }

                });

            try
            {
                Invoke(action);
            }
            catch (Exception ex)
            //catch (ObjectDisposedException)
            {
                // User closed the form
                Console.Write("test");
            }

            //*******************
        }

        private void RealtimeDataManagement_Load(object sender, EventArgs e)
        {
            cqgDataManagement = new CQGDataManagement(this);
        }

        private void btnCQGRecon_Click(object sender, EventArgs e)
        {
            //Task t = MongoDBConnectionAndSetup.UpdateBardataToMongo();

            //Console.WriteLine(cqgDataManagement.instrumentList[0].description);



            //AsyncTaskListener.LogMessage("test");

            //testLoadIn();

            //testGetData();

            //mongoDBConnectionAndSetup.createDoc();
            //mongoDBConnectionAndSetup.getDocument();
        }
    }
}
