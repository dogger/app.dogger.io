using Dogger.Infrastructure.Docker.Yml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Infrastructure.Docker
{
    [TestClass]
    public class DockerComposeParserFactoryTest
    {
        [TestMethod]
        public void Create_DockerComposeContentsGiven_CreatesDockerComposeParser()
        {
            //Arrange
            var dockerComposeContents = "some-docker-compose-contents";

            var factory = new DockerComposeParserFactory();

            //Act
            var parser = factory.Create(dockerComposeContents);

            //Assert
            Assert.IsNotNull(parser);
        }
    }
}
