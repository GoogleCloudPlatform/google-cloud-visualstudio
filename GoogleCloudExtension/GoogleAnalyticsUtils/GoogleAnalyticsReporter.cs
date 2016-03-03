// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAnalyticsUtils
{
    /// <summary>
    /// <para>
    /// Client for the the Google Analytics Measurement Protocol service, which makes
    /// HTTP requests to publish data to a Google Analytics account
    /// </para>
    /// 
    /// <para>
    /// For more information, see:
    /// https://developers.google.com/analytics/devguides/collection/protocol/v1/
    /// </para>
    /// </summary>
    public class AnalyticsReporter
    {
        private const string ProductionServerUrl = "https://ssl.google-analytics.com/collect";
        private const string DebugServerUrl = "https://ssl.google-analytics.com/debug/collect";

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

        private const string VersionValue = "1";
        private const string EventTypeValue = "event";
        private const string SessionStartValue = "start";
        private const string SessionEndValue = "end";

        private readonly bool _debug;
        private readonly string _serverUrl;
        private readonly Dictionary<string, string> _baseHitData;

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
        public AnalyticsReporter(
            string propertyId,
            string appName,
            string clientId = null,
            string appVersion = null,
            bool debug = false)
        {
            PropertyId = Preconditions.CheckNotNull(propertyId, nameof(propertyId));
            ApplicationName = Preconditions.CheckNotNull(appName, nameof(appName));
            ClientId = clientId ?? Guid.NewGuid().ToString();
            ApplicationVersion = appVersion;

            _debug = debug;
            _serverUrl = debug ? DebugServerUrl : ProductionServerUrl;
            _baseHitData = MakeBaseHitData();
        }

        /// <summary>
        /// Convenience method to report a singe event to Google Analytics.
        /// </summary>
        /// <param name="category">The category for the even.</param>
        /// <param name="action">The action that took place.</param>
        /// <param name="label">The label affected by the event.</param>
        /// <param name="value">The new value.</param>
        public void ReportEvent(string category, string action, string label, int? value = null)
        {
            // Data we will send along with the web request. Later baked into the HTTP
            // request's payload.
            var hitData = new Dictionary<string, string>(_baseHitData) {
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
            SendHitData(hitData);
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
            SendHitData(hitData);
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
            SendHitData(hitData);
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

        /// <summary>
        /// Sends the hit data to the server.
        /// </summary>
        /// <param name="hitData">The hit data to be sent.</param>
        private void SendHitData(Dictionary<string, string> hitData)
        {
            var values = new NameValueCollection();
            foreach (var entry in hitData)
            {
                values.Add(entry.Key, entry.Value);
            }

            try
            {
                var client = new WebClient();
                DebugPrintAnalyticsOutput(client.UploadValuesTaskAsync(_serverUrl, values));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to report analytics. {ex.Message}");
            }
        }

        /// <summary>
        /// Debugging utility that will print out to the output window the result of the hit request.
        /// </summary>
        /// <param name="resultTask">The task resulting from the request.</param>
        [Conditional("DEBUG")]
        private async void DebugPrintAnalyticsOutput(Task<byte[]> resultTask)
        {
            var result = await resultTask;
            var decoded = Encoding.UTF8.GetString(result);
            Debug.WriteLine($"Output of analytics: {decoded}");
        }
    }
}
