using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cronofy;
using Cronofy.Requests;
using Cronofy.Responses;
using Newtonsoft.Json;
using static Cronofy.CronofyAccountClient;

namespace Cronofy
{
    /// <summary>
    /// A thin wrapper around an HttpClient
    /// </summary>
    public class CronofyHttpClient
    {

        private static readonly JsonSerializerSettings DefaultSerializerSettings =
            new JsonSerializerSettings{NullValueHandling = NullValueHandling.Ignore};

        private static readonly JsonSerializerSettings DefaultDeserializerSettings =
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None };

        private readonly string _AccountToken;
        private readonly HttpClient _HttpClient;
        private readonly string _ApiBaseUrl;

        /// <summary>
        /// Creates a new CronofyHttpClient
        /// </summary>
        public CronofyHttpClient(string accountToken, HttpClient httpClient, string apiBaseUrl)
        {
            _AccountToken = accountToken;
            _HttpClient = httpClient;
            _ApiBaseUrl = apiBaseUrl;
        }

        private static string FormatQueryKey(string key)
        {
            if(key.Contains("[]")){
                return key.Split("[]")+"[]";
            }else{
                return key;
            }
        }

        /// <summary>
        /// Sends a generic request
        /// </summary>
        public async Task<T> SendAsync<T>(
            HttpMethod method, string path, object data, string token, CancellationToken cancel)
        {

            var uri=path.ToLower().StartsWith("https://")?path:_ApiBaseUrl+path;

            var queryParams=data as Dictionary<string,string>;
            if(queryParams!=null){
                data=null;
                uri+='?'+string.Join('&',queryParams.Select(p=>
                    FormatQueryKey(Uri.EscapeDataString(p.Key))+'='+Uri.EscapeDataString(p.Value)));
            }

            var request=new HttpRequestMessage(method,uri);

            request.Headers.Add("Authorization",$"Bearer {token}");

            if(data!=null){
                request.Content=new StringContent(
                    JsonConvert.SerializeObject(data,DefaultSerializerSettings),
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            var response=await _HttpClient.SendAsync(request,cancel);

            var json=await response.Content.ReadAsStringAsync(cancel);

            if(!response.IsSuccessStatusCode){
                throw new CronofyException(json);
            }

            if(typeof(object).Equals(typeof(T))){
                return (T)(object)null;
            }

            return JsonConvert.DeserializeObject<T>(json,DefaultDeserializerSettings);
        }

        /// <summary>
        /// Get a list of AvailablePeriods
        /// </summary>
        public async Task<List<AvailablePeriod>> GetAccountAvailabilityAsync(
            AvailabilityRequest availabilityRequest, CancellationToken cancel)
        {
            var r=await SendAsync<AvailabilityResponse>(
                HttpMethod.Post,"availability",availabilityRequest,_AccountToken,cancel);

            return r.AvailablePeriods.Select(p=>p.ToAvailablePeriod()).ToList();
        }

        /// <summary>
        /// Get a list of AvailablePeriods
        /// </summary>
        public async Task<List<AvailablePeriod>> GetAccountAvailabilityAsync(
            IBuilder<AvailabilityRequest> builder, CancellationToken cancel)
        {
            return await GetAccountAvailabilityAsync(builder.Build(),cancel);
        }

        /// <summary>
        /// Returns all calendars for an account
        /// </summary>
        public async Task<IEnumerable<Calendar>> GetCalendarsAsync(CancellationToken cancel)
        {
            var response=await SendAsync<CalendarsResponse>(
                HttpMethod.Get,"calendars",null,_AccountToken,cancel);

            return response.Calendars.Select(c => c.ToCalendar());
        }

        /// <summary>
        /// Returns all profiles for a given account
        /// </summary>
        public async Task<IEnumerable<Profile>> GetProfilesAsync(CancellationToken cancel)
        {
            var response = await SendAsync<ProfilesResponse>(
                HttpMethod.Get,"profiles",null,_AccountToken,cancel);

            return response.Profiles.Select(p => p.ToProfile());
        }

        /// <summary>
        /// Returns account detail information
        /// </summary>
        public async Task<Account> GetAccountAsync(CancellationToken cancel)
        {
            var response = await SendAsync<AccountResponse>(
                HttpMethod.Get,"account",null,_AccountToken,cancel);

            return response.ToAccount();
        }

        /// <summary>
        /// Returns account user info
        /// </summary>
        public async Task<UserInfo> GetUserInfoAsync(CancellationToken cancel)
        {
            var response = await SendAsync<UserInfoResponse>(
                HttpMethod.Get,"userinfo",null,_AccountToken,cancel);

            return response.ToUserInfo();
        }


        /// <inheritdoc/>
        public async Task<Channel> CreateChannelAsync(IBuilder<CreateChannelRequest> channelBuilder, CancellationToken cancel)
        {
            return await CreateChannelAsync(channelBuilder.Build(),cancel);
        }

        /// <inheritdoc/>
        public async Task<Channel> CreateChannelAsync(CreateChannelRequest channelRequest, CancellationToken cancel)
        {
            var response = await SendAsync<ChannelResponse>(
                HttpMethod.Post,"channels",channelRequest,_AccountToken,cancel);

            return response.ToChannel();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Channel>> GetChannelsAsync(CancellationToken cancel)
        {
            var response = await SendAsync<ChannelsResponse>(
                HttpMethod.Get,"channels",null,_AccountToken,cancel);

            return response.Channels.Select(c => c.ToChannel());
        }

        /// <inheritdoc/>
        public async Task CloseChannelAsync(string channelId, CancellationToken cancel)
        {
            await SendAsync<object>(
                HttpMethod.Delete,"channels/"+channelId,null,_AccountToken,cancel);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<Event> GetEventsAsync(CancellationToken cancel)
        {
            return GetEventsAsync(new GetEventsRequestBuilder(),cancel);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<Event> GetEventsAsync(IBuilder<GetEventsRequest> builder, CancellationToken cancel)
        {
            return GetEventsAsync(builder.Build(),cancel);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<Event> GetEventsAsync(GetEventsRequest request, [EnumeratorCancellation]CancellationToken cancel)
        {
            var query=new Dictionary<string,string>(){
                {"tzid", request.TimeZoneId},
                {"localized_times", "true"},
                {"last_modified", request.LastModified?.ToString("o")},
                {"include_deleted", request.IncludeDeleted==true?"true":"false"},
                {"include_moved", request.IncludeMoved==true?"true":"false"},
                {"include_managed", request.IncludeManaged==true?"true":"false"},
                {"only_managed", request.OnlyManaged==true?"true":"false"},
                {"include_geo", request.IncludeGeo==true?"true":"false"},
                {"google_event_ids", request.GoogleEventIds==true?"true":"false"},
            };

            if(request.From.HasValue){
                query["from"]=request.From.Value.ToString();
            }

            if(request.To.HasValue){
                query["to"]=request.To.Value.ToString();
            }

            if(request.CalendarIds!=null){
                var eIds=request.CalendarIds.ToArray();
                if(eIds.Length>0){
                    for(int i=0;i<eIds.Length;i++){
                        query["calendar_ids[]"+i]=eIds[i];
                    }
                }
            }

            var response=await SendAsync<ReadEventsResponse>(
                HttpMethod.Get,"events",query,_AccountToken,cancel);

            while(!cancel.IsCancellationRequested || response.Events!=null){

                foreach(var e in response.Events){
                    if(cancel.IsCancellationRequested){
                        break;
                    }
                    yield return e.ToEvent();
                }

                if(string.IsNullOrWhiteSpace(response.Pages?.NextPageUrl)){
                    break;
                }

                if(cancel.IsCancellationRequested){
                    break;
                }

                response=await SendAsync<ReadEventsResponse>(
                    HttpMethod.Get,response.Pages.NextPageUrl,null,_AccountToken,cancel);

            }


        }


        /// <inheritdoc/>
        public async Task UpsertEventAsync(string calendarId, UpsertEventRequestBuilder builder, CancellationToken cancel)
        {
            await UpsertEventAsync(calendarId,builder.Build(),cancel);
        }

        /// <inheritdoc/>
        public async Task UpsertEventAsync(string calendarId, UpsertEventRequest eventRequest, CancellationToken cancel)
        {
            await SendAsync<object>(
                HttpMethod.Post,"calendars/"+calendarId+"/events",eventRequest,_AccountToken,cancel);
        }

        /// <inheritdoc/>
        public async Task DeleteEventAsync(string calendarId, string eventId, CancellationToken cancel)
        {
            var requestBody = new DeleteEventRequest { EventId = eventId };
            await SendAsync<object>(
                HttpMethod.Delete,"calendars/"+calendarId+"/events",requestBody,_AccountToken,cancel);
        }



    }
}
