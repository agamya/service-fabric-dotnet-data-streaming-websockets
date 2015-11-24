// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared
{
    using System.Fabric;
    using System.Fabric.Description;
    using System.Linq;

    public static class ConfigurationHelper
    {
        public static string ReadValue(string sectionName, string parameterName)
        {
            CodePackageActivationContext context = FabricRuntime.GetActivationContext();
            ConfigurationSettings configSettings = context.GetConfigurationPackageObject("Config").Settings;
            ConfigurationSection configSection = configSettings.Sections.FirstOrDefault(s => (s.Name == sectionName));

            ConfigurationProperty configurationProperty = configSection?.Parameters.FirstOrDefault(p => (p.Name == parameterName));
            return configurationProperty?.Value;
        }
    }
}