// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Logging
{
    using System;

    public class DelegateLogger : ILogger
    {
        private readonly Action<string> logMethod;
        private readonly string _logName;

        public DelegateLogger(string logName, Action<string> logMethod)
        {
            this._logName = "[" + (logName ?? string.Empty) + "] ";
            this.logMethod = logMethod;
        }

        public void Debug(string format, params object[] args)
        {
            this.logMethod(this._logName + " " + string.Format(format, args));
        }

        public void Info(string format, params object[] args)
        {
            this.logMethod(this._logName + " " + string.Format(format, args));
        }

        public void Warning(string format, params object[] args)
        {
            this.logMethod(this._logName + " " + string.Format(format, args));
        }

        public void Warning(Exception exception, string format, params object[] args)
        {
            this.logMethod(this._logName + " " + string.Format(format, args) + " " + exception);
        }

        public void Error(Exception exception, string format, params object[] args)
        {
            this.logMethod(this._logName + " " + string.Format(format, args) + " " + exception);
        }

        public void TraceApi(string method, TimeSpan timespan)
        {
        }

        public void TraceApi(string method, TimeSpan timespan, string format, params object[] args)
        {
        }
    }
}