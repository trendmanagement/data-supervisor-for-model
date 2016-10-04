using System;
using System.Drawing;

namespace DataSupervisorForModel
{
    public enum STATUS_TYPE
    {
        NO_STATUS,
        CQG_CONNECTION_STATUS,
        DATA_SUBSCRIPTION_STATUS,
        DATA_STATUS

    }

    public enum STATUS_FORMAT
    {
        DEFAULT,
        ALARM,
        CAUTION
    }

    /// <summary>
    /// This helper class carries three roles for asynchronous tasks:
    /// </summary>
    /// 
    static class AsyncTaskListener
    {
        public delegate void UpdateDelegate(string msg = null, int count = -1, double rps = double.NaN);
        public static event UpdateDelegate Updated;

        public delegate void UpdateStatusDelegate (string msg = null,
            STATUS_FORMAT statusFormat = STATUS_FORMAT.DEFAULT,
            STATUS_TYPE connStatus = STATUS_TYPE.NO_STATUS);
        public static event UpdateStatusDelegate UpdatedStatus;

        public delegate void UpdateExpressionGridDelegate(OptionSpreadExpression ose = null);
        public static event UpdateExpressionGridDelegate UpdateExpressionGrid;

        public delegate void UpdateCQGDataManagementDelegate();
        public static event UpdateCQGDataManagementDelegate UpdateCQGDataManagement;

        // The period of RPS calculation and reporting (in seconds)
        static TimeSpan period = new TimeSpan(0, 0, 1);

        static DateTime notchTime;
        static int notchCount;



        public static InSetupAndConnectionMode _InSetupAndConnectionMode = new InSetupAndConnectionMode();

        /// <summary>
        /// This value is initially set to true. It is used on startup to tell the system it is ok to connect to Mongodb
        /// Once CQG connection and db setup this value is set to false
        /// </summary>
        public class InSetupAndConnectionMode
        {
            public bool value = true;
        }

        internal static void Set_InSetupAndConnectionMode(bool valueIn)
        {
            lock (_InSetupAndConnectionMode)
            {
                _InSetupAndConnectionMode.value = valueIn;

            }
        }


        public static void InitAsync(string msg = null)
        {
            notchTime = DateTime.Now;
            notchCount = 0;

            if (msg != null)
            {
                // Update text box
                Updated.Invoke(msg);
            }
        }

        public static void UpdateAsync(int count = -1, string msg = null)
        {
            if (count != -1)
            {
                DateTime now = DateTime.Now;
                TimeSpan delta = now - notchTime;
                if (delta >= period)
                {
                    // It's time to calculate RPS
                    double rps = (count - notchCount) / delta.TotalSeconds;

                    // Update text box, progress bar and RPS indicator
                    Updated.Invoke(msg, count, rps);

                    // Reset the measurer
                    notchTime = now;
                    notchCount = count;
                }
                else
                {
                    // Update text box and progress bar
                    Updated.Invoke(msg, count);
                }
            }
            else if (msg != null)
            {
                // Update text box
                Updated.Invoke(msg);
            }
        }

        public static void LogMessageAsync(string msg)
        {
            // Update text box
            Updated.Invoke(msg);
        }

        public static void LogMessageFormatAsync(string msgPat, params object[] args)
        {
            // Update text box
            Updated.Invoke(string.Format(msgPat, args));
        }

        public static void StatusUpdateAsync(string msg, 
            STATUS_FORMAT statusFormat, STATUS_TYPE connStatus)
        {
            // Update status strip
            UpdatedStatus.Invoke(msg, statusFormat, connStatus);
        }

        public static void ExpressionListUpdateAsync(OptionSpreadExpression ose)
        {
            // Update status strip
            UpdateExpressionGrid.Invoke(ose);
        }

        
        public static void UpdateCQGDataManagementAsync()
        {
            // Update status strip
            UpdateCQGDataManagement.Invoke();
        }
    }
}
