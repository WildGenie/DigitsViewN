﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Windows.Threading;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Diagnostics;

namespace DigitsViewLib
{
    public class ShotclockRetriever
    {
        private Action<ShotclockResponse> theDelegate;
        private System.Timers.Timer theTimer;
        private string theURL;
        private const int theRequestTimeout = 1000;
        private const int theMaxRetry = 5;
        private int theRetryCount;

        private Dispatcher theDispatcher;

        public ShotclockRetriever(Dispatcher aDispatcher, Action<ShotclockResponse> aDelegate, long aTime, string aURL)
        {
            theDispatcher = aDispatcher;
            theDelegate = aDelegate;
            theURL = aURL;

            theTimer = new System.Timers.Timer(aTime);

            theTimer.Elapsed += TimerProc;
            theTimer.Enabled = true;

        }

        ~ShotclockRetriever()
        {
            theTimer.Enabled = false;
        }

        private void TimerProc(Object source, ElapsedEventArgs ea)
        {
            theTimer.Enabled = false;

            try
            {
                HttpWebRequest request = WebRequest.Create(theURL) as HttpWebRequest;
                request.Timeout = theRequestTimeout;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format(
                        "Server error (HTTP {0}: {1}).",
                        response.StatusCode,
                        response.StatusDescription));
                    DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ShotclockResponse));
                    object objResponse = jsonSerializer.ReadObject(response.GetResponseStream());
                    ShotclockResponse jsonResponse = objResponse as ShotclockResponse;

                    theDispatcher.BeginInvoke(DispatcherPriority.Send, theDelegate, jsonResponse);
                }
                theRetryCount = 0;
            }
            catch (Exception e)
            {
                Logger.Log("Error retrieving data: " + e.Message, EventLogEntryType.Error, Logger.Type.RetrieveFailure);
                if (theRetryCount < theMaxRetry)
                {
                    theRetryCount++;
                }
                else
                {
                    theDispatcher.BeginInvoke(DispatcherPriority.Send, theDelegate, null);
                }

            }

            theTimer.Enabled = true;
 
        }
    }
}
