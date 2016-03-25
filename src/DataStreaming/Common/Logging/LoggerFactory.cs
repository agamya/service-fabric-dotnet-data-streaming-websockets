// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Logging
{
    using System;
    using System.Collections.Concurrent;

    public static class LoggerFactory
    {
        private static Func<string, ILogger> _defaultFactory = n => new TraceLogger(n);

        private static readonly ConcurrentDictionary<string, ILogger> _loggers =
            new ConcurrentDictionary<string, ILogger>();

        public static void SetFactory(Func<string, ILogger> defaultFactory)
        {
            if (null != defaultFactory)
            {
                _defaultFactory = defaultFactory;
            }
        }

        public static ILogger GetLogger()
        {
            return GetLogger(null);
        }

        public static ILogger GetLogger(string logName)
        {
            if (string.IsNullOrEmpty(logName))
            {
                logName = "Default";
            }

            return _loggers.GetOrAdd(logName, name => _defaultFactory(name));
        }
    }
}