// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Logging
{
    using System;

    /// <summary>
    /// Definition of a generic logging interface to abstract 
    /// implementation details. Includes api trace methods.
    /// Feel free to extend it with additional methods including e.g. tracing of activityId
    /// </summary>
    public interface ILogger
    {
        void Debug(string format, params object[] args);
        void Info(string format, params object[] args);
        void Warning(string format, params object[] args);
        void Warning(Exception exception, string format, params object[] args);
        void Error(Exception exception, string format, params object[] args);
    }
}