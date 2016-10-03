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
using System.Threading;

namespace DataSupervisorForModel
{
    public partial class RealtimeDataManagement : Form
    {
        private CQGDataManagement cqgDataManagement;

        //MongoDBConnectionAndSetup mongoDBConnectionAndSetup;

        private static System.Timers.Timer aTimer;

        //private string startupException = null;

        public RealtimeDataManagement()
        {
            InitializeComponent();

            AsyncTaskListener.Updated += AsyncTaskListener_Updated;

            AsyncTaskListener.UpdatedStatus += AsyncTaskListener_UpdatedStatus;



            AsyncTaskListener.UpdateExpressionGrid += AsyncTaskListener_ExpressionListUpdate;

            SetupContractSummaryGridList();

            cqgDataManagement = new CQGDataManagement(this);

            //DataCollectionLibrary DataCollectionLibrary = new DataCollectionLibrary();

            //mongoDBConnectionAndSetup = new MongoDBConnectionAndSetup();




            //SetupMongoUpdateTimerThread();


        }

        private void SetupContractSummaryGridList()
        {
            expressionListDataGrid.DataSource = DataCollectionLibrary.contractSummaryGridListDataTable;

            DataCollectionLibrary.contractSummaryGridListDataTable.Columns.Add("Contract");
            DataCollectionLibrary.contractSummaryGridListDataTable.Columns.Add("Last Update Time");

            expressionListDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

        }

        private bool SetupInstrumentAndContractListToCollect()
        {

            DataCollectionLibrary.ResetAndInitializeData();

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

            int row = 0;

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

                    if (ose == null)
                    {
                        //startupException = "Network Error";

                        return false;
                    }

                    ose.row = row++;

                    DataCollectionLibrary.optionSpreadExpressionList.Add(ose);

                    DataCollectionLibrary.optionSpreadExpressionHashTable_keycontractId.Add(contract.idcontract, ose);

                }

            }

            //DataCollectionLibrary.contractSummaryGridList.

            //expressionListDataGrid.RowCount = row;
            //expressionListDataGrid.ColumnCount = 1;

            //expressionListDataGrid.hea.Columns[-1].Width = 100;
            //expressionListDataGrid.Columns[0].Width = 150;

            //MongoDBConnectionAndSetup.removeExtraContracts(contractListFromMongo);


            return true;
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

                AsyncTaskListener.LogMessageAsync("Start listerning");

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

                    AsyncTaskListener.LogMessageAsync(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());

                    string message = "Your query is successfully added";
                    data = Encoding.Unicode.GetBytes(message);
                    handler.Send(data);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
            catch (Exception ex)
            {
                AsyncTaskListener.LogMessageAsync("Error: " + ex.Message);
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

                        //case STATUS_TYPE.DATA_STATUS:
                        //    DataStatus.Text = msg;
                        //    DataStatus.ForeColor = ForeColor;
                        //    DataStatus.BackColor = backColor;
                        //    break;

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
                //Console.Write("test");
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }

            //*******************
        }

        private void AsyncTaskListener_ExpressionListUpdate(
           OptionSpreadExpression ose)
        {
            Action action = new Action(
                () =>
                {
                    //if((ose.row + 1) > expressionListDataGrid.RowCount)
                    //{
                    //    expressionListDataGrid.RowCount = (ose.row + 1);
                    //}

                    if (ose.row + 1 > DataCollectionLibrary.contractSummaryGridListDataTable.Rows.Count)
                    {
                        DataCollectionLibrary.contractSummaryGridListDataTable.Rows.Add();

                    }

                    if (!ose.filledContractDisplayName)
                    {
                        ose.filledContractDisplayName = true;

                        DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][0] =
                            ose.contract.idcontract + " - "
                            + ose.contract.contractname;
                    }

                    if (ose.futureTimedBars != null
                        && ose.futureTimedBars.Count > 0
                        && ose.futureTimedBars[ose.futureTimedBars.Count - 1].Timestamp != null)
                    {
                        DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][1] =
                            ose.futureTimedBars[ose.futureTimedBars.Count - 1].Timestamp;
                    }

                    /*expressionListDataGrid
                        .Rows[ose.row].Cells[1].Value
                        = ose.futureTimedBars[ose.futureTimedBars.Count - 1].Open;

                    expressionListDataGrid
                        .Rows[ose.row].Cells[2].Value
                        = ose.futureTimedBars[ose.futureTimedBars.Count - 1].High;

                    expressionListDataGrid
                        .Rows[ose.row].Cells[3].Value
                        = ose.futureTimedBars[ose.futureTimedBars.Count - 1].Low;

                    expressionListDataGrid
                        .Rows[ose.row].Cells[4].Value
                        = ose.futureTimedBars[ose.futureTimedBars.Count - 1].Close;
                        */



                    //if (!string.IsNullOrWhiteSpace(message))
                    //{
                    //    richTextBoxLog.Text += message + "\n";
                    //    richTextBoxLog.Select(richTextBoxLog.Text.Length, richTextBoxLog.Text.Length);
                    //   richTextBoxLog.ScrollToCaret();
                    //}
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

        private void RealtimeDataManagement_Load(object sender, EventArgs e)
        {
            //SetupInstrumentAndContractListToCollect();

            //if (startupException != null)
            //{
            //    AsyncTaskListener.LogMessageAsync(startupException);
            //}

            //foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            //{
            //    AsyncTaskListener.ExpressionListUpdateAsync(ose);
            //}

            //cqgDataManagement = new CQGDataManagement(this);

            //cqgDataManagement.sendSubscribeRequest(false);
        }

        private void btnCallAllInstruments_Click(object sender, EventArgs e)
        {
            cqgDataManagement.sendSubscribeRequest(false);
        }

        private void btnCQGRecon_Click(object sender, EventArgs e)
        {
            cqgDataManagement.resetCQGConn();

            //cqgDataManagement.sendSubscribeRequest(false);
        }

        private void RealtimeDataManagement_Shown(object sender, EventArgs e)
        {
            StartDataCollectionSystem();
        }

        public void StartDataCollectionSystem()
        {

#if DEBUG
            try
#endif
            {
                Thread dataCollectionSystemThread = new Thread(new ParameterizedThreadStart(RunDataCollectionSystem));
                dataCollectionSystemThread.IsBackground = true;
                dataCollectionSystemThread.Start();

            }
#if DEBUG
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
#endif

        }

        public void RunDataCollectionSystem(Object obj)
        {
            AsyncTaskListener.StatusUpdateAsync(
                "Starting Up...", STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

            while (AsyncTaskListener._InSetupAndConnectionMode.value)
            {

                bool setupCorrectly = SetupInstrumentAndContractListToCollect();

                if (setupCorrectly)
                {
                    foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
                    {
                        //DataCollectionLibrary.contractSummaryGridList.Rows.Add();
                        //DataCollectionLibrary.contractSummaryGridList.Rows[ose.row][1] = ose.contract.idcontract;
                        AsyncTaskListener.ExpressionListUpdateAsync(ose);
                    }

                    cqgDataManagement.initializeCQGAndCallbacks(null);

                    AsyncTaskListener.Set_InSetupAndConnectionMode(false);

                    AsyncTaskListener.StatusUpdateAsync(
                        "Making Call To Data...", STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                    cqgDataManagement.sendSubscribeRequest(false);

                }
                else
                {
                    AsyncTaskListener.LogMessageAsync("Network or Startup Error; Check VPN");
                }
            }
        }


    }
}
