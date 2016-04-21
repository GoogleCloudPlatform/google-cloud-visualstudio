// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Google.Apis.CloudResourceManager.v1.Data;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorer
{
    public interface ICloudExplorerSource
    {
        /// <summary>
        /// Returns the root of the hierarchy for this source.
        /// </summary>
        TreeHierarchy Root { get; }

        /// <summary>
        /// Returns the buttons, if any, defined by the source.
        /// </summary>
        IEnumerable<ButtonDefinition> Buttons { get; }

        /// <summary>
        /// Set to the current project selected by the user.
        /// </summary>
        Project CurrentProject { get; set; }

        /// <summary>
        /// Called when the sources need to reload their data.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Called when the credentials or project changes.
        /// </summary>
        void InvalidateCredentials();
    }
}
