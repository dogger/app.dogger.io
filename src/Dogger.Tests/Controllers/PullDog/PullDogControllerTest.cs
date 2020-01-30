using System.Threading.Tasks;
using Dogger.Controllers.PullDog;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromGitHub;
using Dogger.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.PullDog
{
    [TestClass]
    public class PullDogControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task InstallGitHub_SetupActionNotInstall_RedirectsToDashboard()
        {
            //Arrange
            var mediator = Substitute.For<MediatR.IMediator>();

            var controller = new PullDogController(mediator);

            //Act
            var result = await controller.InstallGitHub(
                "dummy",
                1337,
                "some-non-install-action");

            //Assert
            Assert.IsInstanceOfType(result, typeof(RedirectResult));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task InstallGitHub_SetupActionInstall_InstallsPullDog()
        {
            //Arrange
            var mediator = Substitute.For<MediatR.IMediator>();

            var controller = new PullDogController(mediator);

            //Act
            await controller.InstallGitHub(
                "some-code",
                1337,
                "install");

            //Assert
            await mediator
                .Received(1)
                .Send(Arg.Is<InstallPullDogFromGitHubCommand>(args => 
                    args.Code == "some-code" &&
                    args.InstallationId == 1337));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void InstallGitHubInstructions_ValidConditions_RendersView()
        {
            //Arrange
            var mediator = Substitute.For<MediatR.IMediator>();

            var controller = new PullDogController(mediator);

            //Act
            var result = controller.InstallGitHubInstructions();

            //Assert
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task InstallGitHub_SetupActionInstall_RedirectsToInstallPage()
        {
            //Arrange
            var mediator = Substitute.For<MediatR.IMediator>();

            var controller = new PullDogController(mediator);

            //Act
            var result = await controller.InstallGitHub(
                "some-code",
                1337,
                "install");

            //Assert
            Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        }
    }
}
