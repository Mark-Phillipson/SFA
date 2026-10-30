using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using SFA_RazorClassLibrary.Services;
using SFA_WebAPI.Services;
using Xunit;

namespace SFA_PWA.Tests
{
    public class CalendarFeedParsingTests
    {
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
            public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request));
            }
        }

        private static DateTime NextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            if (daysToAdd == 0) daysToAdd = 7; // ensure next
            return start.Date.AddDays(daysToAdd);
        }

        [Fact]
        public async Task Gcal_Response_Populates_OriginalDescription_When_Location_Empty()
        {
            // Arrange: return a JSON array from the gcal endpoint with description but no location
            var nextWed = NextWeekday(DateTime.UtcNow, DayOfWeek.Wednesday).AddHours(10);
            string json = $"[{{\"start\": \"{nextWed:yyyy-MM-ddTHH:mm:ssZ}\", \"summary\": \"Midweek Ride\", \"location\": \"\", \"description\": \"Meet at The Cafe\", \"links\": [\"http://route.example/\"]}}]";

            var handler = new TestHttpMessageHandler(req =>
            {
                var uri = req.RequestUri?.AbsoluteUri ?? string.Empty;
                if (uri.Contains("api/calendar/gcal"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var svc = new CalendarFeedService(client, new BotApiConfig { BotApiUrl = string.Empty });

            var groups = new List<(string Name, string CalendarUrl)> { ("TestGroup", "https://calendar.google.com/calendar/embed?src=test@group.calendar.google.com&ctz=Europe%2FLondon") };

            // Act
            var events = await svc.GetUpcomingWedSatForGroupsAsync(groups);

            // Assert
            Assert.NotNull(events);
            Assert.NotEmpty(events);
            var ev = events[0];
            Assert.Equal("TestGroup", ev.GroupName);
            Assert.Equal(string.Empty, ev.Location);
            Assert.False(string.IsNullOrWhiteSpace(ev.OriginalDescription));
            Assert.Contains("Meet at The Cafe", ev.OriginalDescription);
        }

        [Fact]
        public async Task Gcal_Response_Uses_Location_From_Title_When_Location_Field_Empty()
        {
            var nextWed = NextWeekday(DateTime.UtcNow, DayOfWeek.Wednesday).AddHours(10);
            string json = $"[{{\"start\": \"{nextWed:yyyy-MM-ddTHH:mm:ssZ}\", \"summary\": \"Midweek Ride - The Cafe\", \"location\": \"\", \"description\": \"\"}}]";

            var handler = new TestHttpMessageHandler(req =>
            {
                var uri = req.RequestUri?.AbsoluteUri ?? string.Empty;
                if (uri.Contains("api/calendar/gcal"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var svc = new CalendarFeedService(client, new BotApiConfig { BotApiUrl = string.Empty });
            var groups = new List<(string Name, string CalendarUrl)> { ("TestGroup", "https://calendar.google.com/calendar/embed?src=test@group.calendar.google.com&ctz=Europe%2FLondon") };

            var events = await svc.GetUpcomingWedSatForGroupsAsync(groups);

            Assert.NotNull(events);
            Assert.NotEmpty(events);
            Assert.Equal("The Cafe", events[0].Location);
        }

        [Fact]
        public async Task Ics_Response_Populates_OriginalDescription_When_Location_Empty()
        {
            // Arrange: return ICS text with DESCRIPTION containing meeting info
            var nextSat = NextWeekday(DateTime.UtcNow, DayOfWeek.Saturday).AddHours(9);
            string dt = nextSat.ToString("yyyyMMdd'T'HHmmss'Z'");
            string ics = $"BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nDTSTART:{dt}\r\nSUMMARY:Weekend Ride\r\nDESCRIPTION:Meet at The Village Cafe\\nLocation: Village Square\r\nEND:VEVENT\r\nEND:VCALENDAR";

            var handler = new TestHttpMessageHandler(req =>
            {
                var uri = req.RequestUri?.AbsoluteUri ?? string.Empty;
                if (uri.Contains("api/calendar/gcal"))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                if (uri.Contains("api/calendar/ics"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(ics, Encoding.UTF8, "text/plain")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var svc = new CalendarFeedService(client, new BotApiConfig { BotApiUrl = string.Empty });

            var groups = new List<(string Name, string CalendarUrl)> { ("TestGroupIcs", "https://example.com/calendar.ics") };

            // Act
            var events = await svc.GetUpcomingWedSatForGroupsAsync(groups);

            // Assert
            Assert.NotNull(events);
            Assert.NotEmpty(events);
            var ev = events[0];
            Assert.Equal("TestGroupIcs", ev.GroupName);
            Assert.False(string.IsNullOrWhiteSpace(ev.OriginalDescription));
            Assert.Contains("Meet at The Village Cafe", ev.OriginalDescription);
        }

        [Fact]
        public async Task Ics_Response_Uses_Location_From_Title_When_Location_And_Description_Location_Missing()
        {
            var nextSat = NextWeekday(DateTime.UtcNow, DayOfWeek.Saturday).AddHours(9);
            string dt = nextSat.ToString("yyyyMMdd'T'HHmmss'Z'");
            string ics = $"BEGIN:VCALENDAR\r\nBEGIN:VEVENT\r\nDTSTART:{dt}\r\nSUMMARY:Weekend Ride @ Village Square\r\nDESCRIPTION:Bring lights and pump\r\nEND:VEVENT\r\nEND:VCALENDAR";

            var handler = new TestHttpMessageHandler(req =>
            {
                var uri = req.RequestUri?.AbsoluteUri ?? string.Empty;
                if (uri.Contains("api/calendar/gcal"))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                if (uri.Contains("api/calendar/ics"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(ics, Encoding.UTF8, "text/plain")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var svc = new CalendarFeedService(client, new BotApiConfig { BotApiUrl = string.Empty });
            var groups = new List<(string Name, string CalendarUrl)> { ("TestGroupIcs", "https://example.com/calendar.ics") };

            var events = await svc.GetUpcomingWedSatForGroupsAsync(groups);

            Assert.NotNull(events);
            Assert.NotEmpty(events);
            Assert.Equal("Village Square", events[0].Location);
        }

        [Fact]
        public async Task B_Group_Uses_Default_Departure_Note_When_Location_Missing()
        {
            var nextSat = NextWeekday(DateTime.UtcNow, DayOfWeek.Saturday).AddHours(10);
            string json = $"[{{\"start\": \"{nextSat:yyyy-MM-ddTHH:mm:ssZ}\", \"summary\": \"B Ride - Leader Jane Doe\", \"location\": \"\", \"description\": \"\"}}]";

            var handler = new TestHttpMessageHandler(req =>
            {
                var uri = req.RequestUri?.AbsoluteUri ?? string.Empty;
                if (uri.Contains("api/calendar/gcal"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var svc = new CalendarFeedService(client, new BotApiConfig { BotApiUrl = string.Empty });
            var groups = new List<(string Name, string CalendarUrl)> { ("B Group", "https://calendar.google.com/calendar/embed?src=test@group.calendar.google.com&ctz=Europe%2FLondon") };

            var events = await svc.GetUpcomingWedSatForGroupsAsync(groups);

            Assert.NotNull(events);
            Assert.NotEmpty(events);
            Assert.Equal("(normally leaves from Marden Library Carpark)", events[0].Location);
        }

        [Fact]
        public void Sample_Questions_Include_Club_History_And_Saturday_Ride_Examples()
        {
            var questions = OpenAIBotService.GetDefaultSampleQuestions();

            Assert.Contains(questions, q => q.Contains("club history", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(questions, q => q.Contains("Saturday", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(questions, q => q.Contains("B ride", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(questions, q => q.Contains("membership", StringComparison.OrdinalIgnoreCase));
        }
    }
}
