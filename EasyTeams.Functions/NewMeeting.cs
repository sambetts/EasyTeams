using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using EasyTeams.Common.BusinessLogic;
using Microsoft.Graph;
using Microsoft.Extensions.Configuration;
using EasyTeams.Common.Config;
using EasyTeamsBot.Common;
using System.Collections.Generic;
using System;
using System.Linq;
using EasyTeams.Common;

namespace EasyTeams.Functions
{
    public static class NewMeeting
    {
        [FunctionName("NewMeeting")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            var config = GetConfig(context);
            SystemSettings settings = new SystemSettings(config, false);


            log.LogInformation($"NewMeeting function invoked with configuration '{settings}'.");

            string requestBody = await new System.IO.StreamReader(req.Body).ReadToEndAsync();

            // Is this a ping test?
            if (requestBody == EasyTeamsConstants.FUNCTION_BODY_TEST)
            {
                return new OkObjectResult("Ping test OK");
            }

            CreateEventsRequest newMeeting = JsonConvert.DeserializeObject<CreateEventsRequest>(requestBody);
            if (newMeeting == null) return new BadRequestObjectResult("Invalid OnlineMeeting in body");

            // Add events

            // Build list of users to add event to
            var teamsManager = new AppIndentityTeamsManager(settings);
            await AddEvent(newMeeting, teamsManager);

            return new OkObjectResult("All done");
        }



        private static async Task AddEvent(CreateEventsRequest newMeeting, TeamsManager teamsManager)
        {
            List<User> graphUserCache = await teamsManager.GetInternalParticipants(newMeeting.Request);
            Event newEvent = new Event()
            {
                Subject = newMeeting.Request.Subject,
                Body = GenerateHtmlFromMeeting(newMeeting.Meeting),
                Start = new DateTimeTimeZone() { DateTime = newMeeting.Request.Start.ToGraphString(), TimeZone = newMeeting.Request.TimeZoneName },
                End = new DateTimeTimeZone() { DateTime = newMeeting.Request.End.ToGraphString(), TimeZone = newMeeting.Request.TimeZoneName },
                Attendees = GetAttendees(newMeeting.Request.Recipients, graphUserCache)
            };

            // Add calendar event for each user in the organisation (can't for external)
            var eventAuthor = graphUserCache.FindUserByEmail(newMeeting.Request.OnBehalfOf.Email);

            // Will send invites to attendees
            // https://docs.microsoft.com/en-us/graph/api/user-post-events
            var newEventForUser = await teamsManager.Client.Users[eventAuthor.Id].Calendar.Events.Request().AddAsync(newEvent);
            Console.WriteLine($"Created calendar event ID {newEventForUser.Id} for {eventAuthor.DisplayName}.");
        }

        private static IEnumerable<Attendee> GetAttendees(List<MeetingContact> usersInvited, List<User> userCache)
        {
            List<Attendee> attendees = new List<Attendee>();
            foreach (var user in usersInvited)
            {
                // See if attendee is external or internal
                var graphUser = userCache.FindUserByEmail(user.Email);
                if (graphUser != null)
                {
                    // Add internal user
                    attendees.Add(new Attendee() { EmailAddress = new EmailAddress() { Address = user.Email, Name = graphUser.DisplayName } });
                }
                else
                {
                    // Add external user with just email address as name
                    attendees.Add(new Attendee() { EmailAddress = new EmailAddress() { Address = user.Email, Name = user.Email } });
                }
            }
            return attendees;
        }

        private static ItemBody GenerateHtmlFromMeeting(OnlineMeeting newMeeting)
        {
            // Extract HTML for joining from team meeting
            ItemBody itemBody = JsonConvert.DeserializeObject<ItemBody>(newMeeting.AdditionalData["joinInformation"].ToString());

            // Clean up & convert HTML from meeting data
            string html = Uri.UnescapeDataString(itemBody.Content).Replace("+", " ").TrimStart(@"data:text/html,".ToCharArray());
            return new ItemBody() { ContentType = BodyType.Html, Content = html };
        }

        static IConfiguration GetConfig(ExecutionContext context)
        {
            return new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
        }
    }
}
