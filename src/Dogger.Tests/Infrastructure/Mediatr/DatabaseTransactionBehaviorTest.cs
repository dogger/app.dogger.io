using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Infrastructure.Mediatr
{
    [TestClass]
    public class DatabaseTransactionBehaviorTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public async Task Handle_TwoDifferentEnvironments_CantAccessUncommittedRows()
        {
            //Arrange
            var options = new EnvironmentSetupOptions()
            {
                SkipWebServer = true
            };

            await using var environment1 = await IntegrationTestEnvironment.CreateAsync(options);
            await environment1.DataContext.Database.MigrateAsync();

            await using var environment2 = await IntegrationTestEnvironment.CreateAsync(options);

            //Act & Assert
            await environment1.Mediator.Send(new TestCommand(async () =>
            {
                await environment1.DataContext.Clusters.AddAsync(new Cluster()
                {
                    Name = "outer"
                });
                await environment1.DataContext.SaveChangesAsync();

                var clusterCount1 = await environment1.DataContext.Clusters.CountAsync();

                var clusterCount2 = await environment2.WithFreshDataContext(async dataContext => 
                    await dataContext.Clusters.CountAsync());

                Assert.AreEqual(1, clusterCount1);
                Assert.AreEqual(2, clusterCount2);
            }));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public async Task Handle_NestedTransactionsOuterExceptionThrown_InnerAndOuterTransactionContentsReverted()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
            {
                await environment.Mediator.Send(new TestCommand(async () =>
                {
                    await environment.DataContext.Clusters.AddAsync(new Cluster()
                    {
                        Name = "outer"
                    });
                    await environment.DataContext.SaveChangesAsync();

                    await environment.Mediator.Send(new TestCommand(async () =>
                    {
                        await environment.DataContext.Clusters.AddAsync(new Cluster()
                        {
                            Name = "inner"
                        });
                        await environment.DataContext.SaveChangesAsync();
                    }));

                    throw new TestException();
                }));
            });

            //Assert
            Assert.IsNotNull(exception);

            await environment.WithFreshDataContext(async (dataContext) =>
            {
                var clusterCount = await dataContext.Clusters.CountAsync();
                Assert.AreEqual(0, clusterCount);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public async Task Handle_NestedTransactionsWithNoException_InnerAndOuterTransactionContentsSaved()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            //Act
            await environment.Mediator.Send(new TestCommand(async () =>
            {
                await environment.DataContext.Clusters.AddAsync(new Cluster()
                {
                    Name = "outer"
                });
                await environment.DataContext.SaveChangesAsync();

                await environment.Mediator.Send(new TestCommand(async () =>
                {
                    await environment.DataContext.Clusters.AddAsync(new Cluster()
                    {
                        Name = "inner"
                    });
                    await environment.DataContext.SaveChangesAsync();
                }));
            }));

            //Assert
            await environment.WithFreshDataContext(async (dataContext) =>
            {
                var clusterCount = await dataContext.Clusters.CountAsync();
                Assert.AreEqual(2, clusterCount);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        public async Task Handle_NestedTransactionsInnerExceptionThrown_InnerAndOuterTransactionContentsReverted()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
            {
                await environment.Mediator.Send(new TestCommand(async () =>
                {
                    await environment.DataContext.Clusters.AddAsync(new Cluster()
                    {
                        Name = "outer"
                    });
                    await environment.DataContext.SaveChangesAsync();

                    await environment.Mediator.Send(new TestCommand(async () =>
                    {
                        await environment.DataContext.Clusters.AddAsync(new Cluster()
                        {
                            Name = "inner"
                        });
                        await environment.DataContext.SaveChangesAsync();

                        throw new TestException();
                    }));
                }));
            });

            //Assert
            Assert.IsNotNull(exception);

            await environment.WithFreshDataContext(async (dataContext) =>
            {
                var clusterCount = await dataContext.Clusters.CountAsync();
                Assert.AreEqual(0, clusterCount);
            });
        }

        public class TestCommand : IRequest, IDatabaseTransactionRequest
        {
            public Func<Task> Action { get; }

            public TestCommand(
                Func<Task> action)
            {
                this.Action = action;
            }

            public IsolationLevel? TransactionIsolationLevel => default;
        }

        public class TestCommandHandler : IRequestHandler<TestCommand>
        {
            public async Task<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
            {
                await request.Action();
                return Unit.Value;
            }
        }
    }
}
