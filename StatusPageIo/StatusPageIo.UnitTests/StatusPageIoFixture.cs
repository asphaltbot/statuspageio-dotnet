using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using StatusPageIo.Api;
using StatusPageIo.Api.Models.Components;
using StatusPageIo.Api.Models.Incidents;
using System.Threading.Tasks;

namespace StatusPageIo.UnitTests
{
    [TestFixture]
    public class StatusPageIoFixture
    {
        private StatusPageIoApi _statusPageIo;
        private static string _pageId;
        private static string _authToken;
        private static string _testSubscriberEmail;
        private static string _testSubscriberPhone;
        private static string _testSubscriberCountry;

        [SetUp]
        public void SetUp()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<StatusPageIoFixture>();
            var config = builder.Build();
            _pageId = config["pageId"];
            _authToken = config["authToken"];
            _testSubscriberEmail = config["testSubscriberEmail"];
            _testSubscriberPhone = config["testSubscriberPhone"];
            _testSubscriberCountry = config["testSubscriberCountry"];

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _statusPageIo = new StatusPageIoApi(_authToken);
        }

        

        [Test]
        public async Task GetPage_ShouldReturnValidPage()
        {
            var page = await _statusPageIo.GetPageProfile(_pageId);

            Assert.That(page, Is.Not.Null);
        }

        [Test]
        public async Task UpdatePage_ShouldReturnUpdatedPage()
        {
            var page = await _statusPageIo.GetPageProfile(_pageId);

            Assert.That(page, Is.Not.Null);

            page.Name = page.Name + " - test";

            var updatedPage = await _statusPageIo.UpdatePageProfile(page);

            Assert.That(updatedPage.Name == "Test Company - test");

            updatedPage.Name = "Test Company";
            await _statusPageIo.UpdatePageProfile(updatedPage);
        }

        [Test]
        public async Task GetComponents_ShouldReturnAtLeastOneComponent()
        {
            var components = await _statusPageIo.GetComponents(_pageId);
            Assert.That(components.Any());
        }

        [Test]
        public async Task UpdateComponent_ShouldReturnUpdatedComponent()
        {
            var components = await _statusPageIo.GetComponents(_pageId);
            Assert.That(components.Any());

            var componentToUpdate = components.First();
            var originalName = componentToUpdate.Name;

            componentToUpdate.Name = componentToUpdate.Name + " - Test";
            componentToUpdate.Status = ComponentStatus.PartialOutage;

            var updatedComponent = await _statusPageIo.UpdateComponent(_pageId, componentToUpdate);

            Assert.That(updatedComponent.Name == originalName + " - Test");

            updatedComponent.Name = originalName;
            updatedComponent.Status = ComponentStatus.Operational;
            await _statusPageIo.UpdateComponent(_pageId, updatedComponent);
        }

        [Test]
        public async Task CreateComponent_ShouldReturnNewComponent()
        {
            var existingComponents = await _statusPageIo.GetComponents(_pageId);

            var randomName = $"UnitTest{Guid.NewGuid().ToString()}";
            var component = await _statusPageIo.CreateComponent(_pageId, randomName);
            Assert.That(component, Is.Not.Null);
            Assert.That(existingComponents.Count(component => component.Id.Equals(randomName)), Is.Zero);

            var updatedComponents = await _statusPageIo.GetComponents(_pageId);

            Assert.That(updatedComponents.Count(component => component.Id.Equals(randomName)), Is.EqualTo(1));

            await _statusPageIo.DeleteComponent(_pageId, component.Id);
            
            updatedComponents = await _statusPageIo.GetComponents(_pageId);

            Assert.That(updatedComponents.Count(component => component.Id.Equals(randomName)), Is.Zero);

        }

        [Test]
        public async Task GetAllIncidents_ShouldReturnAtLeastOneIncident()
        {
            var components = await _statusPageIo.GetAllIncidents(_pageId);
            Assert.That(components.Any());
        }

        [Test]
        public async Task GetUnresolvedIncidents_ShouldReturnAtLeastOneIncident()
        {
            var components = await _statusPageIo.GetUnresolvedIncidents(_pageId);
            Assert.That(components.All(n => n.Status != IncidentStatus.Resolved));
        }

        [Test]
        public async Task GetScheduledIncidents_ShouldReturnAtLeastOneIncident()
        {
            var components = await _statusPageIo.GetScheduledIncidents(_pageId);
            Assert.That(components.All(n => n.Status == IncidentStatus.Scheduled));
        }

        [Test]
        public async Task CreateRealtimeIncident_ShouldReturnNewIncident()
        {
            var incident = await _statusPageIo.CreateRealtimeIncident(_pageId, "Test Incident");
            Assert.That(incident.Name == "Test Incident");

            await _statusPageIo.DeleteIncident(_pageId, incident.Id);

            var incidentsAfterDelete = await _statusPageIo.GetAllIncidents(_pageId);

            Assert.That(incidentsAfterDelete.All(n => n.Id != incident.Id));
        }

        [Test]
        [Ignore("Trial pages cannot add subscribers who are not team members")]
        public async Task GetSubscribers_ShouldReturnAtLeastOneSubscriber()
        {
            var subscriber1 = await _statusPageIo.CreateEmailSubscriber(_pageId, _testSubscriberEmail);

            var subscribers = await _statusPageIo.GetSubscribers(_pageId);
            Assert.That(subscribers.Any());

            await _statusPageIo.DeleteSubscriber(_pageId, subscriber1.Id);
        }

        //this sends real SMS and email
        [Test]
        [Ignore("Trial pages cannot add subscribers who are not team members")]
        public async Task CreateSubscribers_ShouldCreateSubscriber()
        {
            var subscriber1 = await _statusPageIo.CreateEmailSubscriber(_pageId, _testSubscriberEmail);
            Assert.That(subscriber1, Is.Not.Null);

            var subscriber2 = await _statusPageIo.CreatePhoneSubscriber(_pageId, _testSubscriberPhone, _testSubscriberCountry);
            Assert.That(subscriber2, Is.Not.Null);

            await _statusPageIo.DeleteSubscriber(_pageId, subscriber1.Id);
            await _statusPageIo.DeleteSubscriber(_pageId, subscriber2.Id);
        }

        [Test]
        public async Task GetMetricProviders_ShouldReturnFiveMetricProviders()
        {
            var providers = await _statusPageIo.GetMetricProviders();
            Assert.That(providers.Count() == 5);
        }

        [Test]
        public async Task GetMetricProvidersForPage_ShouldReturnAMetricProviders()
        {
            var providers = await _statusPageIo.GetMetricProvidersForPage(_pageId);
            Assert.That(providers.Any());
        }

    }
}
