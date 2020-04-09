using EasyTeams.Common.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTeams.Tests
{
    public class TestObjects
    {
        public static NewConferenceCallRequest NewConferenceCallRequest 
        {
            get 
            {
                // Create test meeting
                var newConfCall = new NewConferenceCallRequest()
                {
                    Subject = "Test Meeting",
                    Start = DateTime.Now.AddHours(1),
                    OnBehalfOf = new ContactEmailAddress("admin@M365x176143.onmicrosoft.com"),
                    Recipients = new List<ContactEmailAddress>()
                {
                    new ContactEmailAddress("meganb@M365x176143.onmicrosoft.com")
                },
                    CreateCalendarEvents = true
                };

                return newConfCall;
            }
        }
    }
}
