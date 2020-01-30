using System.Linq;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Infrastructure.Docker
{
    [TestClass]
    public class DockerComposeParserTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetDockerfilePaths_DockerfilePathGiven_ReturnsDockerfilePath()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.6'

services:
  elasticsearch_searchguard_1:
    build:
      context: .
      dockerfile: foo
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var filePaths = parser.GetDockerfilePaths().ToArray();

            //Assert
            Assert.AreEqual(1, filePaths.Length);
            Assert.AreEqual("foo", filePaths[0]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetDockerfilePaths_OnlyBuildContextGiven_ReturnsDefaultDockerfile()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.6'

services:
  elasticsearch_searchguard_1:
    build:
      context: .
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var filePaths = parser.GetDockerfilePaths().ToArray();

            //Assert
            Assert.AreEqual(1, filePaths.Length);
            Assert.AreEqual("Dockerfile", filePaths[0]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetEnvironmentFilePaths_SingleStringEnvironmentFile_ReturnsEnvironmentFile()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.3'

services:
   test-service-1:
     env_file: ""some-file-path-1""
   test-service-2:
     env_file: ""some-file-path-2""
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var filePaths = parser.GetEnvironmentFilePaths().ToArray();

            //Assert
            Assert.AreEqual(2, filePaths.Length);
            Assert.AreEqual("some-file-path-1", filePaths[0]);
            Assert.AreEqual("some-file-path-2", filePaths[1]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetEnvironmentFilePaths_MultipleListEnvironmentFiles_ReturnsEnvironmentFiles()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.3'

services:
   test-service-1:
     env_file:
     - ""some-file-path-1""
     - ""some-file-path-2""
   test-service-2:
     env_file:
     - ""some-file-path-3""
     - ""some-file-path-4""
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var filePaths = parser.GetEnvironmentFilePaths().ToArray();

            //Assert
            Assert.AreEqual(4, filePaths.Length);
            Assert.AreEqual("some-file-path-1", filePaths[0]);
            Assert.AreEqual("some-file-path-2", filePaths[1]);
            Assert.AreEqual("some-file-path-3", filePaths[2]);
            Assert.AreEqual("some-file-path-4", filePaths[3]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetVolumePaths_MultipleListVolumeFilesShortHand_ReturnsEnvironmentFile()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.3'

services:
  db:
    image: postgres:latest
    volumes:
      - ""/var/run/postgres/postgres.sock:/var/run/postgres/postgres.sock""
      - ""dbdata:/var/lib/postgresql/data""
volumes:
  dbdata:
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var filePaths = parser.GetVolumePaths().ToArray();

            //Assert
            Assert.AreEqual(1, filePaths.Length);
            Assert.AreEqual("/var/run/postgres/postgres.sock", filePaths[0]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public void GetVolumePaths_MultipleListVolumeFilesLongHand_ReturnsEnvironmentFiles()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.3'

services:
  web:
    image: nginx:alpine
    volumes:
      - type: volume
        source: mydata
        target: /data
        volume:
          nocopy: true
      - type: bind
        source: ./static
        target: /opt/app/static
volumes:
  mydata:
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var filePaths = parser.GetVolumePaths().ToArray();

            //Assert
            Assert.AreEqual(1, filePaths.Length);
            Assert.AreEqual("./static", filePaths[0]);
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_EmptyYmlContents_ReturnsNoPorts()
        {
            //Arrange
            var parser = new DockerComposeParser(string.Empty);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.AreEqual(0, ports.Count);
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_WordpressSample_ReturnsOnePort()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3.3'

services:
   db:
     image: mysql:5.7
     volumes:
       - db_data:/var/lib/mysql
     restart: always
     environment:
       MYSQL_ROOT_PASSWORD: somewordpress
       MYSQL_DATABASE: wordpress
       MYSQL_USER: wordpress
       MYSQL_PASSWORD: wordpress

   wordpress:
     depends_on:
       - db
     image: wordpress:latest
     ports:
       - ""80:80""
     restart: always
     environment:
         WORDPRESS_DB_HOST: db:3306
         WORDPRESS_DB_USER: wordpress
         WORDPRESS_DB_PASSWORD: wordpress
         WORDPRESS_DB_NAME: wordpress
     volumes:
         db_data: {{}}
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.AreEqual(1, ports.Count);
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_ServiceWithoutPortsSection_ReturnsNoPorts()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3'

services:
  test-service:
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.AreEqual(0, ports.Count);
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_ServiceWithEmptyPortsSection_ReturnsNoPorts()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3'

services:
  test-service:
    ports:
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.AreEqual(0, ports.Count);
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_ShortPortNumberSyntax_ReturnsProperPorts()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3'

services:
  test-service:
    ports:
     - 3000
     - 3000-3005
     - 8000:8001
     - 9090-9091:8080-8081
     - 49100:22
     - 127.0.0.1:8002:8001
     - 127.0.0.1:5000-5010:6000-6010
     - 6060:6061/udp
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.IsTrue(ports.Any(x => x.Port == 3000 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3001 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3002 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3003 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3004 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3005 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 8000 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 9090 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 9091 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 49100 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 8002 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 5000 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5001 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5002 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5003 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5004 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5005 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5006 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5007 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5008 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5009 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5010 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 6060 && x.Protocol == SocketProtocol.Udp));
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_ShortPortStringSyntax_ReturnsProperPorts()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3'

services:
  test-service:
    ports:
     - ""3000""
     - ""3000-3005""
     - ""8000:8001""
     - ""9090-9091:8080-8081""
     - ""49100:22""
     - ""127.0.0.1:8002:8001""
     - ""127.0.0.1:5000-5010:6000-6010""
     - ""6060:6061/udp""
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.IsTrue(ports.Any(x => x.Port == 3000 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3001 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3002 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3003 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3004 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 3005 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 8000 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 9090 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 9091 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 49100 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 8002 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 5000 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5001 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5002 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5003 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5004 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5005 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5006 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5007 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5008 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5009 && x.Protocol == SocketProtocol.Tcp));
            Assert.IsTrue(ports.Any(x => x.Port == 5010 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 6060 && x.Protocol == SocketProtocol.Udp));
        }

        [TestCategory(TestCategories.UnitCategory)]
        [TestMethod]
        public void GetExposedHostPorts_LongPortSyntax_ReturnsProperPorts()
        {
            //Arrange
            var dockerComposeYmlContents = @$"
version: '3'

services:
  test-service:
    ports:
     - target: 3000
       published: 3000
     - target: 8001
       published: 8000
     - target: 22
       published: 49100
     - target: 6061
       published: 6060
       protocol: udp
";

            var parser = new DockerComposeParser(dockerComposeYmlContents);

            //Act
            var ports = parser.GetExposedHostPorts();

            //Assert
            Assert.IsTrue(ports.Any(x => x.Port == 3000 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 8000 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 49100 && x.Protocol == SocketProtocol.Tcp));
            
            Assert.IsTrue(ports.Any(x => x.Port == 6060 && x.Protocol == SocketProtocol.Udp));
        }
    }
}
