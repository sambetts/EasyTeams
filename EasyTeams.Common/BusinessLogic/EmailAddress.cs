using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EasyTeams.Common.BusinessLogic
{
    public class ContactEmailAddress
    {
        /// <summary>
        /// Deserialisation constructor only
        /// </summary>
        [JsonConstructor]
        public ContactEmailAddress() { }

        /// <summary>
        /// Throws ArgumentOutOfRangeException if email address is invalid
        /// </summary>
        public ContactEmailAddress(string emailAddress)
        {
            if (IsValid(emailAddress))
            {
                this.Email = emailAddress;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(emailAddress), $"Not a valid email address: '{emailAddress}'");
            }
        }
        public string Email { get; set; }

        bool IsValid(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        internal async Task<MeetingParticipantInfo> ToMeetingParticipantInfo(TeamsObjectCache teamsCache)
        {
            var user = await teamsCache.GetUser(this.Email);
            return new MeetingParticipantInfo()
            {
                Identity = new IdentitySet() { User = new Identity() { Id = user.Id } },
                Upn = user.UserPrincipalName
            };
        }
    }
}
