// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;

namespace GoogleAnalyticsUtils
{
    /// <summary>
    /// <para>
    /// Client for the the Google Analytics Measurement Protocol service, which makes
    /// HTTP requests to publish data to a Google Analytics account
    /// </para>
    /// 
    /// <para>
    /// </para>
    /// </summary>
    public class AnalyticsReporter : IAnalyticsReporter
    {
        private const string HitTypeParam = "t";
        private const string VersionParam = "v";
        private const string EventCategoryParam = "ec";
        private const string EventActionParam = "ea";
        private const string EventLabelParam = "el";
        private const string EventValueParam = "ev";
        private const string SessionControlParam = "sc";
        private const string PropertyIdParam = "tid";
        private const string ClientIdParam = "cid";
        private const string AppNameParam = "an";
        private const string AppVersionParam = "av";
        private const string ScreenNameParam = "cd";
        private const string DocumentTitleParam = "dt";
        private const string DocumentPathParam = "dp";
        private const string DocumentHostNameParam = "dh";

        private const string VersionValue = "1";
        private const string EventTypeValue = "event";
        private const string PageViewValue = "pageView";
        private const string SessionStartValue = "start";
        private const string SessionEndValue = "end";
        private const string ScreenViewValue = "screenview";

        private readonly Dictionary<string, string> _baseHitData;
        private readonly IHitSender _hitSender;

        /// <summary>
        /// The name of the application to use when reporting data.
        /// </summary>
        public string ApplicationName { get; }

        /// <summary>
        /// The version to use when reporting data.
        /// </summary>
        public string ApplicationVersion { get; }

        /// <summary>
        /// The property ID being used by this reporter.
        /// </summary>
        public string PropertyId { get; }

        /// <summary>
        /// The client ID being used by this reporter.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="propertyId">The property ID to use, string with the format US-XXXX. Must not be null.</param>
        /// <param name="appName">The name of the app for which this reporter is reporting. Must not be null.</param>
        /// <param name="clientId">The client id to use when reporting, if null a new random Guid will be generated.</param>
        /// <param name="appVersion">Optional, the app version. Defaults to null.</param>
        /// <param name="debug">Optional, whether this reporter is in debug mode. Defaults to false.</param>
        /// <param name="userAgent">Optiona, the user agent to use for all HTTP requests.</param>
        /// <param name="sender">The instance of <seealso cref="IHitSender"/> to use to send the this.</param>
        public AnalyticsReporter(
            string propertyId,
            string appName,
            string clientId = null,
            string appVersion = null,
            bool debug = false,
            string userAgent = null,
            IHitSender sender = null)
        {
            PropertyId = Preconditions.CheckNotNull(propertyId, nameof(propertyId));
            ApplicationName = Preconditions.CheckNotNull(appName, nameof(appName));
            ClientId = clientId ?? Guid.NewGuid().ToString();
            ApplicationVersion = appVersion;

            _baseHitData = MakeBaseHitData();
            _hitSender = sender ?? new HitSender(debug, userAgent);
        }

        /// <summary>
        /// Convenience method to report a single event to Google Analytics.
        /// </summary>
        /// <param name="category">The category for the event.</param>
        /// <param name="action">The action that took place.</param>
        /// <param name="label">The label affected by the event.</param>
        /// <param name="value">The new value.</param>
        public void ReportEvent(string category, string action, string label = null, int? value = null)
        {
            Preconditions.CheckNotNull(category, nameof(category));
            Preconditions.CheckNotNull(action, nameof(action));

            // Data we will send along with the web request. Later baked into the HTTP
            // request's payload.
            var hitData = new Dictionary<string, string>(_baseHitData)
            {
                { HitTypeParam, EventTypeValue },
                { EventCategoryParam, category },
                { EventActionParam, action },
            };
            if (label != null)
            {
                hitData[EventLabelParam] = label;
            }
            if (value != null)
            {
                hitData[EventValueParam] = value.ToString();
            }
            _hitSender.SendHitData(hitData);
        }

        /// <summary>
        /// Reports a page view hit to analytics.
        /// </summary>
        /// <param name="page">The URL to the page.</param>
        /// <param name="title">The page title.</param>
        /// <param name="host">The page host name.</param>
        /// <param name="customDimensions">Custom dimensions to add to the hit.</param>
        public void ReportPageView(
            string page,
            string title,
            string host,
            Dictionary<int, string> customDimensions = null)
        {
            Preconditions.CheckNotNull(page, nameof(page));

            var hitData = new Dictionary<string, string>(_baseHitData)
            {
                { HitTypeParam, PageViewValue },
                { DocumentPathParam, page },
            };

            if (title != null)
            {
                hitData[DocumentTitleParam] = title;
            }
            if (host != null)
            {
                hitData[DocumentHostNameParam] = host;
            }
            if (customDimensions != null)
            {
                foreach (var entry in customDimensions)
                {
                    hitData[GetCustomDimension(entry.Key)] = entry.Value;
                }
            }

            _hitSender.SendHitData(hitData);
        }

        /// <summary>
        /// Reports a window view.
        /// </summary>
        /// <param name="name">The name of the window. Must not be null.</param>
        public void ReportScreen(string name)
        {
            Preconditions.CheckNotNull(name, nameof(name));

            var hitData = new Dictionary<string, string>(_baseHitData)
            {
                { HitTypeParam, ScreenViewValue },
                { ScreenNameParam, name },
            };
            _hitSender.SendHitData(hitData);
        }

        /// <summary>
        /// Reports that the session is starting.
        /// </summary>
        public void ReportStartSession()
        {
            var hitData = new Dictionary<string, string>(_baseHitData)
            {
                { HitTypeParam,EventTypeValue },
                { SessionControlParam, SessionStartValue }
            };
            _hitSender.SendHitData(hitData);
        }

        /// <summary>
        /// Reports that the session is ending.
        /// </summary>
        public void ReportEndSession()
        {
            var hitData = new Dictionary<string, string>(_baseHitData)
            {
                { HitTypeParam, EventTypeValue },
                { SessionControlParam, SessionEndValue }
            };
            _hitSender.SendHitData(hitData);
        }

        /// <summary>
        /// Constructs the dictionary with the common parameters that all requests must
        /// have.
        /// </summary>
        /// <returns>Dictionary with the parameters for the report request.</returns>
        private Dictionary<string, string> MakeBaseHitData()
        {
            var result = new Dictionary<string, string>
            {
                { VersionParam, VersionValue },
                { PropertyIdParam, PropertyId },
                { ClientIdParam, ClientId },
                { AppNameParam, ApplicationName },
            };
            if (ApplicationVersion != null)
            {
                result.Add(AppVersionParam, ApplicationVersion);
            }
            return result;
        }

        private static string GetCustomDimension(int index) => $"cd{index}";
    }
}
