using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CQG;
using System.Collections.Concurrent;
using System.Threading;

namespace DataSupervisorForModel
{
    class CQGDataManagement
    {
        internal CQGDataManagement(RealtimeDataManagement realtimeDataManagement)
        {
            this.realtimeDataManagement = realtimeDataManagement;

            ThreadPool.QueueUserWorkItem(new WaitCallback(initializeCQGAndCallbacks));

            //AsyncTaskListener.LogMessage("test");
        }

        private DataManagementUtility dataManagementUtility = new DataManagementUtility();

        private Thread subscriptionThread;
        private bool subscriptionThreadShouldStop = false;
        private const int SUBSCRIPTION_TIMEDELAY_CONSTANT = 125;

        private RealtimeDataManagement realtimeDataManagement;

        private CQG.CQGCEL m_CEL;



        internal void connectCQG()
        {
            if (m_CEL != null)
            {
                m_CEL.Startup();
            }
        }

        internal void shutDownCQGConn()
        {
            if (m_CEL != null)
            {
                if (m_CEL.IsStarted)
                    m_CEL.RemoveAllInstruments();

                //m_CEL.Shutdown();
            }
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

                //AsyncTaskListener.LogMessage("test");
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void m_CEL_DataError(System.Object cqg_error, System.String error_description)
        {
            AsyncTaskListener.StatusUpdate("CQG ERROR", STATUS_FORMAT.ALARM, STATUS_TYPE.DATA_STATUS);
        }

        public void sendSubscribeRequest(bool sendOnlyUnsubscribed)
        {

#if DEBUG
            try
#endif
            {
                subscriptionThread = new Thread(new ParameterizedThreadStart(sendSubscribeRequestRun));
                subscriptionThread.IsBackground = true;
                subscriptionThread.Start(sendOnlyUnsubscribed);

            }
#if DEBUG
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
#endif

        }

        public void sendSubscribeRequestRun(Object obj)
        {
            dataManagementUtility.openThread(null, null);

            try
            {
                //m_CEL.RemoveAllTimedBars();
                //Thread.Sleep(3000);

                if (m_CEL.IsStarted)
                {
                    bool sendOnlyUnsubscribed = (bool)obj;

                    int i = 0;

                    while (!subscriptionThreadShouldStop && i < DataCollectionLibrary.optionSpreadExpressionList.Count)
                    {
                        MongoDBConnectionAndSetup.GetFutureBarsFromMongo(DataCollectionLibrary.optionSpreadExpressionList[i]);

                        int count = i + 1;

                        //TSErrorCatch.debugWriteOut("SUBSCRIBE " + optionSpreadExpressionList[i].cqgSymbol);

                        if (sendOnlyUnsubscribed)
                        {

                            if (!DataCollectionLibrary.optionSpreadExpressionList[i].setSubscriptionLevel)
                            {
                                Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                                string message = "SUBSCRIBE " + DataCollectionLibrary.optionSpreadExpressionList[i].contract.cqgsymbol
                                    + " : " + count + " OF " +
                                    DataCollectionLibrary.optionSpreadExpressionList.Count;

                                AsyncTaskListener.LogMessage(message);

                                AsyncTaskListener.StatusUpdate(
                                    message, STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                                m_CEL.NewInstrument(DataCollectionLibrary.optionSpreadExpressionList[i].contract.cqgsymbol);


                            }

                            if (DataCollectionLibrary.optionSpreadExpressionList[i].normalSubscriptionRequest
                                && !DataCollectionLibrary.optionSpreadExpressionList[i].alreadyRequestedMinuteBars)
                            {
                                requestFutureContractTimeBars(DataCollectionLibrary.optionSpreadExpressionList[i]);
                            }

                        }
                        else
                        {
                            DataCollectionLibrary.optionSpreadExpressionList[i].alreadyRequestedMinuteBars = false;



                            DataCollectionLibrary.optionSpreadExpressionList[i].setSubscriptionLevel = false;

                            Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                            string message = "SUBSCRIBE " + DataCollectionLibrary.optionSpreadExpressionList[i].contract.cqgsymbol
                                    + " : " + count + " OF " +
                                    DataCollectionLibrary.optionSpreadExpressionList.Count;

                            AsyncTaskListener.LogMessage(message);

                            AsyncTaskListener.StatusUpdate(
                                message, STATUS_FORMAT.CAUTION, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                            m_CEL.NewInstrument(DataCollectionLibrary.optionSpreadExpressionList[i].contract.cqgsymbol);

                            if (DataCollectionLibrary.optionSpreadExpressionList[i].normalSubscriptionRequest)
                            {
                                requestFutureContractTimeBars(DataCollectionLibrary.optionSpreadExpressionList[i]);
                            }
                        }

                        i++;
                    }

                    Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                    AsyncTaskListener.StatusUpdate(
                                "", STATUS_FORMAT.DEFAULT, STATUS_TYPE.DATA_SUBSCRIPTION_STATUS);

                }
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }

            dataManagementUtility.closeThread(null, null);
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

                DateTime rangeStart = optionSpreadExpression.CQGBarQueryStart;

                DateTime rangeEnd = m_CEL.Environment.LineTime;

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
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }



        private void m_CEL_CELDataConnectionChg(CQG.eConnectionStatus new_status)
        {
            StringBuilder connStatusString = new StringBuilder();
            StringBuilder connStatusShortString = new StringBuilder();

            STATUS_FORMAT statusFormat = STATUS_FORMAT.DEFAULT;

            if (m_CEL.IsStarted)
            {
                connStatusString.Append("CQG API:");
                connStatusString.Append(m_CEL.Environment.CELVersion);
                connStatusShortString.Append("CQG:");

                if (new_status != CQG.eConnectionStatus.csConnectionUp)
                {
                    if (new_status == CQG.eConnectionStatus.csConnectionDelayed)
                    {
                        statusFormat = STATUS_FORMAT.CAUTION;
                        connStatusString.Append(" - CONNECTION IS DELAYED");
                        connStatusShortString.Append("DELAYED");
                    }
                    else
                    {
                        statusFormat = STATUS_FORMAT.ALARM;
                        connStatusString.Append(" - CONNECTION IS DOWN");
                        connStatusShortString.Append("DOWN");
                    }
                }
                else
                {
                    //statusFormat = STATUS_FORMAT.DEFAULT;
                    connStatusString.Append(" - CONNECTION IS UP");
                    connStatusShortString.Append("UP");
                }
            }
            else
            {
                statusFormat = STATUS_FORMAT.CAUTION;

                connStatusString.Append("WAITING FOR API CONNECTION");

                connStatusShortString.Append("WAITING");
            }

            AsyncTaskListener.StatusUpdate(connStatusShortString.ToString(), statusFormat, STATUS_TYPE.CQG_CONNECTION_STATUS);

        }

        private void m_CEL_TimedBarResolved(CQG.CQGTimedBars cqg_TimedBarsIn, CQGError cqg_error)
        {
            try
            {
                if (cqg_error == null)
                {
                    AddTimedBars(cqg_TimedBarsIn);
                }
                else
                {
                    AsyncTaskListener.LogMessage(cqg_error.Description);
                }
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void AddTimedBars(CQG.CQGTimedBars cqg_TimedBarsIn)
        {
            try
            {

                if (DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                {

                    OptionSpreadExpression ose = DataCollectionLibrary.optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];

                    if (ose.continueUpdating
                        && ose.futureTimedBars != null)
                    {


                        //
                        if (!ose.alreadyRequestedMinuteBars)
                        {
                            ose.alreadyRequestedMinuteBars = true;

                            DateTime currentDay = DateTime.Now;

                            ose.transactionTime
                                    = currentDay.Date
                                    .AddHours(
                                        ose.instrument.customdayboundarytime.Hour)
                                    .AddMinutes(
                                        ose.instrument.customdayboundarytime.Minute);

                            ose.decisionTime
                                = currentDay.Date
                                .AddHours(
                                    ose.instrument.customdayboundarytime.Hour)
                                .AddMinutes(
                                    ose.instrument.customdayboundarytime.Minute
                                    - ose.instrument.decisionoffsetminutes);
                        }


                        List<OHLCData> barsToAdd = new List<OHLCData>();

                        int lastIdxAdded = cqg_TimedBarsIn.Count - 1;

                        int idxToAdd = ose.lastIdxToAdd;

                        ose.lastIdxToAdd = lastIdxAdded;

                        bool firstBarAdded = true;

                        while (idxToAdd <= lastIdxAdded)
                        {

                            bool error = false;

                            OHLCData ohlcData = new OHLCData();

                            ohlcData.idcontract = ose.contract.idcontract;

                            if (!firstBarAdded)
                            {
                                barsToAdd.Add(ohlcData);
                            }

                            ohlcData.bartime = cqg_TimedBarsIn[idxToAdd].Timestamp;

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


                            if (!error
                                && !ose.reachedTransactionBar
                                && ohlcData.bartime
                                .CompareTo(ose.transactionTime) <= 0)
                            {
                                ose.transactionBar = ohlcData;
                            }

                            if (!error
                                && !ose.reachedTransactionBar
                                && ohlcData.bartime
                                .CompareTo(ose.transactionTime) > 0)
                            {
                                ose.reachedTransactionBar = true;
                            }

                            if (!error
                            && !ose.reachedBarAfterTransactionBar
                            && ohlcData.bartime
                            .CompareTo(ose.transactionTime) >= 0)
                            {
                                ose.reachedBarAfterTransactionBar = true;
                            }

                            if (!error
                                && !ose.reachedDecisionBar
                                && ohlcData.bartime
                                .CompareTo(ose.decisionTime) <= 0)
                            {
                                ose.decisionBar = ohlcData;
                            }

                            if (!error
                                && !ose.reachedDecisionBar
                                && ohlcData.bartime
                                .CompareTo(ose.decisionTime) >= 0)
                            {
                                ose.reachedDecisionBar = true;
                            }

                            if (!error
                                && !ose.reachedBarAfterDecisionBar
                                && ohlcData.bartime
                                .CompareTo(ose.decisionTime) > 0)
                            {
                                ose.reachedBarAfterDecisionBar = true;
                            }


                            idxToAdd++;

                            if (firstBarAdded)
                            {
                                firstBarAdded = false;

                                Task t1 = MongoDBConnectionAndSetup.UpsertBardataToMongo(ohlcData);
                            }
                        }

                        if (barsToAdd.Count > 0)
                        {
                            Task t2 = MongoDBConnectionAndSetup.AddDataMongo(barsToAdd);
                        }

                        ose.lastTimeFuturePriceUpdated =
                                        cqg_TimedBarsIn.EndTimestamp;


                    }

                }

            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void m_CEL_TimedBarsAdded(CQG.CQGTimedBars cqg_TimedBarsIn)
        {
            AddTimedBars(cqg_TimedBarsIn);
        }

       
        private void m_CEL_TimedBarsUpdated(CQG.CQGTimedBars cqg_TimedBarsIn, int index)
        {
            //Debug.WriteLine("m_CEL_ExpressionResolved" + cqg_expression.Count);
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

                        ohlcData.bartime = cqg_TimedBarsIn[index].Timestamp;

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


                        if (!error
                            && !ose.reachedTransactionBar
                            && ohlcData.bartime
                            .CompareTo(ose.transactionTime) <= 0)
                        {
                            ose.transactionBar = ohlcData;
                        }

                        if (!error
                            && !ose.reachedTransactionBar
                            && ohlcData.bartime
                            .CompareTo(ose.transactionTime) > 0)
                        {
                            ose.reachedTransactionBar = true;
                        }

                        if (!error
                        && !ose.reachedBarAfterTransactionBar
                        && ohlcData.bartime
                        .CompareTo(ose.transactionTime) >= 0)
                        {
                            ose.reachedBarAfterTransactionBar = true;
                        }

                        if (!error
                            && !ose.reachedDecisionBar
                            && ohlcData.bartime
                            .CompareTo(ose.decisionTime) <= 0)
                        {
                            ose.decisionBar = ohlcData;
                        }

                        if (!error
                            && !ose.reachedDecisionBar
                            && ohlcData.bartime
                            .CompareTo(ose.decisionTime) >= 0)
                        {
                            ose.reachedDecisionBar = true;
                        }

                        if (!error
                            && !ose.reachedBarAfterDecisionBar
                            && ohlcData.bartime
                            .CompareTo(ose.decisionTime) > 0)
                        {
                            ose.reachedBarAfterDecisionBar = true;
                        }

                        Task t = MongoDBConnectionAndSetup.UpdateBardataToMongo(ohlcData);

                    }

                    //expressionCounter++;
                }

            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void m_CEL_InstrumentSubscribed(String symbol, CQGInstrument cqgInstrument)
        {
            try
            {
                AsyncTaskListener.StatusUpdate("CQG GOOD", STATUS_FORMAT.ALARM, STATUS_TYPE.DATA_STATUS);


                if (DataCollectionLibrary.optionSpreadExpressionHashTable_keySymbol.ContainsKey(symbol))
                {

                    OptionSpreadExpression optionSpreadExpression =
                        DataCollectionLibrary.optionSpreadExpressionHashTable_keySymbol[symbol];

                    //while (expressionCounter < optionSpreadExpressionList.Count)
                    //{
                    if (optionSpreadExpression.continueUpdating
                        //&& symbol.CompareTo(optionSpreadExpressionList[expressionCounter].cqgSymbol) == 0
                        && !optionSpreadExpression.setSubscriptionLevel)
                    {
                        optionSpreadExpression.setSubscriptionLevel = true;

                        optionSpreadExpression.cqgInstrument = cqgInstrument;


                        //int idx = expressionCounter;

                        //optionSpreadExpressionListHashTableIdx.AddOrUpdate(
                        //        cqgInstrument.FullName, idx,
                        //        (oldKey, oldValue) => idx);

                        DataCollectionLibrary.optionSpreadExpressionHashTable_keyFullName.AddOrUpdate(
                                cqgInstrument.FullName, optionSpreadExpression,
                                (oldKey, oldValue) => optionSpreadExpression);

                        //if (cqgInstrument.FullName.CompareTo("P.US.EU6J1511100") == 0)
                        //{
                        //    Console.WriteLine(cqgInstrument.FullName);
                        //}

                        fillPricesFromQuote(optionSpreadExpression,
                            optionSpreadExpression.cqgInstrument.Quotes);



                        ///<summary>below sets the subscription level of the CQG data</summary>
                        optionSpreadExpression.cqgInstrument.DataSubscriptionLevel
                            = eDataSubscriptionLevel.dsQuotes;


                    }

                    //expressionCounter++;
                }

            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
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
                            //                                 if (optionSpreadExpressionList[expressionCounter].callPutOrFuture
                            //                                     != (int)OPTION_SPREAD_CONTRACT_TYPE.FUTURE)
                            //                                 {
                            //                                     TSErrorCatch.debugWriteOut(
                            //                                         cqgInstrument.FullName + "  " +
                            //                                         optionSpreadExpressionList[expressionCounter].cqgInstrument.FullName + "  " +
                            //                                         optionSpreadExpressionList[expressionCounter].cqgSymbol + "  " +
                            //                                         "ASK " + ((quoteAsk != null && quoteAsk.IsValid) ? quoteAsk.Price.ToString() : "blank") + " " +
                            //                                         "BID " + ((quoteBid != null && quoteBid.IsValid) ? quoteBid.Price.ToString() : "blank") + " " +
                            //                                         "TRADE " + ((quoteTrade != null && quoteTrade.IsValid) ? quoteTrade.Price.ToString() : "blank") + " " +
                            //                                         "SETTL " + ((quoteSettlement != null && quoteSettlement.IsValid) ? quoteSettlement.Price.ToString() : "blank") + " " +
                            //                                         "YEST " + ((quoteYestSettlement != null && quoteYestSettlement.IsValid) ? quoteYestSettlement.Price.ToString() : "blank") + " "
                            //                                         );
                            //                                 }

                            //                                 quoteValue =
                            //                                     optionSpreadExpressionList[expressionCounter].cqgInstrument.ToDisplayPrice(quote.Price);

                            fillPricesFromQuote(optionSpreadExpression,
                                optionSpreadExpression.cqgInstrument.Quotes);

                            //if (optionSpreadExpression.callPutOrFuture !=
                            //        OPTION_SPREAD_CONTRACT_TYPE.FUTURE)
                            //{
                            //    fillDefaultMidPrice(optionSpreadExpression);

                            //    manageExpressionPriceCalcs(optionSpreadExpression);
                            //}

                        }


                        //break;
                    }

                }
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        //private void fillDefaultMidPrice(OptionSpreadExpression optionSpreadExpression)  //, Instrument instrument)
        //{

        //    double defaultPrice = 0;

        //    optionSpreadExpression.lastTimeUpdated = optionSpreadExpression.lastTimeFuturePriceUpdated;

        //    TimeSpan span = DateTime.Now - optionSpreadExpression.lastTimeUpdated;

        //    optionSpreadExpression.minutesSinceLastUpdate = span.TotalMinutes;

        //    if (optionSpreadExpression.)
        //    {
        //        defaultPrice = optionSpreadExpression.trade;
        //    }
        //    else if (optionSpreadExpression.settlementFilled)
        //    {
        //        defaultPrice = optionSpreadExpression.settlement;
        //    }
        //    else if (optionSpreadExpression.yesterdaySettlementFilled)
        //    {
        //        defaultPrice = optionSpreadExpression.yesterdaySettlement;
        //    }

        //    if (defaultPrice == 0)
        //    {
        //        defaultPrice = DataCollectionConstants.ZERO_PRICE;
        //    }


        //    //can set default price for futures here b/c no further price possibilities for future;
        //    optionSpreadExpression.defaultPrice = defaultPrice;

        //    optionSpreadExpression.defaultPriceFilled = true;

        //}

        //public void manageExpressionPriceCalcs(OptionSpreadExpression optionSpreadExpression)
        //{
        //    fillFutureDecisionAndTransactionPrice(optionSpreadExpression);            
        //}

        //public void fillFutureDecisionAndTransactionPrice(OptionSpreadExpression optionSpreadExpression)
        //{
        //    if (optionSpreadExpression.decisionBar != null && optionSpreadExpression.todayTransactionBar != null)
        //    {

        //        optionSpreadExpression.decisionPrice =
        //            optionSpreadExpression.decisionBar.close;

        //        optionSpreadExpression.decisionPriceTime =
        //            optionSpreadExpression.decisionBar.barTime;

        //        optionSpreadExpression.decisionPriceFilled = true;



        //        optionSpreadExpression.transactionPrice =
        //            optionSpreadExpression.todayTransactionBar.close;

        //        optionSpreadExpression.transactionPriceTime =
        //            optionSpreadExpression.todayTransactionBar.barTime;


        //        if (optionSpreadExpression.reachedTransactionTimeBoundary)
        //        {
        //            //optionSpreadExpression.filledAfterTransactionTimeBoundary = true;

        //            optionSpreadExpression.transactionPriceFilled = true;

        //            //foreach (OptionSpreadExpression ose in optionSpreadExpression.optionExpressionsThatUseThisFutureAsUnderlying)
        //            //{
        //            //    ose.transactionPriceFilled = true;
        //            //}

        //        }




        //    }

        //}

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
