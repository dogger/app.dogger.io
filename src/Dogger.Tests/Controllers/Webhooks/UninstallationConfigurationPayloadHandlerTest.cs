using System;
using System.Threading.Tasks;
using Dogger.Controllers.Webhooks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.Webhooks
{
    [TestClass]
    public class UninstallationConfigurationPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ToDo()
        {
            Assert.Fail();
        }
    }
}
