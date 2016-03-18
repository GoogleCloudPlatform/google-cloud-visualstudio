// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.DataSources.Models;
using System;
using System.ComponentModel;

namespace GoogleCloudExtension.CloudExplorerSources.AppEngine
{
    /// <summary>
    /// This class provides read-only access to the properties in the ModuleAndVersion
    /// class. Together with the documentation for those properties.
    /// </summary>
    internal class VersionItem
    {
        private const string AppEngineCategory = "AppEngine Properties";

        private readonly GaeVersion _version;
        private readonly double _trafficSplit;

        internal VersionItem(GaeVersion version, double trafficSplit)
        {
            _version = version;
            _trafficSplit = trafficSplit;
        }

        #region Properties to be displayed in the properties window

        [Description("The amount of traffic assigned to this version")]
        [Category(AppEngineCategory)]
        public string TrafficSplit => string.Format("{0:0.00}%", _trafficSplit * 100.0);

        [Description("The version id")]
        [Category(AppEngineCategory)]
        public string Id => _version.Id;

        [Description("The time when this version was deployed.")]
        [Category(AppEngineCategory)]
        public DateTime CreationTime => _version.CreationTime;

        [Description("The email of the user who deployed this version.")]
        [Category(AppEngineCategory)]
        public string Deployer => _version.Deployer;

        [Description("The serving status for this version.")]
        [Category(AppEngineCategory)]
        public string ServingStatus => _version.ServingStatus;

        #endregion
    }
}
