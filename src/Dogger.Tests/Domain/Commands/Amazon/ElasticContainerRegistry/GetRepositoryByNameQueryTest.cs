using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryByName;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Commands.Amazon.ElasticContainerRegistry
{
    [TestClass]
    public class GetRepositoryByNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_RepositoryFound_ReturnsSingleRepository()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .DescribeRepositoriesAsync(Arg.Is<DescribeRepositoriesRequest>(args => args
                    .RepositoryNames
                    .Contains("some-repository-name")))
                .Returns(new DescribeRepositoriesResponse()
                {
                    Repositories = new List<Repository>()
                    {
                        new Repository()
                    }
                });

            var handler = new GetRepositoryByNameQueryHandler(fakeAmazonEcr);

            //Act
            var repository = await handler.Handle(new GetRepositoryByNameQuery("some-repository-name"), default);

            //Assert
            Assert.IsNotNull(repository);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_RepositoryNotFound_ReturnsNull()
        {
            //Arrange
            var fakeAmazonEcr = Substitute.For<IAmazonECR>();
            fakeAmazonEcr
                .DescribeRepositoriesAsync(Arg.Is<DescribeRepositoriesRequest>(args => args
                    .RepositoryNames
                    .Contains("some-repository-name")))
                .Throws(new RepositoryNotFoundException("dummy"));

            var handler = new GetRepositoryByNameQueryHandler(fakeAmazonEcr);

            //Act
            var repository = await handler.Handle(new GetRepositoryByNameQuery("some-repository-name"), default);

            //Assert
            Assert.IsNull(repository);
        }
    }
}
