﻿using System;
using Cronofy.Requests;
using NUnit.Framework;

namespace Cronofy.Test.CronofyAccountClientTests
{
    internal sealed class Batch : Base
    {
        [Test]
        public void CanUpsertEvent()
        {
            this.Http.Stub(
                HttpPost
                    .Url("https://api.cronofy.com/v1/batch")
                    .RequestHeader("Authorization", "Bearer " + AccessToken)
                    .JsonRequest(@"
                        {
                            ""batch"": [
                                {
                                    ""method"": ""POST"",
                                    ""relative_url"": ""/v1/calendars/cal_n23kjnwrw2_jsdfjksn234/events"",
                                    ""data"": {
                                        ""event_id"": ""qTtZdczOccgaPncGJaCiLg"",
                                        ""summary"": ""Board meeting"",
                                        ""description"": ""Discuss plans for the next quarter."",
                                        ""start"": { ""time"": ""2014-08-05 15:30:00Z"", ""tzid"": ""Etc/UTC"" },
                                        ""end"": { ""time"": ""2014-08-05 17:00:00Z"", ""tzid"": ""Etc/UTC"" },
                                        ""location"": {
                                            ""description"": ""Board room""
                                        }
                                    }
                                }
                            ]
                        }
                    ")
                    .ResponseCode(207)
                    .ResponseBody(@"
                        {
                            ""batch"": [
                                { ""status"": 202 }
                            ]
                        }
                    ")
            );

            var batchBuilder = new BatchRequestBuilder();

            var upsertRequest = new UpsertEventRequestBuilder()
                .EventId("qTtZdczOccgaPncGJaCiLg")
                .Summary("Board meeting")
                .Description("Discuss plans for the next quarter.")
                .Start(new DateTime(2014, 8, 5, 15, 30, 0, DateTimeKind.Utc))
                .End(new DateTime(2014, 8, 5, 17, 0, 0, DateTimeKind.Utc))
                .Location("Board room")
                .Build();

            batchBuilder.UpsertEvent("cal_n23kjnwrw2_jsdfjksn234", upsertRequest);

            var response = this.Client.BatchRequest(batchBuilder);

            Assert.AreEqual(202, response.Batch[0].Status);

            var expectedRequestEntry = new BatchRequest.EntryBuilder()
                .Method("POST")
                .RelativeUrl("/v1/calendars/cal_n23kjnwrw2_jsdfjksn234/events")
                .Data(upsertRequest)
                .Build();

            Assert.AreEqual(expectedRequestEntry, response.Batch[0].Request);
        }

        [Test]
        public void CanDeleteEvent()
        {
            this.Http.Stub(
                HttpPost
                    .Url("https://api.cronofy.com/v1/batch")
                    .RequestHeader("Authorization", "Bearer " + AccessToken)
                    .JsonRequest(@"
                        {
                            ""batch"": [
                                {
                                    ""method"": ""DELETE"",
                                    ""relative_url"": ""/v1/calendars/cal_n23kjnwrw2_jsdfjksn234/events"",
                                    ""data"": {
                                        ""event_id"": ""qTtZdczOccgaPncGJaCiLg""
                                    }
                                }
                            ]
                        }
                    ")
                    .ResponseCode(207)
                    .ResponseBody(@"
                        {
                            ""batch"": [
                                { ""status"": 202 }
                            ]
                        }
                    ")
            );

            var batchBuilder = new BatchRequestBuilder();

            batchBuilder.DeleteEvent("cal_n23kjnwrw2_jsdfjksn234", "qTtZdczOccgaPncGJaCiLg");

            var response = this.Client.BatchRequest(batchBuilder);

            Assert.AreEqual(202, response.Batch[0].Status);

            var expectedRequestEntry = new BatchRequest.EntryBuilder()
                .Method("DELETE")
                .RelativeUrl("/v1/calendars/cal_n23kjnwrw2_jsdfjksn234/events")
                .Data(new DeleteEventRequest { EventId = "qTtZdczOccgaPncGJaCiLg" })
                .Build();

            Assert.AreEqual(expectedRequestEntry, response.Batch[0].Request);
        }
    }
}
