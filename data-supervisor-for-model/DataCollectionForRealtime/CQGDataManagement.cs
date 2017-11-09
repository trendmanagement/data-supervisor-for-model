using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQG;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace DataSupervisorForModel
{
    class CQGDataManagement
    {
        //private MongoDBConnectionAndSetup mdbcas;
        //private DataCollectionLibrary DataCollectionLibrary;

        internal CQGDataManagement(RealtimeDataManagement realtimeDataManagement) //, DataCollectionLibrary DataCollectionLibrary)
        {
            AsyncTaskListener.UpdateCQGDataManagement += AsyncTaskListener_UpdateCQGDataManagement;

            this.realtimeDataManagement = realtimeDataManagement;
            //this.DataCollectionLibrary = DataCollectionLibrary;

            //mdbcas = new MongoDBConnectionAndSetup();



            //ThreadPool.QueueUserWorkItem(new WaitCallback(initializeCQGAndCallbacks));

            //AsyncTaskListener.LogMessage("test");
        }

        private Thread subscriptionThread;
        private bool continueSubscriptionRequest = true;
        

        private RealtimeDataManagement realtimeDataManagement;

        private CQG.CQGCEL m_CEL;

        internal void AsyncTaskListener_UpdateCQGDataManagement()
        {
            //resetCQGConn();

            realtimeDataManagement.StartDataCollectionSystem(false);
        }

        internal void connectCQG()
        {
            if (m_CEL != null)
            {
                m_CEL.Startup();
            }
        }

        internal void shutDownCQGConn()
        {
            continueSubscriptionRequest = false;

            if (m_CEL != null)
            {
                if (m_CEL.IsStarted)
                {
                    m_CEL.RemoveAllInstruments();
                }

                //m_CEL.Shutdown();
            }
        }

        internal void resetCQGConn()
        {
            shutDownCQGConn();

            if (m_CEL != null)
            {
                if (!m_CEL.IsStarted)
                {
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(initializeCQGAndCallbacks));
                    initializeCQGAndCallbacks(null);  
                }
            }

            //while (continueSubscriptionRequest && i < DataCollectionLibrary.optionSpreadExpressionList.Count)

            //foreach(OptionSpreadExpression ose in DataCollectionLibrary.optionSpreadExpressionList)
            //{
            //    ose.alreadyRequestedMinuteBars = false;

            //    //ose.setSubscriptionLevel = false;
            //}

            //continueSubscriptionRequest = false;

        }

        internal void initializeCQGAndCallbacks(Object obj)
        {
            try
            {

                m_CEL = new CQG.CQGCEL();

                m_CEL_CELDataConnectionChg(CQG.eConnectionStatus.csConnectionDown);

                //(callsFromCQG,&CallsFromCQG.m_CEL_CELDataConnectionChg);

                m_CEL.DataConnectionStatusChanged += new CQG._ICQGCELEvents_DataConnectionStatusChangedEventHandler(m_CEL_CELDataConnectionChg);


                m_CEL.TimedBarsResolved += new CQG._ICQGCELEvents_TimedBarsResolvedEventHandler(m_CEL_TimedBarResolved);

                m_CEL.TimedBarsAdded += new CQG._ICQGCELEvents_TimedBarsAddedEventHandler(m_CEL_TimedBarsAdded);

                m_CEL.TimedBarsInserted += new CQG._ICQGCELEvents_TimedBarsInsertedEventHandler(m_CEL_TimedBarsInserted);

                m_CEL.TimedBarsUpdated += new CQG._ICQGCELEvents_TimedBarsUpdatedEventHandler(m_CEL_TimedBarsUpdated);

                //m_CEL.IncorrectSymbol += new _ICQGCELEvents_IncorrectSymbolEventHandler(CEL_IncorrectSymbol);
                //m_CEL.InstrumentSubscribed += new _ICQGCELEvents_InstrumentSubscribedEventHandler(m_CEL_InstrumentSubscribed);
                //m_CEL.InstrumentChanged += new _ICQGCELEvents_InstrumentChangedEventHandler(m_CEL_InstrumentChanged);

                m_CEL.DataError += new _ICQGCELEvents_DataErrorEventHandler(m_CEL_DataError);

                //m_CEL.APIConfiguration.NewInstrumentMode = true;

                m_CEL.APIConfiguration.ReadyStatusCheck = CQG.eReadyStatusCheck.rscOff;

                m_CEL.APIConfiguration.CollectionsThrowException = false;

                m_CEL.APIConfiguration.TimeZoneCode = CQG.eTimeZone.tzPacific;

                connectCQG();

                Thread.Sleep(7000);

                //AsyncTaskListener.LogMessage("test");
            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        private void m_CEL_DataError(System.Object cqg_error, System.String error_description)
        {
            AsyncTaskListener.LogMessageAsync("CQG ERROR");

            //AsyncTaskListener.StatusUpdate("CQG ERROR", STATUS_FORMAT.ALARM, STATUS_TYPE.DATA_STATUS);
        }

        public void sendSubscribeRequest(Object queueOfOSE)
        {

#if DEBUG
            try
#endif
            {                                
                continueSubscriptionRequest = true;

                subscriptionThread = new Thread(new ParameterizedThreadStart(sendSubscribeRequestRun));
                subscriptionThread.IsBackground = true;
                subscriptionThread.Start(queueOfOSE);

            }
#if DEBUG
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
#endif

        }

        delegate void ThreadSafeGenericDelegateWithParams(OptionSpreadExpression optionSpreadExpression);

        public void resetOptionSpreadExpression(OptionSpreadExpression ose)
        {
            MongoDBConnectionAndSetup.GetFutureBarsFromMongo(ose);

            ose.lastIdxToAdd = 0;
        }

        public void sendSubscribeRequestRun(Object obj)
        {
            AsyncTaskListener.LogMessageAsync("sendSubscribeRequestRun");

            DataManagementUtility.openThread(null, null);

            try
            {


                //MongoDBConnectionAndSetup mdbcas = new MongoDBConnectionAndSetup();
                //m_CEL.RemoveAllTimedBars();
                //Thread.Sleep(3000);
                //MongoDBConnectionAndSetup mdbcs = new MongoDBConnectionAndSetup();

                //AsyncTaskListener.LogMessageAsync("sendSubscribeRequestRun 2 ");

                ConcurrentQueue<OptionSpreadExpression> queueOfOSE = (ConcurrentQueue<OptionSpreadExpression>)obj;
                if (queueOfOSE != null)
                {
                    //Debug.Assert(queueOfOSE.Count == 0);
                    //AsyncTaskListener.LogMessageAsync("sendSubscribeRequestRun 3 " + queueOfOSE.Count);

                    if (m_CEL != null && m_CEL.IsStarted)
                    {

                        //AsyncTaskListener.LogMessageAsync("sendSubscribeRequestRun 4 ");

                        int count = 1;
                        int totalCount = queueOfOSE.Count;

                        while (continueSubscriptionRequest && queueOfOSE.Count > 0)
                        {
                            //AsyncTaskListener.LogMessageAsync("sendSubscribeRequestRun * ");

                            OptionSpreadExpression inputExpression;
                            bool isSuccessful = queueOfOSE.TryDequeue(out inputExpression);
                            if (isSuccessful)
                            {
                                //MongoDBConnectionAndSetup.GetFutureBarsFromMongo(inputExpression);
                                resetOptionSpreadExpression(inputExpression);

                                Thread.Sleep(DataCollectionConstants.SUBSCRIPTION_TIMEDELAY_CONSTANT);

                                //string message = "SUBSCRIBE " + inputExpression.contract.cqgsymbol + " " + count + " of " + totalCount;
                                string message = $"SUBSCRIBE {inputExpression.contract.cqgsymbol} {count} of {totalCount} called at {DateTime.Now}";

                                AsyncTaskListener.LogMessageAsync(message);

                                AsyncTaskListener.StatusUpdateAsync(
                                    message, STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                                requestFutureContractTimeBars(inputExpression);

                                AsyncTaskListener.StatusUpdateAsync(
                                            "", STATUS_FORMAT.DEFAULT, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                                //Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                                count++;
                            }
                        }

                        AsyncTaskListener.StatusUpdateAsync(
                                    "", STATUS_FORMAT.DEFAULT, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);
                    }
                }
                //else
                //{

                //    Thread.Sleep(3000);

                //    if (m_CEL.IsStarted)
                //    {
                //        int i = 0;

                //        while (continueSubscriptionRequest && i < DataCollectionLibrary.optionSpreadExpressionList.Count)
                //        {
                //            MongoDBConnectionAndSetup.GetFutureBarsFromMongo(DataCollectionLibrary.optionSpreadExpressionList[i]);

                //            int count = i + 1;

                //            //TSErrorCatch.debugWriteOut("SUBSCRIBE " + optionSpreadExpressionList[i].cqgSymbol);


                //            if (DataCollectionLibrary.optionSpreadExpressionList.Count > i
                //                //&& DataCollectionLibrary.optionSpreadExpressionList[i].normalSubscriptionRequest
                //                && !DataCollectionLibrary.optionSpreadExpressionList[i].alreadyRequestedMinuteBars)
                //            {
                //                //if (DataCollectionLibrary.optionSpreadExpressionList[i].contract
                //                //    .idcontract == 6570)
                //                {

                //                    Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                //                    string message = "SUBSCRIBE " + DataCollectionLibrary.optionSpreadExpressionList[i].contract.cqgsymbol
                //                            + " : " + count + " OF " +
                //                            DataCollectionLibrary.optionSpreadExpressionList.Count;

                //                    AsyncTaskListener.LogMessageAsync(message);

                //                    AsyncTaskListener.StatusUpdateAsync(
                //                        message, STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                //                    requestFutureContractTimeBars(DataCollectionLibrary.optionSpreadExpressionList[i]);
                //                }
                //            }


                //            i++;
                //        }

                //        Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                //        AsyncTaskListener.StatusUpdateAsync(
                //                    "", STATUS_FORMAT.DEFAULT, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }

            

            DataManagementUtility.closeThread(null, null);
        }

        public void requestFutureContractTimeBars(OptionSpreadExpression optionSpreadExpression)
        {
            try
            {

                CQGTimedBarsRequest timedBarsRequest = m_CEL.CreateTimedBarsRequest();

                timedBarsRequest.Symbol = optionSpreadExpression.contract.cqgsymbol;

                timedBarsRequest.SessionsFilter = 31;

                timedBarsRequest.IntradayPeriod = 1;

                timedBarsRequest.Continuation = CQG.eTimeSeriesContinuationType.tsctNoContinuation;
                //do not want continuation bars

                DateTime currentTime = m_CEL.Environment.LineTime;
                

                if (optionSpreadExpression.CQGBarQueryStart.CompareTo(
                    new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, currentTime.Minute, 0))
                    >= 0)
                {
                    optionSpreadExpression.CQGBarQueryStart = currentTime.AddMinutes(-1);
                }

                DateTime rangeStart = optionSpreadExpression.CQGBarQueryStart;

                DateTime rangeEnd = currentTime;

                timedBarsRequest.RangeStart = rangeStart;

                timedBarsRequest.RangeEnd = rangeEnd;

                timedBarsRequest.IncludeEnd = true;

                timedBarsRequest.UpdatesEnabled = true;

                optionSpreadExpression.futureTimedBars = m_CEL.RequestTimedBars(timedBarsRequest);


                DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId.AddOrUpdate(
                    optionSpreadExpression.futureTimedBars.Id,
                    optionSpreadExpression, (oldKey, oldValue) => optionSpreadExpression);
            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }



        private void m_CEL_CELDataConnectionChg(CQG.eConnectionStatus new_status)
        {
            //StringBuilder connStatusString = new StringBuilder();
            StringBuilder connStatusShortString = new StringBuilder();

            STATUS_FORMAT statusFormat = STATUS_FORMAT.DEFAULT;

            if (m_CEL.IsStarted)
            {
                //connStatusString.Append("CQG API:");
                //connStatusString.Append(m_CEL.Environment.CELVersion);
                connStatusShortString.Append("CQG:");

                if (new_status != CQG.eConnectionStatus.csConnectionUp)
                {
                    if (new_status == CQG.eConnectionStatus.csConnectionDelayed)
                    {
                        statusFormat = STATUS_FORMAT.CAUTION;
                        //connStatusString.Append(" - CONNECTION IS DELAYED");
                        connStatusShortString.Append("DELAYED");
                    }
                    else
                    {
                        statusFormat = STATUS_FORMAT.ALARM;
                        //connStatusString.Append(" - CONNECTION IS DOWN");
                        connStatusShortString.Append("DOWN");
                    }
                }
                else
                {
                    //statusFormat = STATUS_FORMAT.DEFAULT;
                    //connStatusString.Append(" - CONNECTION IS UP");
                    connStatusShortString.Append("UP");
                }
            }
            else
            {
                statusFormat = STATUS_FORMAT.CAUTION;

                //connStatusString.Append("WAITING FOR API CONNECTION");

                connStatusShortString.Append("WAITING");
            }

            AsyncTaskListener.StatusUpdateAsync(connStatusShortString.ToString(), statusFormat, STATUS_TYPE.CQG_CONNECTION_STATUS);

        }

        private void m_CEL_TimedBarResolved(CQG.CQGTimedBars cqg_TimedBarsIn, CQGError cqg_error)
        {
            try
            {
                if (cqg_error == null)
                {
                    AddTimedBars(cqg_TimedBarsIn, true);
                }
                else
                {
                    AsyncTaskListener.LogMessageAsync(cqg_error.Description);
                }
            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        private void m_CEL_TimedBarsAdded(CQG.CQGTimedBars cqg_TimedBarsIn)
        {
            AddTimedBars(cqg_TimedBarsIn, true);
        }

        internal void AddTimedBarsTest()
        {
            try
            {
                List<OHLCData> barsToAdd = new List<OHLCData>();
                  

                bool singleUpdate = true;

                int idxToAdd = 0;

                DateTime currentTime = DateTime.Now;

                OHLCData ohlcData = new OHLCData();

                while (idxToAdd <= 2)
                {

                    bool error = false;

                    ohlcData = new OHLCData();

                    ohlcData.idcontract = 1;



                    //ohlcData.bartime = new DateTime(cqg_TimedBarsIn[idxToAdd].Timestamp.Ticks, DateTimeKind.Utc);

                    ohlcData.bartime = currentTime;

                    ohlcData.open = 1.0;
                    ohlcData.high = 1.0;
                    ohlcData.low = 1.0;
                    ohlcData.close = 1.0;

                            
                    ohlcData.volume = 1;

                            

                    ohlcData.errorbar = error;

                    if (!ohlcData.errorbar)
                    {
                        if (singleUpdate)
                        {
                            Task t1 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);
                        }
                        else
                        {
                            //if (!barsToAdd.ContainsKey(ohlcData.bartime))
                            {
                                barsToAdd.Add(ohlcData);
                            }
                        }
                    }

                    idxToAdd++;

                }

                ohlcData.volume = 10;

                Task t5 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);

                if (!singleUpdate && barsToAdd.Count > 0)
                {
                    Task t2 = MongoDBConnectionAndSetup.UpsertManyDataMongo(barsToAdd);
                    Task t3 = MongoDBConnectionAndSetup.UpsertManyDataMongo(barsToAdd);
                    barsToAdd[2].volume = 3;
                    Task t4 = MongoDBConnectionAndSetup.UpsertManyDataMongo(barsToAdd);
                }

            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        private void AddTimedBars(CQG.CQGTimedBars cqg_TimedBarsIn, bool resolved = false)
        {
            try
            {

                if (DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                {

                    OptionSpreadExpression ose = DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];

                    if (ose.continueUpdating
                        && ose.futureTimedBars != null)
                    {

                        if(resolved)
                        {
                            ose.lastIdxToAdd = 0;
                        }

                        //Dictionary<DateTime, OHLCData> barsToAdd = new Dictionary<DateTime, OHLCData>();

                        List<OHLCData> barsToAdd = new List<OHLCData>();

                        int lastIdxAdded = cqg_TimedBarsIn.Count - 1;

                        int idxToAdd = ose.lastIdxToAdd;

                        //Console.Write("idxToAdd "+ idxToAdd);

                        ose.lastIdxToAdd = lastIdxAdded;

                        //bool firstBarAdded = true;                        

                        //bool singleUpdate = false;
                        //if( (lastIdxAdded - idxToAdd) <= 1)
                        //{
                        //    singleUpdate = true;
                        //}

                        while (idxToAdd <= lastIdxAdded)
                        {

                            bool error = false;

                            OHLCData ohlcData = new OHLCData();

                            ohlcData.idcontract = ose.contract.idcontract;



                            ohlcData.bartime = new DateTime(cqg_TimedBarsIn[idxToAdd].Timestamp.Ticks, DateTimeKind.Utc);


                            ohlcData.open = 0;
                            ohlcData.high = 0;
                            ohlcData.low = 0;
                            ohlcData.close = 0;
                            ohlcData.volume = 0;

                            if (cqg_TimedBarsIn[idxToAdd].ActualVolume
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.volume = cqg_TimedBarsIn[idxToAdd].ActualVolume;
                            }
                            else
                            {
                                error = true;
                            }


                            if (cqg_TimedBarsIn[idxToAdd].Open
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.open = cqg_TimedBarsIn[idxToAdd].Open;
                            }
                            else
                            {
                                error = true;
                            }

                            if (cqg_TimedBarsIn[idxToAdd].High
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.high = cqg_TimedBarsIn[idxToAdd].High;
                            }
                            else
                            {
                                error = true;
                            }

                            if (cqg_TimedBarsIn[idxToAdd].Low
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.low = cqg_TimedBarsIn[idxToAdd].Low;
                            }
                            else
                            {
                                error = true;
                            }

                            if (cqg_TimedBarsIn[idxToAdd].Close
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.close = cqg_TimedBarsIn[idxToAdd].Close;
                            }
                            else
                            {
                                error = true;
                            }

                            ohlcData.errorbar = error;

                            //if (!ohlcData.errorbar)
                            {
                                //if (singleUpdate)
                                //{
                                //    Task t1 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);
                                //}
                                //else
                                {
                                    //if (!barsToAdd.ContainsKey(ohlcData.bartime))
                                    {
                                        //barsToAdd.Add(ohlcData.bartime, ohlcData);
                                        barsToAdd.Add(ohlcData);
                                    }
                                }
                            }                           

                            idxToAdd++;

                            //if (firstBarAdded)
                            //{
                            //    firstBarAdded = false;

                            //    if (!ohlcData.errorbar)
                            //    {
                            //        Task t1 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);
                            //    }

                            //}

                        }

                        //if (!singleUpdate && barsToAdd.Count > 0)
                        if (barsToAdd.Count > 0)
                        {
                            //Task t2 = MongoDBConnectionAndSetup.UpsertManyDataMongo(barsToAdd.Values.ToList());
                            Task t2 = MongoDBConnectionAndSetup.UpsertManyDataMongo(barsToAdd);
                        }


                        ose.lastTimeFuturePriceUpdated =
                                        cqg_TimedBarsIn.EndTimestamp;

                        //ose.staleData = STALE_DATA_INDICATORS.UP_TO_DATE;

                        AsyncTaskListener.ExpressionListUpdateAsync(ose);

                        

                    }

                }
            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        private void AddTimedBarsOld(CQG.CQGTimedBars cqg_TimedBarsIn, bool resolved = false)
        {
            try
            {

                if (DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                {

                    OptionSpreadExpression ose = DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];

                    if (ose.continueUpdating
                        && ose.futureTimedBars != null)
                    {



                        List<OHLCData> barsToAdd = new List<OHLCData>();

                        int lastIdxAdded = cqg_TimedBarsIn.Count - 1;

                        int idxToAdd = ose.lastIdxToAdd;

                        //Console.Write("idxToAdd "+ idxToAdd);

                        ose.lastIdxToAdd = lastIdxAdded;

                        bool firstBarAdded = true;

                        while (idxToAdd <= lastIdxAdded)
                        {

                            bool error = false;

                            OHLCData ohlcData = new OHLCData();

                            ohlcData.idcontract = ose.contract.idcontract;

                            

                            ohlcData.bartime = new DateTime(cqg_TimedBarsIn[idxToAdd].Timestamp.Ticks, DateTimeKind.Utc);

                            ohlcData.open = 0;
                            ohlcData.high = 0;
                            ohlcData.low = 0;
                            ohlcData.close = 0;
                            ohlcData.volume = 0;

                            if (cqg_TimedBarsIn[idxToAdd].ActualVolume
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.volume = cqg_TimedBarsIn[idxToAdd].ActualVolume;
                            }
                            else
                            {
                                error = true;
                            }


                            if (cqg_TimedBarsIn[idxToAdd].Open
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.open = cqg_TimedBarsIn[idxToAdd].Open;
                            }
                            else
                            {
                                error = true;
                            }

                            if (cqg_TimedBarsIn[idxToAdd].High
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.high = cqg_TimedBarsIn[idxToAdd].High;
                            }
                            else
                            {
                                error = true;
                            }

                            if (cqg_TimedBarsIn[idxToAdd].Low
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.low = cqg_TimedBarsIn[idxToAdd].Low;
                            }
                            else
                            {
                                error = true;
                            }

                            if (cqg_TimedBarsIn[idxToAdd].Close
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.close = cqg_TimedBarsIn[idxToAdd].Close;
                            }
                            else
                            {
                                error = true;
                            }

                            ohlcData.errorbar = error;





                            if (!firstBarAdded)  // && !ohlcData.errorbar)
                            {
                                barsToAdd.Add(ohlcData);
                            }

                            idxToAdd++;

                            //if (firstBarAdded)
                            {
                                //firstBarAdded = false;

                                if (!ohlcData.errorbar)
                                {
                                    Task t1 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);
                                }

                            }
                        }

                        //if (barsToAdd.Count > 0)
                        //{
                        //    Task t2 = MongoDBConnectionAndSetup.AddDataMongo(barsToAdd);
                        //}

                        ose.lastTimeFuturePriceUpdated =
                                        cqg_TimedBarsIn.EndTimestamp;

                        //ose.staleData = STALE_DATA_INDICATORS.UP_TO_DATE;

                        AsyncTaskListener.ExpressionListUpdateAsync(ose);
                    }

                }

            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        internal bool check_cqg_status_fail()
        {
            return m_CEL == null || (m_CEL != null && !m_CEL.IsStarted);
        }
        
        private void m_CEL_TimedBarsInserted(CQG.CQGTimedBars cqg_TimedBarsIn, int index)
        {
            UpdateTimedBars(cqg_TimedBarsIn, index, true);
        }

        private void m_CEL_TimedBarsUpdated(CQG.CQGTimedBars cqg_TimedBarsIn, int index)
        {
            UpdateTimedBars(cqg_TimedBarsIn, index);
        }

        private void UpdateTimedBars(CQG.CQGTimedBars cqg_TimedBarsIn, int index, bool inserted = false)
        {
            try
            {

                if (DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                {

                    OptionSpreadExpression ose
                            = DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];


                    if (ose.continueUpdating
                        && ose.futureTimedBars != null
)
                    {
                        bool error = false;

                        OHLCData ohlcData = new OHLCData();

                        ohlcData.idcontract = ose.contract.idcontract;

                        //ohlcData.bartime = cqg_TimedBarsIn[index].Timestamp.ToUniversalTime();

                        ohlcData.bartime = new DateTime(cqg_TimedBarsIn[index].Timestamp.Ticks, DateTimeKind.Utc);


                        ohlcData.open = 0;
                        ohlcData.high = 0;
                        ohlcData.low = 0;
                        ohlcData.close = 0;
                        ohlcData.volume = 0;

                        if (cqg_TimedBarsIn[index].ActualVolume
                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                        {
                            ohlcData.volume = cqg_TimedBarsIn[index].ActualVolume;
                        }
                        else
                        {
                            error = true;
                        }


                        if (cqg_TimedBarsIn[index].Open
                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                        {
                            ohlcData.open = cqg_TimedBarsIn[index].Open;
                        }
                        else
                        {
                            error = true;
                        }

                        if (cqg_TimedBarsIn[index].High
                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                        {
                            ohlcData.high = cqg_TimedBarsIn[index].High;
                        }
                        else
                        {
                            error = true;
                        }

                        if (cqg_TimedBarsIn[index].Low
                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                        {
                            ohlcData.low = cqg_TimedBarsIn[index].Low;
                        }
                        else
                        {
                            error = true;
                        }

                        if (cqg_TimedBarsIn[index].Close
                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                        {
                            ohlcData.close = cqg_TimedBarsIn[index].Close;
                        }
                        else
                        {
                            error = true;
                        }

                        ohlcData.errorbar = error;


                        //if (!error
                        //    && !ose.reachedTransactionBar
                        //    && ohlcData.bartime
                        //    .CompareTo(ose.transactionTime) <= 0)
                        //{
                        //    ose.transactionBar = ohlcData;
                        //}

                        //if (!error
                        //    && !ose.reachedTransactionBar
                        //    && ohlcData.bartime
                        //    .CompareTo(ose.transactionTime) > 0)
                        //{
                        //    ose.reachedTransactionBar = true;
                        //}

                        //if (!error
                        //&& !ose.reachedBarAfterTransactionBar
                        //&& ohlcData.bartime
                        //.CompareTo(ose.transactionTime) >= 0)
                        //{
                        //    ose.reachedBarAfterTransactionBar = true;
                        //}

                        //if (!error
                        //    && !ose.reachedDecisionBar
                        //    && ohlcData.bartime
                        //    .CompareTo(ose.decisionTime) <= 0)
                        //{
                        //    ose.decisionBar = ohlcData;
                        //}

                        //if (!error
                        //    && !ose.reachedDecisionBar
                        //    && ohlcData.bartime
                        //    .CompareTo(ose.decisionTime) >= 0)
                        //{
                        //    ose.reachedDecisionBar = true;
                        //}

                        //if (!error
                        //    && !ose.reachedBarAfterDecisionBar
                        //    && ohlcData.bartime
                        //    .CompareTo(ose.decisionTime) > 0)
                        //{
                        //    ose.reachedBarAfterDecisionBar = true;
                        //}

                        //if (!ohlcData.errorbar)
                        {
                            //if (inserted)
                            {
                                Task t1 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);
                            }
                            //else
                            //{
                            //    Task t = MongoDBConnectionAndSetup.UpdateBardataToMongo(ohlcData);
                            //}
                        }

                    }

                    //expressionCounter++;
                }

            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }

        private void m_CEL_InstrumentChanged(CQGInstrument cqgInstrument,
                                 CQGQuotes quotes,
                                 CQGInstrumentProperties props)
        {
            try
            {

                if (DataCollectionLibrary.optionSpreadExpressionHashTable_keyFullName.ContainsKey(cqgInstrument.FullName))
                {

                    //optionSpreadExpressionCheckSubscribedListIdx

                    OptionSpreadExpression optionSpreadExpression
                        = DataCollectionLibrary.optionSpreadExpressionHashTable_keyFullName[cqgInstrument.FullName];

                    if (optionSpreadExpression != null
                        && optionSpreadExpression.continueUpdating
                        && optionSpreadExpression.cqgInstrument != null

                        && cqgInstrument.CEL != null)
                    {
                        //optionSpreadExpressionList[expressionCounter].cqgInstrument = cqgInstrument;

                        CQGQuote quoteAsk = quotes[eQuoteType.qtAsk];
                        CQGQuote quoteBid = quotes[eQuoteType.qtBid];
                        CQGQuote quoteTrade = quotes[eQuoteType.qtTrade];
                        CQGQuote quoteSettlement = quotes[eQuoteType.qtSettlement];
                        CQGQuote quoteYestSettlement = quotes[eQuoteType.qtYesterdaySettlement];

                        if ((quoteAsk != null)
                            || (quoteBid != null)
                            || (quoteTrade != null)
                            || (quoteSettlement != null)
                            || (quoteYestSettlement != null))
                        {


                            fillPricesFromQuote(optionSpreadExpression,
                                optionSpreadExpression.cqgInstrument.Quotes);



                        }


                        //break;
                    }

                }
            }
            catch (Exception ex)
            {
                //TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
                AsyncTaskListener.LogMessageAsync(ex.ToString());
            }
        }


        private void fillPricesFromQuote(OptionSpreadExpression optionSpreadExpression, CQGQuotes quotes)
        {
            CQGQuote quoteAsk = quotes[eQuoteType.qtAsk];
            CQGQuote quoteBid = quotes[eQuoteType.qtBid];
            CQGQuote quoteTrade = quotes[eQuoteType.qtTrade];
            CQGQuote quoteSettlement = quotes[eQuoteType.qtSettlement];
            CQGQuote quoteYestSettlement = quotes[eQuoteType.qtYesterdaySettlement];

            if (quoteSettlement != null)
            {
                if (quoteSettlement.IsValid)
                {
                    optionSpreadExpression.settlement = quoteSettlement.Price;

                    optionSpreadExpression.settlementDateTime = quoteSettlement.ServerTimestamp;

                    if (optionSpreadExpression.settlementDateTime.Date.CompareTo(DateTime.Now.Date) == 0)
                    {
                        optionSpreadExpression.settlementIsCurrentDay = true;
                    }


                    optionSpreadExpression.settlementFilled = true;

                }
                else
                {
                    //if (!optionSpreadExpression.manuallyFilled)
                    {
                        optionSpreadExpression.settlement = 0;

                        optionSpreadExpression.settlementFilled = false;
                    }
                }


            }

        }



    }
}
