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

            CreateEventsRequest newMeeting = JsonConvert.DeserializeObject<CreateEventsRequest>(requestBody);
            if (newMeeting == null) return new BadRequestObjectResult("Invalid OnlineMeeting in body");
            
            // Add events
            await AddCalendarEvents(newMeeting, settings, new AppIndentityTeamsManager(settings));

            return new OkObjectResult("Sounds good");
        }


        private async static Task AddCalendarEvents(CreateEventsRequest newMeeting, SystemSettings settings, TeamsManager teamsManager)
        {

            // Build list of users to add event to
            List<User> usersToAddEventTo = await teamsManager.GetParticipants(newMeeting.Request);
            await AddEvent(usersToAddEventTo, newMeeting, teamsManager);
        }



        private static async Task AddEvent(List<User> usersToAddEventTo, CreateEventsRequest newMeeting, TeamsManager teamsManager)
        {
            Event newEvent = new Event()
            {
                Subject = newMeeting.Request.Subject,
                Body = GenerateBody(newMeeting.Meeting),
                Start = new DateTimeTimeZone() { DateTime = newMeeting.Request.Start.ToString(), TimeZone = newMeeting.Request.TimeZoneName },
                End = new DateTimeTimeZone() { DateTime = newMeeting.Request.End.ToString(), TimeZone = newMeeting.Request.TimeZoneName }
            };

            // Add calendar event for each user
            foreach (var userToAddEventTo in usersToAddEventTo)
            {
                var newEventForUser = await teamsManager.Client.Users[userToAddEventTo.Id].Events.Request().AddAsync(newEvent);
            }
        }


        private static ItemBody GenerateBody(OnlineMeeting newMeeting)
        {
            //string body = Properties.Resources;
            return new ItemBody() { ContentType = BodyType.Html };
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
