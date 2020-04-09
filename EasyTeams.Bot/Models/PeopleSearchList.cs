using EasyTeams.Common.BusinessLogic;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EasyTeams.Bot.Models
{
    /// <summary>
    /// To build a list of people against Graph
    /// </summary>
    public class PeopleSearchList
    {
        public PeopleSearchList()
        {
            Recipients = new List<ContactEmailAddress>();
        }

        public List<ContactEmailAddress> Recipients { get; set; }

        public TokenResponse OAuthToken { get; set; }
    }
}
