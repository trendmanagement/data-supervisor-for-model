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

        internal REALTIME_PRICE_FILL_TYPE realtimePriceFillType = REALTIME_PRICE_FILL_TYPE.PRICE_DEFAULT;

        private Thread subscriptionThread;
        private bool subscriptionThreadShouldStop = false;
        private const int SUBSCRIPTION_TIMEDELAY_CONSTANT = 125;

        private RealtimeDataManagement realtimeDataManagement;
        
        private CQG.CQGCEL m_CEL;

        List<OptionSpreadExpression> optionSpreadExpressionList = new List<OptionSpreadExpression>();

        private ConcurrentDictionary<string, OptionSpreadExpression> optionSpreadExpressionHashTable_keySymbol
            = new ConcurrentDictionary<string, OptionSpreadExpression>();

        private ConcurrentDictionary<string, OptionSpreadExpression> optionSpreadExpressionHashTable_keyCQGInId
            = new ConcurrentDictionary<string, OptionSpreadExpression>();

        private ConcurrentDictionary<string, OptionSpreadExpression> optionSpreadExpressionHashTable_keyFullName
            = new ConcurrentDictionary<string, OptionSpreadExpression>();

        internal ConcurrentDictionary<long, Instrument> instrumentHashTable
            = new ConcurrentDictionary<long, Instrument>();

        internal ConcurrentDictionary<long, List<Contract>> contractHashTableByInstId
            = new ConcurrentDictionary<long, List<Contract>>();

        internal List<Instrument> instrumentList = new List<Instrument>();


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
            try
            {
                if (realtimeDataManagement != null)
                {
                    realtimeDataManagement.updateCQGDataStatus(
                        "CQG ERROR", Color.Yellow, Color.Red);
                }
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
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

                    //m_CEL.NewInstrument("C.US.EPN1312600");

                    while (!subscriptionThreadShouldStop && i < optionSpreadExpressionList.Count)
                    {

                        //TSErrorCatch.debugWriteOut("SUBSCRIBE " + optionSpreadExpressionList[i].cqgSymbol);

                        if (sendOnlyUnsubscribed)
                        {

                            if (!optionSpreadExpressionList[i].requestedMinuteBars
                                    && optionSpreadExpressionList[i].normalSubscriptionRequest)
                            {
                                Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                                requestFutureContractTimeBars(optionSpreadExpressionList[i]);
                            }


                            //if (!optionSpreadExpressionList[i].setSubscriptionLevel)
                            //{
                            //    Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                            //    realtimeDataManagement.updateStatusSubscribeData(
                            //        "SUBSCRIBE " + optionSpreadExpressionList[i].cqgSymbol
                            //        + " : " + i + " OF " +
                            //        optionSpreadExpressionList.Count);

                            //    m_CEL.NewInstrument(optionSpreadExpressionList[i].cqgSymbol);

                            //}

                            //if (optionSpreadExpressionList[i].callPutOrFuture == OPTION_SPREAD_CONTRACT_TYPE.FUTURE)
                            //{
                            //    if (!optionSpreadExpressionList[i].requestedMinuteBars
                            //        && optionSpreadExpressionList[i].normalSubscriptionRequest)
                            //    {
                            //        requestFutureContractTimeBars(optionSpreadExpressionList[i]);
                            //    }
                            //}

                        }
                        else
                        {
                            optionSpreadExpressionList[i].requestedMinuteBars = false;

                            if (optionSpreadExpressionList[i].normalSubscriptionRequest)
                            {
                                Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                                requestFutureContractTimeBars(optionSpreadExpressionList[i]);
                            }


                            //optionSpreadExpressionList[i].setSubscriptionLevel = false;

                            //Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                            //realtimeDataManagement.updateStatusSubscribeData(
                            //        "SUBSCRIBE " + optionSpreadExpressionList[i].cqgSymbol
                            //        + " : " + i + " OF " +
                            //        optionSpreadExpressionList.Count);

                            //m_CEL.NewInstrument(optionSpreadExpressionList[i].cqgSymbol);

                            //if (optionSpreadExpressionList[i].callPutOrFuture == OPTION_SPREAD_CONTRACT_TYPE.FUTURE)
                            //{
                            //    optionSpreadExpressionList[i].requestedMinuteBars = false;                                

                            //    if (optionSpreadExpressionList[i].normalSubscriptionRequest)
                            //    {
                            //        requestFutureContractTimeBars(optionSpreadExpressionList[i]);
                            //    }

                            //}
                        }



                        i++;
                    }

                    Thread.Sleep(SUBSCRIPTION_TIMEDELAY_CONSTANT);

                    realtimeDataManagement.updateStatusSubscribeData("");

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

                DateTime rangeStart = optionSpreadExpression.previousDateTimeBoundaryStart;

                DateTime rangeEnd = m_CEL.Environment.LineTime;

                timedBarsRequest.RangeStart = rangeStart;

                timedBarsRequest.RangeEnd = rangeEnd;

                timedBarsRequest.IncludeEnd = true;

                timedBarsRequest.UpdatesEnabled = true;

                optionSpreadExpression.futureTimedBars = m_CEL.RequestTimedBars(timedBarsRequest);


                optionSpreadExpressionHashTable_keyCQGInId.AddOrUpdate(
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
            Color connColor = Color.Red;

            try
            {
                if (m_CEL.IsStarted)
                {
                    connStatusString.Append("CQG API:");
                    connStatusString.Append(m_CEL.Environment.CELVersion);
                    connStatusShortString.Append("CQG:");

                    if (new_status != CQG.eConnectionStatus.csConnectionUp)
                    {
                        if (new_status == CQG.eConnectionStatus.csConnectionDelayed)
                        {
                            connColor = Color.BlanchedAlmond;
                            connStatusString.Append(" - CONNECTION IS DELAYED");
                            connStatusShortString.Append("DELAYED");
                        }
                        else
                        {
                            connStatusString.Append(" - CONNECTION IS DOWN");
                            connStatusShortString.Append("DOWN");
                        }
                    }
                    else
                    {
                        connColor = Color.LawnGreen;
                        connStatusString.Append(" - CONNECTION IS UP");
                        connStatusShortString.Append("UP");
                    }
                }
                else
                {
                    connStatusString.Append("WAITING FOR API CONNECTION");

                    connStatusShortString.Append("WAITING");
                }

                if (realtimeDataManagement != null)
                {
                    realtimeDataManagement.updateConnectionStatus(
                        connStatusString.ToString(), connColor);
                }
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void m_CEL_TimedBarResolved(CQG.CQGTimedBars cqg_TimedBarsIn, CQGError cqg_error)
        {
            //Debug.WriteLine("m_CEL_ExpressionResolved" + cqg_expression.Count);
            try
            {
                //TSErrorCatch.debugWriteOut(cqg_TimedBarsIn.Id);

                if (cqg_error == null)
                {
                    //int expressionCounter = 0;

                    if (optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                    {

                        OptionSpreadExpression optionSpreadExpression
                            = optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];

                        Instrument instrument = null;

                        if (instrumentHashTable.ContainsKey(optionSpreadExpression.instrument.idinstrument))
                        {
                            instrument = instrumentHashTable[optionSpreadExpression.instrument.idinstrument];
                        }

                        //while (expressionCounter < optionSpreadExpressionList.Count)
                        //{
                        if (optionSpreadExpression.stopUpdating
                            && optionSpreadExpression.futureTimedBars != null)
                        {
                            optionSpreadExpression.requestedMinuteBars = true;

                            optionSpreadExpression.futureBarData
                                = new List<OHLCData>(DataCollectionConstants.MINUTES_IN_DAY);

                            //OHLCData ohlcData = new OHLCData();

                            //optionSpreadExpressionList[expressionCounter].futureBarData.Add(ohlcData);

                            //TSErrorCatch.debugWriteOut("resolved " + cqg_TimedBarsIn.Id
                            //    + "  " + cqg_TimedBarsIn.Count);

                            if (cqg_TimedBarsIn.Count > 0)
                            {

                                int timeBarsIn_CurrentDay_TransactionIdx = cqg_TimedBarsIn.Count - 1;

                                DateTime currentDay = DateTime.Now;

                                if (instrument != null)
                                {

                                    //changed decision and transaction date to the modelDateTime rather than
                                    //the date of the latest bar. Because it was failing on instruments like cattle
                                    //and hogs that don't have data
                                    optionSpreadExpression.todayTransactionTimeBoundary
                                        = currentDay.Date
                                        .AddHours(
                                           instrument.customdayboundarytime.Hour)
                                        .AddMinutes(
                                            instrument.customdayboundarytime.Minute);

                                    optionSpreadExpression.todayDecisionTime
                                        = currentDay.Date
                                        .AddHours(
                                            instrument.customdayboundarytime.Hour)
                                        .AddMinutes(
                                            instrument.customdayboundarytime.Minute
                                            - instrument.decisionoffsetminutes);
                                }

                                //TSErrorCatch.debugWriteOut(
                                //    optionSpreadExpressionList[expressionCounter].cqgSymbol + " " +
                                //    optionSpreadExpressionList[expressionCounter].todayDecisionTime)

                                //optionSpreadExpressionList[expressionCounter].settlementDateTimeMarker
                                //    = optionSpreadManager.initializationParmsSaved.modelDateTime.Date
                                //    .AddHours(
                                //        optionSpreadExpressionList[expressionCounter].instrument.settlementTime.Hour)
                                //    .AddMinutes(
                                //        optionSpreadExpressionList[expressionCounter].instrument.settlementTime.Minute
                                //        + 15);

                                //TSErrorCatch.debugWriteOut(
                                //    optionSpreadExpressionList[expressionCounter].instrument.CQGsymbol + "  " +
                                //    optionSpreadExpressionList[expressionCounter].settlementDateTimeMarker);


                                int timeBarsIn_Counter = 0;
                                int cumVolume = 0;


                                while (timeBarsIn_Counter < cqg_TimedBarsIn.Count)
                                {




                                    if (cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                        .CompareTo(optionSpreadExpression.previousDateTimeBoundaryStart) >= 0)
                                    //&& cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                    //.CompareTo(optionSpreadExpressionList[expressionCounter].todayTransactionTimeBoundary) <= 0)
                                    {
                                        bool error = false;

                                        OHLCData ohlcData = new OHLCData();

                                        optionSpreadExpression.futureBarData.Add(ohlcData);

                                        ohlcData.barTime = cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp;

                                        int volume = 0;

                                        if (cqg_TimedBarsIn[timeBarsIn_Counter].ActualVolume
                                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                        {
                                            volume = cqg_TimedBarsIn[timeBarsIn_Counter].ActualVolume;
                                        }
                                        else
                                        {
                                            error = true;
                                        }

                                        //if (timeBarsIn_Counter == 0)
                                        //{
                                        //    TSErrorCatch.debugWriteOut(
                                        //        optionSpreadExpressionList[expressionCounter].cqgSymbol +
                                        //        " VOL BAR " + cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp);
                                        //}
                                        //else if (timeBarsIn_Counter == cqg_TimedBarsIn.Count - 1)
                                        //{
                                        //    TSErrorCatch.debugWriteOut(
                                        //        optionSpreadExpressionList[expressionCounter].cqgSymbol +
                                        //        " VOL BAR " + cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp);
                                        //}

                                        cumVolume += volume;

                                        ohlcData.cumulativeVolume = cumVolume;

                                        ohlcData.volume = volume;



                                        if (cqg_TimedBarsIn[timeBarsIn_Counter].Open
                                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                        {
                                            ohlcData.open = cqg_TimedBarsIn[timeBarsIn_Counter].Open;
                                        }
                                        else
                                        {
                                            error = true;
                                        }

                                        if (cqg_TimedBarsIn[timeBarsIn_Counter].High
                                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                        {
                                            ohlcData.high = cqg_TimedBarsIn[timeBarsIn_Counter].High;
                                        }
                                        else
                                        {
                                            error = true;
                                        }

                                        if (cqg_TimedBarsIn[timeBarsIn_Counter].Low
                                            != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                        {
                                            ohlcData.low = cqg_TimedBarsIn[timeBarsIn_Counter].Low;
                                        }
                                        else
                                        {
                                            error = true;
                                        }


                                        if (cqg_TimedBarsIn[timeBarsIn_Counter].Close
                                        != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                        {
                                            ohlcData.close = cqg_TimedBarsIn[timeBarsIn_Counter].Close;
                                        }
                                        else
                                        {
                                            error = true;
                                        }

                                        ohlcData.errorBar = error;



                                        if (!error
                                            && !optionSpreadExpression.reachedTransactionTimeBoundary
                                            && cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                            .CompareTo(optionSpreadExpression.todayTransactionTimeBoundary) <= 0)
                                        {
                                            optionSpreadExpression.todayTransactionBar = ohlcData;
                                        }

                                        if (!error
                                            && !optionSpreadExpression.reachedTransactionTimeBoundary
                                            && cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                            .CompareTo(optionSpreadExpression.todayTransactionTimeBoundary) >= 0)
                                        {
                                            optionSpreadExpression.reachedTransactionTimeBoundary = true;
                                        }

                                        if (!error
                                            && !optionSpreadExpression.reachedDecisionBar
                                            && cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                            .CompareTo(optionSpreadExpression.todayDecisionTime) <= 0)
                                        {
                                            optionSpreadExpression.decisionBar = ohlcData;
                                        }

                                        if (!error
                                            && !optionSpreadExpression.reachedDecisionBar
                                            && cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                            .CompareTo(optionSpreadExpression.todayDecisionTime) >= 0)
                                        {
                                            optionSpreadExpression.reachedDecisionBar = true;


                                        }

                                        if (!error
                                            && !optionSpreadExpression.reachedBarAfterDecisionBar
                                            && cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                            .CompareTo(optionSpreadExpression.todayDecisionTime) > 0)
                                        {
                                            optionSpreadExpression.reachedBarAfterDecisionBar = true;
                                        }

                                        if (!error
                                            && !optionSpreadExpression.reached1MinAfterDecisionBarUsedForSnapshot
                                            && cqg_TimedBarsIn[timeBarsIn_Counter].Timestamp
                                            .CompareTo(optionSpreadExpression.todayDecisionTime.AddMinutes(1)) > 0)
                                        {
                                            optionSpreadExpression.reached1MinAfterDecisionBarUsedForSnapshot = true;
                                        }


                                    }

                                    timeBarsIn_Counter++;
                                }

                                int backTimeCounter = cqg_TimedBarsIn.Count - 1;

                                while (backTimeCounter >= 0)
                                {
                                    if (cqg_TimedBarsIn[backTimeCounter].Close
                                    != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                    {
                                        optionSpreadExpression.lastTimeFuturePriceUpdated =
                                            cqg_TimedBarsIn[backTimeCounter].Timestamp;

                                        optionSpreadExpression.trade =
                                            cqg_TimedBarsIn[backTimeCounter].Close;

                                        optionSpreadExpression.tradeFilled = true;

                                        break;
                                    }

                                    backTimeCounter--;
                                }



                                fillDefaultMidPrice(optionSpreadExpression);

                                manageExpressionPriceCalcs(optionSpreadExpression);

                            }

                            //break;  //while (expressionCounter < optionSpreadExpressionList.Count)

                        }

                        //expressionCounter++;

                    }


                }
                else
                {
                    realtimeDataManagement.updateCQGDataStatus("CQG ERROR", Color.Yellow, Color.Red);
                }
            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void m_CEL_TimedBarsAdded(CQG.CQGTimedBars cqg_TimedBarsIn)
        {
            try
            {
                //TSErrorCatch.debugWriteOut(cqg_TimedBarsIn.Id);

                {

                    //if (optionSpreadExpressionFutureTimedBarsListIdx.Count > 0
                    //    &&
                    //    optionSpreadExpressionFutureTimedBarsListIdx.ContainsKey(cqg_TimedBarsIn.Id))
                    
                    if (optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                    {

                        OptionSpreadExpression optionSpreadExpression = optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];

                        //while (expressionCounter < optionSpreadExpressionList.Count)
                        //{
                        if (!optionSpreadExpression.stopUpdating
                            && optionSpreadExpression.futureTimedBars != null)

                        //while (expressionCounter < optionSpreadExpressionList.Count)
                        //{
                        //    if (!optionSpreadExpressionList[expressionCounter].stopUpdating
                        //        && optionSpreadExpressionList[expressionCounter].futureTimedBars != null
                        //        && cqg_TimedBarsIn.Id.CompareTo(optionSpreadExpressionList[expressionCounter].futureTimedBars.Id) == 0)
                        //&& !optionSpreadExpressionList[expressionCounter].requestedMinuteBars)
                        {
                            int lastTimdBarsInIdx = cqg_TimedBarsIn.Count - 1;

                            while (lastTimdBarsInIdx >= 0)
                            {
                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].Timestamp.CompareTo(
                                    optionSpreadExpression.futureBarData.Last().barTime) <= 0)
                                {
                                    lastTimdBarsInIdx++;
                                    break;
                                }

                                lastTimdBarsInIdx--;
                            }

                            if (lastTimdBarsInIdx < 0)
                            {
                                lastTimdBarsInIdx = 0;
                            }

                            while (lastTimdBarsInIdx < cqg_TimedBarsIn.Count)
                            {

                                bool error = false;


                                OHLCData ohlcData = new OHLCData();

                                optionSpreadExpression.futureBarData.Add(ohlcData);

                                ohlcData.barTime = cqg_TimedBarsIn[lastTimdBarsInIdx].Timestamp;

                                int volume = 0;

                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].ActualVolume
                                    != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                {
                                    volume = cqg_TimedBarsIn[lastTimdBarsInIdx].ActualVolume;
                                }
                                else
                                {
                                    error = true;
                                }



                                ohlcData.volume = volume;


                                int cumVolume = 0;

                                int cumCounter = 0;

                                foreach (OHLCData ohlcDataCumvol in optionSpreadExpression.futureBarData)
                                {
                                    if (ohlcDataCumvol.barTime.CompareTo(
                                        optionSpreadExpression.previousDateTimeBoundaryStart) >= 0)
                                    {
                                        //if (cumCounter == 0)
                                        //{
                                        //    TSErrorCatch.debugWriteOut(
                                        //        optionSpreadExpressionList[expressionCounter].cqgSymbol +
                                        //        " VOL BAR " + ohlcDataCumvol.barTime);
                                        //}
                                        //else if (cumCounter == 
                                        //    optionSpreadExpressionList[expressionCounter].futureBarData.Count - 1)
                                        //{
                                        //    TSErrorCatch.debugWriteOut(
                                        //        optionSpreadExpressionList[expressionCounter].cqgSymbol +
                                        //        " VOL BAR " + ohlcDataCumvol.barTime);
                                        //}

                                        cumCounter++;

                                        cumVolume += ohlcDataCumvol.volume;

                                        ohlcDataCumvol.cumulativeVolume = cumVolume;
                                    }
                                }


                                //int timeBarsIn_Counter = 0;

                                //while (timeBarsIn_Counter < cqg_TimedBarsIn.Count)
                                //{


                                //    if (cqg_TimedBarsIn[timeBarsIn_Counter].ActualVolume
                                //                != -TradingSystemConstants.CQG_DATA_ERROR_CODE)
                                //    {
                                //        //volume = cqg_TimedBarsIn[timeBarsIn_Counter].ActualVolume;

                                //        cumVolume += cqg_TimedBarsIn[timeBarsIn_Counter].ActualVolume;
                                //    }

                                //    timeBarsIn_Counter++;

                                //}

                                ohlcData.cumulativeVolume = cumVolume;


                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].Open
                                    != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                {
                                    ohlcData.open = cqg_TimedBarsIn[lastTimdBarsInIdx].Open;
                                }
                                else
                                {
                                    error = true;
                                }

                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].High
                                    != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                {
                                    ohlcData.high = cqg_TimedBarsIn[lastTimdBarsInIdx].High;
                                }
                                else
                                {
                                    error = true;
                                }

                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].Low
                                    != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                {
                                    ohlcData.low = cqg_TimedBarsIn[lastTimdBarsInIdx].Low;
                                }
                                else
                                {
                                    error = true;
                                }

                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].Close
                                    != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                {
                                    ohlcData.close = cqg_TimedBarsIn[lastTimdBarsInIdx].Close;
                                }
                                else
                                {
                                    error = true;
                                }

                                ohlcData.errorBar = error;


                                //OHLCData futureBarData = optionSpreadExpressionList[expressionCounter].futureBarData[0];

                                //futureBarData.barTime = ohlcData.barTime;

                                //futureBarData.open = ohlcData.open;

                                //futureBarData.high = ohlcData.high;

                                //futureBarData.low = ohlcData.low;

                                //futureBarData.close = ohlcData.close;

                                //futureBarData.volume = ohlcData.volume;

                                //futureBarData.cumulativeVolume = ohlcData.cumulativeVolume;

                                //futureBarData.errorBar = ohlcData.errorBar;


                                if (!error
                                    && !optionSpreadExpression.reachedTransactionTimeBoundary
                                    && ohlcData.barTime
                                    .CompareTo(optionSpreadExpression.todayTransactionTimeBoundary) <= 0)
                                {
                                    optionSpreadExpression.todayTransactionBar = ohlcData;
                                }

                                if (!error
                                    && !optionSpreadExpression.reachedTransactionTimeBoundary
                                    && ohlcData.barTime
                                    .CompareTo(optionSpreadExpression.todayTransactionTimeBoundary) >= 0)
                                {
                                    optionSpreadExpression.reachedTransactionTimeBoundary = true;
                                }

                                if (!error
                                    && !optionSpreadExpression.reachedDecisionBar
                                    && ohlcData.barTime
                                    .CompareTo(optionSpreadExpression.todayDecisionTime) <= 0)
                                {
                                    optionSpreadExpression.decisionBar = ohlcData;
                                }

                                if (!error
                                    && !optionSpreadExpression.reachedDecisionBar
                                    && ohlcData.barTime
                                    .CompareTo(optionSpreadExpression.todayDecisionTime) >= 0)
                                {
                                    optionSpreadExpression.reachedDecisionBar = true;
                                }

                                if (!error
                                    && !optionSpreadExpression.reachedBarAfterDecisionBar
                                    && ohlcData.barTime
                                    .CompareTo(optionSpreadExpression.todayDecisionTime) > 0)
                                {
                                    optionSpreadExpression.reachedBarAfterDecisionBar = true;
                                }

                                if (!error
                                    && !optionSpreadExpression.reached1MinAfterDecisionBarUsedForSnapshot
                                    && ohlcData.barTime
                                    .CompareTo(optionSpreadExpression.todayDecisionTime.AddMinutes(1)) > 0)
                                {
                                    optionSpreadExpression.reached1MinAfterDecisionBarUsedForSnapshot = true;
                                }



                                if (cqg_TimedBarsIn[lastTimdBarsInIdx].Close
                                        != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                                {
                                    optionSpreadExpression.lastTimeFuturePriceUpdated =
                                                cqg_TimedBarsIn[lastTimdBarsInIdx].Timestamp;

                                    optionSpreadExpression.trade =
                                            cqg_TimedBarsIn[lastTimdBarsInIdx].Close;

                                    optionSpreadExpression.tradeFilled = true;

                                    fillDefaultMidPrice(optionSpreadExpression);

                                    manageExpressionPriceCalcs(optionSpreadExpression);
                                }

                                lastTimdBarsInIdx++;

                            }



                        }

                    }



                }

            }
            catch (Exception ex)
            {
                TSErrorCatch.errorCatchOut(Convert.ToString(this), ex);
            }
        }

        private void m_CEL_TimedBarsUpdated(CQG.CQGTimedBars cqg_TimedBarsIn, int index)
        {
            //Debug.WriteLine("m_CEL_ExpressionResolved" + cqg_expression.Count);
            try
            {

                if (optionSpreadExpressionHashTable_keyCQGInId.ContainsKey(cqg_TimedBarsIn.Id))
                {

                    OptionSpreadExpression optionSpreadExpression
                            = optionSpreadExpressionHashTable_keyCQGInId[cqg_TimedBarsIn.Id];


                    if (!optionSpreadExpression.stopUpdating
                        && optionSpreadExpression.futureTimedBars != null
                        && optionSpreadExpression.futureBarData != null
                        && optionSpreadExpression.futureBarData.Count > 0)
                    {
                        int futureBarDataCounter = optionSpreadExpression.futureBarData.Count - 1;

                        bool foundBar = false;

                        while (futureBarDataCounter >= 0)
                        {
                            if (cqg_TimedBarsIn[index].Timestamp.CompareTo(
                                optionSpreadExpression.futureBarData[futureBarDataCounter].barTime) == 0)
                            {
                                foundBar = true;
                                break;
                            }

                            futureBarDataCounter--;
                        }

                        if (foundBar)
                        {

                            //***********************************


                            OHLCData ohlcData = optionSpreadExpression.futureBarData[futureBarDataCounter];

                            bool error = false;

                            int volume = 0;

                            if (cqg_TimedBarsIn[index].ActualVolume
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                volume = cqg_TimedBarsIn[index].ActualVolume;
                            }
                            else
                            {
                                error = true;
                            }

                            ohlcData.volume = volume;

                            if (cqg_TimedBarsIn[index].Open
                                != -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                ohlcData.open = cqg_TimedBarsIn[index].Open;
                            }
                            else
                            {
                                error = true;
                            }

                            //**********************************************


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

                            ohlcData.errorBar = error;


                            //**********************************************




                            if (!error
                                && !optionSpreadExpression.reachedTransactionTimeBoundary
                                && ohlcData.barTime
                                .CompareTo(optionSpreadExpression.todayTransactionTimeBoundary) <= 0)
                            {
                                optionSpreadExpression.todayTransactionBar = ohlcData;
                            }

                            if (!error
                                && !optionSpreadExpression.reachedTransactionTimeBoundary
                                && ohlcData.barTime
                                .CompareTo(optionSpreadExpression.todayTransactionTimeBoundary) >= 0)
                            {
                                optionSpreadExpression.reachedTransactionTimeBoundary = true;
                            }

                            if (!error
                                && !optionSpreadExpression.reachedDecisionBar
                                && ohlcData.barTime
                                .CompareTo(optionSpreadExpression.todayDecisionTime) <= 0)
                            {
                                optionSpreadExpression.decisionBar = ohlcData;
                            }

                            if (!error
                                && !optionSpreadExpression.reachedDecisionBar
                                && ohlcData.barTime
                                .CompareTo(optionSpreadExpression.todayDecisionTime) >= 0)
                            {
                                optionSpreadExpression.reachedDecisionBar = true;
                            }

                            if (!error
                                && !optionSpreadExpression.reachedBarAfterDecisionBar
                                && ohlcData.barTime
                                .CompareTo(optionSpreadExpression.todayDecisionTime) > 0)
                            {
                                optionSpreadExpression.reachedBarAfterDecisionBar = true;
                            }

                            if (!error
                                && !optionSpreadExpression.reached1MinAfterDecisionBarUsedForSnapshot
                                && ohlcData.barTime
                                .CompareTo(optionSpreadExpression.todayDecisionTime.AddMinutes(1)) > 0)
                            {
                                optionSpreadExpression.reached1MinAfterDecisionBarUsedForSnapshot = true;
                            }


                            if (!optionSpreadExpression.futureBarData.Last().errorBar)
                            //!= -DataCollectionConstants.CQG_DATA_ERROR_CODE)
                            {
                                optionSpreadExpression.lastTimeFuturePriceUpdated =
                                    optionSpreadExpression.futureBarData.Last().barTime;

                                optionSpreadExpression.trade =
                                    optionSpreadExpression.futureBarData.Last().close;

                                fillDefaultMidPrice(optionSpreadExpression);

                                manageExpressionPriceCalcs(optionSpreadExpression);

                            }

                        }

                        //break;  //while (expressionCounter < optionSpreadExpressionList.Count)

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
                realtimeDataManagement.updateCQGDataStatus("CQG GOOD", Color.Black, Color.LawnGreen);


                if (optionSpreadExpressionHashTable_keySymbol.ContainsKey(symbol))
                {

                    OptionSpreadExpression optionSpreadExpression = 
                        optionSpreadExpressionHashTable_keySymbol[symbol];

                    //while (expressionCounter < optionSpreadExpressionList.Count)
                    //{
                    if (!optionSpreadExpression.stopUpdating
                        //&& symbol.CompareTo(optionSpreadExpressionList[expressionCounter].cqgSymbol) == 0
                        && !optionSpreadExpression.setSubscriptionLevel)
                    {
                        optionSpreadExpression.setSubscriptionLevel = true;

                        optionSpreadExpression.cqgInstrument = cqgInstrument;


                        //int idx = expressionCounter;

                        //optionSpreadExpressionListHashTableIdx.AddOrUpdate(
                        //        cqgInstrument.FullName, idx,
                        //        (oldKey, oldValue) => idx);

                        optionSpreadExpressionHashTable_keyFullName.AddOrUpdate(
                                cqgInstrument.FullName, optionSpreadExpression,
                                (oldKey, oldValue) => optionSpreadExpression);

                        //if (cqgInstrument.FullName.CompareTo("P.US.EU6J1511100") == 0)
                        //{
                        //    Console.WriteLine(cqgInstrument.FullName);
                        //}

                        fillPricesFromQuote(optionSpreadExpression,
                            optionSpreadExpression.cqgInstrument.Quotes);

                        //if is an option (not a future)
                        //if (optionSpreadExpression.callPutOrFuture !=
                        //        OPTION_SPREAD_CONTRACT_TYPE.FUTURE)
                        //{
                        //    fillDefaultMidPrice(optionSpreadExpression);

                        //    manageExpressionPriceCalcs(optionSpreadExpression);
                        //}


                        if (optionSpreadExpression.optionExpressionType
                            == OPTION_EXPRESSION_TYPES.SPREAD_LEG_PRICE)
                        {
                            //subscribes to futures data
                            optionSpreadExpression.cqgInstrument.DataSubscriptionLevel
                                = eDataSubscriptionLevel.dsQuotes;
                        }
                        else
                        {
                            //TSErrorCatch.debugWriteOut(symbol + " interest rate no update");

                            //set updates for interest rate to stop
                            optionSpreadExpression.cqgInstrument.DataSubscriptionLevel
                                = eDataSubscriptionLevel.dsNone;

                        }

                        //break;
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

                if (optionSpreadExpressionHashTable_keyFullName.ContainsKey(cqgInstrument.FullName))
                {

                    //optionSpreadExpressionCheckSubscribedListIdx

                    OptionSpreadExpression optionSpreadExpression 
                        = optionSpreadExpressionHashTable_keyFullName[cqgInstrument.FullName];

                    if (optionSpreadExpression != null
                        && !optionSpreadExpression.stopUpdating
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

        private void fillDefaultMidPrice(OptionSpreadExpression optionSpreadExpression)  //, Instrument instrument)
        {

            double defaultPrice = 0;

            if (optionSpreadExpression.optionExpressionType == OPTION_EXPRESSION_TYPES.OPTION_EXPRESSION_RISK_FREE_RATE)
            {
                if (optionSpreadExpression.tradeFilled)
                {
                    defaultPrice = optionSpreadExpression.trade;
                }
                else if (optionSpreadExpression.settlementFilled)
                {
                    defaultPrice = optionSpreadExpression.settlement;
                }
                else if (optionSpreadExpression.yesterdaySettlementFilled)
                {
                    defaultPrice = optionSpreadExpression.yesterdaySettlement;
                }

                if (defaultPrice == 0)
                {
                    defaultPrice = 0.01;
                }

                defaultPrice = defaultPrice == 0 ? 0 :
                    ((int)((100 - defaultPrice + DataCollectionConstants.EPSILON) * 1000) / 100000.0);

                optionSpreadExpression.riskFreeRate = defaultPrice;
            }
            else
            {

                optionSpreadExpression.lastTimeUpdated = optionSpreadExpression.lastTimeFuturePriceUpdated;

                TimeSpan span = DateTime.Now - optionSpreadExpression.lastTimeUpdated;

                optionSpreadExpression.minutesSinceLastUpdate = span.TotalMinutes;

                if (optionSpreadExpression.tradeFilled)
                {
                    defaultPrice = optionSpreadExpression.trade;
                }
                else if (optionSpreadExpression.settlementFilled)
                {
                    defaultPrice = optionSpreadExpression.settlement;
                }
                else if (optionSpreadExpression.yesterdaySettlementFilled)
                {
                    defaultPrice = optionSpreadExpression.yesterdaySettlement;
                }

                if (defaultPrice == 0)
                {
                    defaultPrice = DataCollectionConstants.OPTION_ZERO_PRICE;
                }


                //can set default price for futures here b/c no further price possibilities for future;
                optionSpreadExpression.defaultPrice = defaultPrice;

                optionSpreadExpression.defaultPriceFilled = true;

            }
        }

        public void manageExpressionPriceCalcs(OptionSpreadExpression optionSpreadExpression)
        {

            if (optionSpreadExpression.optionExpressionType == OPTION_EXPRESSION_TYPES.OPTION_EXPRESSION_RISK_FREE_RATE)
            {
                // fill all expressions with the latest interest rate

                foreach (KeyValuePair<string, OptionSpreadExpression> entry in optionSpreadExpressionHashTable_keyCQGInId)
                {
                    entry.Value.riskFreeRateFilled = true;

                    entry.Value.riskFreeRate = optionSpreadExpression.riskFreeRate;

                    //only update if subscribed
                    //if (entry.Value.setSubscriptionLevel)
                    //{
                    //    entry.Value.totalCalcsRefresh = CQG_REFRESH_STATE.DATA_UPDATED;
                    //}
                }
            }
            else
            {
                //if (optionSpreadExpression.callPutOrFuture == OPTION_SPREAD_CONTRACT_TYPE.FUTURE)
                {

                    optionSpreadExpression.impliedVol = 0;
                    optionSpreadExpression.delta = 1 * DataCollectionConstants.OPTION_DELTA_MULTIPLIER;

                    fillFutureDecisionAndTransactionPrice(optionSpreadExpression);

                    


                }



                //optionSpreadExpression.totalCalcsRefresh = CQG_REFRESH_STATE.DATA_UPDATED;
            }
        }

        public void fillFutureDecisionAndTransactionPrice(OptionSpreadExpression optionSpreadExpression)
        {
            if (optionSpreadExpression.decisionBar != null && optionSpreadExpression.todayTransactionBar != null)
            {

                optionSpreadExpression.decisionPrice =
                    optionSpreadExpression.decisionBar.close;

                optionSpreadExpression.decisionPriceTime =
                    optionSpreadExpression.decisionBar.barTime;

                optionSpreadExpression.decisionPriceFilled = true;

                //if (optionSpreadExpression.reachedDecisionBar)
                //{
                //    optionSpreadExpression.filledAfterReachedDecisionBar = true;
                //}




                optionSpreadExpression.transactionPrice =
                    optionSpreadExpression.todayTransactionBar.close;

                optionSpreadExpression.transactionPriceTime =
                    optionSpreadExpression.todayTransactionBar.barTime;


                if (optionSpreadExpression.reachedTransactionTimeBoundary)
                {
                    //optionSpreadExpression.filledAfterTransactionTimeBoundary = true;

                    optionSpreadExpression.transactionPriceFilled = true;

                    foreach (OptionSpreadExpression ose in optionSpreadExpression.optionExpressionsThatUseThisFutureAsUnderlying)
                    {
                        ose.transactionPriceFilled = true;
                    }

                }




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
