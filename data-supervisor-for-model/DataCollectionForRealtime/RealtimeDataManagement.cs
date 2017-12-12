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
using System.Collections.Concurrent;
using log4net;

namespace DataSupervisorForModel
{
    public partial class RealtimeDataManagement : Form
    {
        private CQGDataManagement cqgDataManagement;

        //MongoDBConnectionAndSetup mongoDBConnectionAndSetup;

        //private static System.Timers.Timer aTimer;
        private BackgroundWorker backgroundWorkerThread;

        //private DataCollectionLibrary dataCollectionLibrary;

        //private string startupException = null;

        public RealtimeDataManagement()
        {
            InitializeComponent();

            AsyncTaskListener.Updated += AsyncTaskListener_Updated;

            AsyncTaskListener.UpdatedStatus += AsyncTaskListener_UpdatedStatus;



            AsyncTaskListener.UpdateExpressionGrid += AsyncTaskListener_ExpressionListUpdate;

            SetupContractSummaryGridList();



            cqgDataManagement = new CQGDataManagement(this);


            SetupMongoUpdateTimerThread();


        }

        private void SetupContractSummaryGridList()
        {
            //dataCollectionLibrary = new DataCollectionLibrary();

            expressionListDataGrid.DataSource = DataCollectionLibrary.contractSummaryGridListDataTable;

            DataCollectionLibrary.contractSummaryGridListDataTable.Columns.Add("Contract");
            DataCollectionLibrary.contractSummaryGridListDataTable.Columns.Add("Last Update Time");
            //DataCollectionLibrary.contractSummaryGridListDataTable.Columns.Add("Subscribed");

            expressionListDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

        }

        private bool SetupInstrumentAndContractListToCollect()
        {

            DataCollectionLibrary.ResetAndInitializeData();

            //MongoDBConnectionAndSetup mdbcas = new MongoDBConnectionAndSetup();

            DataCollectionLibrary.instrumentList
                = MongoDBConnectionAndSetup.GetInstrumentInfoListFromMongo();

            DataCollectionLibrary.instrumentHashTable
                = DataCollectionLibrary.instrumentList.ToDictionary(x => x.idinstrument, x => x);


            DataCollectionLibrary.contractHashTableByInstId
                = MongoDBConnectionAndSetup.GetContractListFromMongo_tmldb_v2(
                    DataCollectionLibrary.instrumentList, DateTime.Today);



            int row = 0;

            foreach (KeyValuePair<long, List<Contract>> contractHashEntry in DataCollectionLibrary.contractHashTableByInstId)
            {

                foreach (Contract contract in contractHashEntry.Value)
                {
                    // if (contractListFromMongo.ContainsKey(contract.idcontract))
                    // {
                    //     contractListFromMongo.Remove(contract.idcontract);
                    // }

                    Instrument_mongo instrument = DataCollectionLibrary.instrumentHashTable[contract.idinstrument];

                    DateTime previousDateCollectionStart
                        = MongoDBConnectionAndSetup.GetContractPreviousDateTimeFromMongo(contract.idcontract)
                            .AddHours(
                                instrument.customdayboundarytime.Hour)
                            .AddMinutes(
                                instrument.customdayboundarytime.Minute)
                            .AddMinutes(1);

                    //DateTime previousDateCollectionStart = TMLDBReader.GetContractPreviousDateTime(contract.idcontract)
                    //    .AddHours(
                    //        instrument.customdayboundarytime.Hour)
                    //    .AddMinutes(
                    //        instrument.customdayboundarytime.Minute)
                    //    .AddMinutes(1);

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



            return true;
        }

        private void SetupMongoUpdateTimerThread()
        {
            try
            {
                //aTimer = new System.Timers.Timer();
                //aTimer.Interval = 5000;

                //// Alternate method: create a Timer with an interval argument to the constructor.
                ////aTimer = new System.Timers.Timer(2000);

                //// Create a timer with a two second interval.
                ////aTimer = new System.Timers.Timer(30000);

                //// Hook up the Elapsed event for the timer. 
                //aTimer.Elapsed += OnTimedEvent;

                //// Have the timer fire repeated events (true is the default)
                //aTimer.AutoReset = true;

                //// Start the timer
                //aTimer.Enabled = true;


                backgroundWorkerThread = new BackgroundWorker();
                backgroundWorkerThread.WorkerReportsProgress = true;
                backgroundWorkerThread.WorkerSupportsCancellation = true;

                backgroundWorkerThread.DoWork +=
                new DoWorkEventHandler(setupBackgroundWorkerLoop);

                backgroundWorkerThread.ProgressChanged +=
                    new ProgressChangedEventHandler(
                        backgroundWorkerProgressChanged);

                backgroundWorkerThread.RunWorkerAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }

        int dateResetIndex = 0;

        private void setupBackgroundWorkerLoop(object sender,
            DoWorkEventArgs e)
        {

            //ThreadTracker.openThread(null, null);

            List<TimeSpan> dataResetTime = new List<TimeSpan>();
            dataResetTime.Add(new TimeSpan(7, 10, 0));
            dataResetTime.Add(new TimeSpan(8, 30, 0));
            dataResetTime.Add(new TimeSpan(10, 30, 0));

            //bool previousLoopWasBeforeStartTime = false;

            while (true)  //continueCheckingStatus
            {
                System.Threading.Thread.Sleep(600000);
                //System.Threading.Thread.Sleep(5);
                //+ DataCollectionLibrary.optionSpreadExpressionList.Count 
                //* DataCollectionConstants.SUBSCRIPTION_TIMEDELAY_CONSTANT);

                bool resetConnection = false;

                DateTime current = DateTime.Now;

                if (current.DayOfWeek != DayOfWeek.Saturday
                    && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    if (dataResetTime[0] - current.TimeOfDay > TimeSpan.Zero)
                    {
                        dateResetIndex = 0;
                    }
                    //TimeSpan timeToGo = dataResetTime[0] - current.TimeOfDay;
                    while (dateResetIndex < dataResetTime.Count
                        && dataResetTime[dateResetIndex] - current.TimeOfDay < TimeSpan.Zero)
                    {
                        resetConnection = true;

                        dateResetIndex++;
                    }
                }

                if (resetConnection && dateResetIndex < dataResetTime.Count)
                {
                    backgroundWorkerThread.ReportProgress(0, true);
                }

                //if (DateTime.Now.Hour >= 5 && DateTime.Now.Hour < 15
                //    && DateTime.Now.DayOfWeek != DayOfWeek.Saturday
                //    && DateTime.Now.DayOfWeek != DayOfWeek.Sunday)
                //{
                //    backgroundWorkerThread.ReportProgress(0, previousLoopWasBeforeStartTime);

                //    previousLoopWasBeforeStartTime = false;
                //}
                //else
                //{
                //    previousLoopWasBeforeStartTime = true;
                //}
            }


            //ThreadTracker.closeThread(null, null);

        }

        delegate void ThreadSafeGenericDelegateWithParams(Object obj);

        //bool runOnce = true;

        private void backgroundWorkerProgressChanged(object sender,
            ProgressChangedEventArgs argsObj)
        {
            try
            {
                //this code is used to test the problem with upserting to mongodb
                //if (true)
                //{
                //    if (runOnce)
                //    {
                //        runOnce = false;
                //        cqgDataManagement.AddTimedBarsTest();
                //    }
                //}
                //else
                {

                    bool connectionRefreshUpdate = (bool)argsObj.UserState;

                    //Console.WriteLine("The Elapsed event was raised at {0}", DateTime.Now);
                    //if (!AsyncTaskListener._InSetupAndConnectionMode.setup_mode_value)
                    //{
                    if (connectionRefreshUpdate)
                    {
                        MongoDBConnectionAndSetup.MongoFailureMethod("CQG Timer Restart");
                    }
                    else if (cqgDataManagement.check_cqg_status_fail())
                    {
                        MongoDBConnectionAndSetup.MongoFailureMethod("ERROR: CQG Failure");
                    }
                    else
                    {

                        int staleCount = 0;

                        ConcurrentQueue<OptionSpreadExpression> queueOfOSE = new ConcurrentQueue<OptionSpreadExpression>();

                        foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
                        {
                            var testMinutes = (DateTime.Now -
                                        ose.lastTimeFuturePriceUpdated).TotalMinutes;

                            if (testMinutes > 15)
                            {
                                ose.staleData = STALE_DATA_INDICATORS.VERY_STALE;

                                staleCount++;
                            }
                            else if (testMinutes > 5)
                            {
                                ose.staleData = STALE_DATA_INDICATORS.MILDLY_STALE;
                            }
                            else
                            {
                                ose.staleData = STALE_DATA_INDICATORS.UP_TO_DATE;
                            }

                            if (testMinutes > 30)
                            {
                                queueOfOSE.Enqueue(ose);

                            }
                        }

                        if (queueOfOSE.Count > 0)
                        {
                            //lock (AsyncTaskListener._InSetupAndConnectionMode)
                            //{
                            //    if (!AsyncTaskListener._InSetupAndConnectionMode.subscription_mode_value)
                            //    {
                            //AsyncTaskListener.Set_subscription_mode_value(true);

                            //ThreadSafeGenericDelegateWithParams d = new ThreadSafeGenericDelegateWithParams(cqgDataManagement.sendSubscribeRequestRun);

                            //this.Invoke(d, queueOfOSE);

                            cqgDataManagement.sendSubscribeRequest(queueOfOSE);


                            //        AsyncTaskListener.Set_subscription_mode_value(false);
                            //    }
                            //}
                        }
                    }
                    //}
                }

            }
            catch (Exception ex)
            {
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        //private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        //{
        //    Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);

        //    //if (DataCollectionLibrary.optionSpreadExpressionList.Count > 0)
        //    {

        //        int staleCount = 0;

        //        ConcurrentQueue<OptionSpreadExpression> queueOfOSE = new ConcurrentQueue<OptionSpreadExpression>();

        //        foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
        //        {
        //            var testMinutes = (DateTime.Now -
        //                        ose.lastTimeFuturePriceUpdated).TotalMinutes;

        //            if (testMinutes > 15)
        //            {
        //                //DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
        //                // Convert.ToInt16(STALE_DATA_INDICATORS.VERY_STALE);

        //                ose.staleData = STALE_DATA_INDICATORS.VERY_STALE;

        //                staleCount++;
        //            }
        //            else if (testMinutes > 5)
        //            {
        //                //DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
        //                // Convert.ToInt16(STALE_DATA_INDICATORS.MILDLY_STALE);

        //                ose.staleData = STALE_DATA_INDICATORS.MILDLY_STALE;
        //            }
        //            else
        //            {
        //                //DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
        //                // Convert.ToInt16(STALE_DATA_INDICATORS.UP_TO_DATE);

        //                ose.staleData = STALE_DATA_INDICATORS.UP_TO_DATE;
        //            }

        //            if(testMinutes > 1440)
        //            {
        //                queueOfOSE.Enqueue(ose);

        //            }
        //        }

        //        if(queueOfOSE.Count > 0)
        //        {
        //            lock (AsyncTaskListener._InSetupAndConnectionMode)
        //            {
        //                if (!AsyncTaskListener._InSetupAndConnectionMode.subscription_mode_value)
        //                {
        //                    AsyncTaskListener.Set_subscription_mode_value(true);

        //                    cqgDataManagement.sendSubscribeRequest(queueOfOSE);

        //                    AsyncTaskListener.Set_subscription_mode_value(false);
        //                }
        //            }
        //        }

        //        if(staleCount >= DataCollectionLibrary.optionSpreadExpressionList.Count - 5)
        //        {
        //            //MongoDBConnectionAndSetup.MongoFailureMethod("CQG Data Stale");
        //        }


        //    }

        //}

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
                    try
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
                            //if ((DateTime.Now.TimeOfDay -
                            //    ose.lastTimeFuturePriceUpdated.TimeOfDay).TotalMinutes > 10)
                            //{
                            //    DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
                            //     Convert.ToInt16(STALE_DATA_INDICATORS.VERY_STALE);

                            //    ose.staleData = STALE_DATA_INDICATORS.VERY_STALE;
                            //}
                            //else if ((DateTime.Now.TimeOfDay -
                            //    ose.lastTimeFuturePriceUpdated.TimeOfDay).TotalMinutes > 5)
                            //{
                            //    DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
                            //     Convert.ToInt16(STALE_DATA_INDICATORS.MILDLY_STALE);

                            //    ose.staleData = STALE_DATA_INDICATORS.MILDLY_STALE;
                            //}
                            //else
                            //{
                            //    DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
                            //     Convert.ToInt16(STALE_DATA_INDICATORS.UP_TO_DATE);

                            //    ose.staleData = STALE_DATA_INDICATORS.UP_TO_DATE;
                            //}

                            //DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] = ose.alreadyRequestedMinuteBars;

                            DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][1] =
                                ose.lastTimeFuturePriceUpdated;
                        }

                        //else
                        //{
                        //    DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] = false;
                        //}


                        //else
                        //{
                        //    DataCollectionLibrary.contractSummaryGridListDataTable.Rows[ose.row][2] =
                        //         Convert.ToInt16(STALE_DATA_INDICATORS.VERY_STALE);

                        //    ose.staleData = STALE_DATA_INDICATORS.VERY_STALE;
                        //}

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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
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

        private void RealtimeDataManagement_Load(object sender, EventArgs e)
        {

        }

        private void btnCallAllInstruments_Click(object sender, EventArgs e)
        {
            StartDataCollectionSystem(false);
            //if (!AsyncTaskListener._InSetupAndConnectionMode.setup_mode_value)
            //{

            //    ConcurrentQueue<OptionSpreadExpression> queueOfOSE = new ConcurrentQueue<OptionSpreadExpression>();

            //    foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            //    {
            //        queueOfOSE.Enqueue(ose);
            //    }

            //    if (queueOfOSE.Count > 0)
            //    {
            //        lock (AsyncTaskListener._InSetupAndConnectionMode)
            //        {
            //            if (!AsyncTaskListener._InSetupAndConnectionMode.subscription_mode_value)
            //            {
            //                AsyncTaskListener.Set_subscription_mode_value(true);

            //                //ThreadSafeGenericDelegateWithParams d = new ThreadSafeGenericDelegateWithParams(cqgDataManagement.sendSubscribeRequestRun);

            //                //this.Invoke(d, queueOfOSE);

            //                cqgDataManagement.sendSubscribeRequest(queueOfOSE);


            //                AsyncTaskListener.Set_subscription_mode_value(false);
            //            }
            //        }
            //    }
            //}
        }

        private void btnCQGRecon_Click(object sender, EventArgs e)
        {
            cqgDataManagement.resetCQGConn();
        }

        private void RealtimeDataManagement_Shown(object sender, EventArgs e)
        {
            //cqgDataManagement.initializeCQGAndCallbacks(null);

            StartDataCollectionSystem(true);
        }

        public void StartDataCollectionSystem(bool initialize = false)
        {

#if DEBUG
            try
#endif
            {
                Thread dataCollectionSystemThread = new Thread(new ParameterizedThreadStart(RunDataCollectionSystem));
                dataCollectionSystemThread.IsBackground = true;
                dataCollectionSystemThread.Start(initialize);

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
            try
            {
                bool initialize = (bool)obj;

                AsyncTaskListener.StatusUpdateAsync(
                    "Starting Up...", STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                AsyncTaskListener.LogMessageAsync("Data collection will start momentarily...");

                lock (AsyncTaskListener._InSetupAndConnectionMode)
                {
                    if (!AsyncTaskListener._InSetupAndConnectionMode.setup_mode_value)
                    {
                        AsyncTaskListener.Set_setup_connection_mode_value(true);

                        AsyncTaskListener.LogMessageAsync("Data collection started will continue after resetting contracts to collect...");

                        bool setupCorrectly = SetupInstrumentAndContractListToCollect();

                        int setupCount = 1;

                        AsyncTaskListener.LogMessageAsync($"Data collection Setting Up {setupCount++}");

                        if (setupCorrectly)
                        {
                            foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
                            {
                                AsyncTaskListener.ExpressionListUpdateAsync(ose);
                            }

                            AsyncTaskListener.LogMessageAsync($"Data collection Setting Up {setupCount++}");

                            if (initialize)
                            {
                                cqgDataManagement.initializeCQGAndCallbacks(null);
                            }

                            AsyncTaskListener.LogMessageAsync($"Data collection Setting Up {setupCount++}");

                            //AsyncTaskListener.Set_setup_connection_mode_value(false);

                            AsyncTaskListener.LogMessageAsync($"Data collection Setting Up {setupCount++}");

                            //AsyncTaskListener.StatusUpdateAsync(
                            //    "Making Call To Data...", STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                            ConcurrentQueue<OptionSpreadExpression> queueOfOSE = new ConcurrentQueue<OptionSpreadExpression>();

                            foreach (OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
                            {
                                queueOfOSE.Enqueue(ose);
                            }

                            AsyncTaskListener.LogMessageAsync($"Data collection Setting Up {setupCount++}");

                            if (queueOfOSE.Count > 0)
                            {
                                //lock (AsyncTaskListener._InSetupAndConnectionMode)
                                //{
                                //    if (!AsyncTaskListener._InSetupAndConnectionMode.subscription_mode_value)
                                //    {
                                AsyncTaskListener.LogMessageAsync($"Data collection Setting Up {setupCount++}");

                                //AsyncTaskListener.Set_subscription_mode_value(true);


                                cqgDataManagement.sendSubscribeRequest(queueOfOSE);


                                //        AsyncTaskListener.Set_subscription_mode_value(false);
                                //    }
                                //}
                            }

                        }
                        else
                        {
                            AsyncTaskListener.LogMessageAsync("Network or Startup Error; Check VPN");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
            finally
            {
                AsyncTaskListener.Set_setup_connection_mode_value(false);
            }
        }

        private void expressionListDataGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //try
            //{
            //    if (!Convert.IsDBNull(dataCollectionLibrary.contractSummaryGridListDataTable.Rows[e.RowIndex][2])
            //        && e.ColumnIndex == 2)
            //    {
            //        if (Convert.ToInt16(dataCollectionLibrary.contractSummaryGridListDataTable.Rows[e.RowIndex][2])
            //            == Convert.ToInt16(STALE_DATA_INDICATORS.UP_TO_DATE))
            //        {
            //            e.CellStyle.BackColor = Color.Green;
            //        }
            //        else if (Convert.ToInt16(dataCollectionLibrary.contractSummaryGridListDataTable.Rows[e.RowIndex][2])
            //            == Convert.ToInt16(STALE_DATA_INDICATORS.MILDLY_STALE))
            //        {
            //            e.CellStyle.BackColor = Color.Yellow;
            //        }
            //        else
            //        {
            //            e.CellStyle.BackColor = Color.Red;
            //        }
            //    }
            //}
            //catch (Exception ee)
            //{
            //    Console.WriteLine(ee.ToString());
            //}
        }
    }
}
