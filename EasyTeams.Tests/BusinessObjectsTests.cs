using EasyTeams.Common.BusinessLogic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyTeams.Tests
{
    [TestClass]
    public class BusinessObjectsTests
    {

        [TestMethod]
        public void ValidNewConferenceCallRequestTests()
        {
            NewConferenceCallRequest request = new NewConferenceCallRequest();

            Assert.IsFalse(request.IsValid());

            // Fill out the things correctly
            request.MinutesLong = 10;
            request.OnBehalfOf = new ContactEmailAddress("jimbo@contoso.com");
            request.Recipients.Add(new ContactEmailAddress("bob@contoso.com"));
            request.TimeZoneName = TimeZoneInfo.Local.DisplayName;
            request.Start = DateTime.Now;
            request.Subject = "Test";

            Assert.IsTrue(request.IsValid());
        }

        [TestMethod]
        public void ValidContactEmailAddressTests()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                ContactEmailAddress emailAddress = new ContactEmailAddress("bob");
            });

            // Should work
            ContactEmailAddress emailAddress = new ContactEmailAddress("jimbo@contoso.com");
        }
    }
}
