// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Logging
{
    using System;

    internal static class StringEx
    {
        internal static string SafeFormat(string fmt, params object[] args)
        {
            if (fmt == null)
            {
                fmt = string.Empty;
            }

            if (args != null && args.Length > 0)
            {
                try
                {
                    fmt = string.Format(fmt, args);
                }
                catch (Exception)
                {
                    // we don't want a bad logging string to ruin the whole app
                }
            }

            return fmt;
        }
    }
}