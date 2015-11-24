// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Logging
{
    using System;
    using System.Collections.Generic;

    public class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers;

        public CompositeLogger()
        {
            this._loggers = new List<ILogger>();
        }

        public CompositeLogger(params ILogger[] loggers)
        {
            this._loggers = new List<ILogger>(loggers);
        }

        public void Debug(string fmt, params object[] vars)
        {
            this._loggers.ForEach(l => l.Debug(fmt, vars));
        }

        public void Info(string fmt, params object[] vars)
        {
            this._loggers.ForEach(l => l.Info(fmt, vars));
        }

        public void Warning(string fmt, params object[] vars)
        {
            this._loggers.ForEach(l => l.Warning(fmt, vars));
        }

        public void Warning(Exception exception, string fmt, params object[] args)
        {
            this._loggers.ForEach(l => l.Warning(exception, fmt, args));
        }

        public void Error(Exception exception, string fmt, params object[] args)
        {
            this._loggers.ForEach(l => l.Error(exception, fmt, args));
        }
    }
}