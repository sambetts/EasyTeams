﻿using EasyTeams.Common;
using EasyTeams.Common.BusinessLogic;
using EasyTeams.Common.Config;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace EasyTeamsBot.Common
{
    /// <summary>
    /// For when a token already exists
    /// </summary>
    public class PrecachedAuthTokenTeamsManager : TeamsManager
    {
        private GraphServiceClient _client;

        /// <summary>
        /// No settings required; already have an OAuth token.
        /// </summary>
        public PrecachedAuthTokenTeamsManager(string token) : base(null)
        {
            _client = new GraphServiceClient(new DelegateAuthenticationProvider(
                async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", token);
                    await Task.FromResult(0);
                })
            );
        }

        public override GraphServiceClient Client => _client;
    }
    public class UserDelegatedTeamsManager : TeamsManager
    {
        static string[] _userDelegatedScopes = new string[] { "user.read", "OnlineMeetings.ReadWrite", "User.ReadBasic.All", "Calendars.ReadWrite" };
        private GraphServiceClient _client;

        public UserDelegatedTeamsManager(SystemSettings systemSettings) : base(systemSettings)
        {

            var app = PublicClientApplicationBuilder.Create(systemSettings.AzureAdOptions.ClientId)
                .WithRedirectUri(systemSettings.AzureAdOptions.RedirectURL)
                .Build();

            var accounts = app.GetAccountsAsync().Result;
            AuthenticationResult result;
            try
            {
                result = app.AcquireTokenSilent(_userDelegatedScopes, accounts.FirstOrDefault())
                            .ExecuteAsync().Result;
            }
            catch (MsalUiRequiredException)
            {
                result = app.AcquireTokenInteractive(_userDelegatedScopes)
                         .ExecuteAsync().Result;
            }
            InteractiveAuthenticationProvider authProvider = new InteractiveAuthenticationProvider(app, _userDelegatedScopes);


            _client = new GraphServiceClient(authProvider);
        }

        public override GraphServiceClient Client => _client;
    }

    public class AppIndentityTeamsManager : TeamsManager
    {
        private GraphServiceClient _client = null;
        public AppIndentityTeamsManager(SystemSettings systemSettings) : base(systemSettings)
        {
            var app = ConfidentialClientApplicationBuilder.Create(systemSettings.AzureAdOptions.ClientId)
                .WithTenantId(systemSettings.AzureAdOptions.TenantId)
                .WithRedirectUri(systemSettings.AzureAdOptions.RedirectURL)
                .WithClientSecret(systemSettings.AzureAdOptions.ClientSecret)
                .Build();

            ClientCredentialProvider authProvider = new ClientCredentialProvider(app);

            _client = new GraphServiceClient(authProvider);

        }

        public override GraphServiceClient Client => _client;
        private const string APP_IDENTITIES_NOT_SUPPORTED = "Creating OnlineMeetings not supported with application identities - see https://docs.microsoft.com/en-us/graph/api/application-post-onlinemeetings?view=graph-rest-1.0&tabs=http#permissions";

        public override Task<OnlineMeeting> CreateNewConferenceCall(NewConferenceCallRequest newConfCall, bool throwExceptionIfFuncionAppCallFails)
        {
            throw new NotSupportedException(APP_IDENTITIES_NOT_SUPPORTED);
        }
    }

    /// <summary>
    /// Create Online Meetings & events via Graph
    /// </summary>
    public abstract class TeamsManager
    {

        #region Constructors

        public TeamsManager(SystemSettings systemSettings)
        {
            Cache = new TeamsObjectCache(this);
            Settings = systemSettings;
        }

        #endregion

        public abstract GraphServiceClient Client { get; }
        public TeamsObjectCache Cache { get; set; }
        public SystemSettings Settings { get; set; }

        /// <summary>
        /// Create a new online meeting + calendar events (if requested)
        /// </summary>
        public virtual async Task<OnlineMeeting> CreateNewConferenceCall(NewConferenceCallRequest newConfCall, bool throwExceptionIfFuncionAppCallFails)
        {
            var call = await newConfCall.ToNewConfCall(this);

            var requestingUser = await this.Cache.GetUser(newConfCall.OnBehalfOf.Email);

            var newMeeting = await Client.Users[requestingUser.Id].OnlineMeetings.Request().AddAsync(call);

            // Add events?
            if (newConfCall.CreateCalendarEvents.HasValue && newConfCall.CreateCalendarEvents.Value)
            {
                // Fire functions app
                using (var client = new HttpClient())
                {
                    CreateEventsRequest requestContent = new CreateEventsRequest() { Meeting = newMeeting, Request = newConfCall };

                    // Add functions key if defined in configuration
                    if (!string.IsNullOrEmpty(Settings.FunctionAppKey))
                    {
                        client.DefaultRequestHeaders.Add("x-functions-key", Settings.FunctionAppKey);
                    }

                    // POST request to functions app to create meetings
                    var response = await client.PostAsync(
                        Settings.NewEventCreationURL,
                         new StringContent(JsonConvert.SerializeObject(requestContent), System.Text.Encoding.UTF8, "application/json"));
                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException ex)
                    {

                        string requestBody = await response.Content.ReadAsStringAsync();
                        string msg = $"Could not submit meeting request to function app @ {Settings.NewEventCreationURL}.";
                        if (throwExceptionIfFuncionAppCallFails)
                        {
                            throw new ApplicationException(msg, ex);
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: {msg}");
                        }
                    }
                }

            }

            return newMeeting;
        }

        /// <summary>
        /// Gets a list of user that will participating in the call
        /// </summary>
        public async Task<List<User>> GetParticipants(NewConferenceCallRequest newConfCall)
        {
            List<User> users = new List<User>();
            foreach (var recipient in newConfCall.Recipients)
            {
                var user = await Cache.GetUser(recipient.Email);
                users.Add(user);
            }

            var creator = await Cache.GetUser(newConfCall.OnBehalfOf.Email);
            users.Add(creator);

            return users;
        }

    }
}