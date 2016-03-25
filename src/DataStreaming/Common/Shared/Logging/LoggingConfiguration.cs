// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Logging
{
    using System;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;

    /// <summary>
    /// Wrapper for reading data from Setting.xml in the Config package
    /// </summary>
    internal sealed class LoggingConfiguration
    {
        #region Constructors

        private LoggingConfiguration()
        {
            CodePackageActivationContext codeContext = FabricRuntime.GetActivationContext();
            if (codeContext == null) // sanity check
            {
                throw new ApplicationException("CodePackageActivationContext is null");
            }

            // let exceptions flow out, they will be traced downstream and the service won't start
            ConfigurationPackage configurationPackage = codeContext.GetConfigurationPackageObject(ConfigurationPackageName);

            // Read settings from configuration package
            this.InitializeSettingsFromConfigurationPackage(configurationPackage);
        }

        #endregion

        #region Configuration properties

        internal string Logger { get; private set; }

        #endregion

        #region Helpers

        internal void InitializeSettingsFromConfigurationPackage(ConfigurationPackage configurationPackage)
        {
            //Guard.NotNull(configurationPackage, nameof(configurationPackage));

            if (configurationPackage.Settings?.Sections == null ||
                !configurationPackage.Settings.Sections.Contains(ConfigurationSectionName))
            {
                // fallback to default setup of LoggerFactory
                return;
            }

            KeyedCollection<string, ConfigurationProperty> configurationParameters =
                configurationPackage.Settings.Sections[ConfigurationSectionName].Parameters;

            if (!configurationParameters.Contains(LoggerKey))
            {
                return;
            }

            this.Logger = configurationParameters[LoggerKey].Value;
        }

        #endregion

        #region Constants

        // Configuration package constants
        private const string ConfigurationPackageName = "Config";
        private const string ConfigurationSectionName = "Logging";
        private const string LoggerKey = "Logger";

        #endregion

        #region Singleton instance

        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<LoggingConfiguration> _instance = new Lazy<LoggingConfiguration>(
            () => new LoggingConfiguration()
            );

        internal static LoggingConfiguration Instance => _instance.Value;

        #endregion
    }
}