using System;
using System.Linq;
using AutoFixture;

namespace Dogger.Tests.TestHelpers
{
    class RandomObjectFactory
    {
        public static T Create<T>(Func<T, object> modifications)
        {
            return Create<T>(t =>
            {
                modifications?.Invoke(t);
            });
        }

        public static T Create<T>(Action<T> modifications = null)
        {
            var fixture = new Fixture()
            {
                RepeatCount = 1
            };

            fixture
                .Behaviors
                .Remove(fixture
                    .Behaviors
                    .Single(x => 
                        x.GetType() == typeof(ThrowingRecursionBehavior)));

            fixture.Behaviors.Add(new OmitOnRecursionBehavior(2));

            var instance = fixture.Build<T>().Create();
            modifications?.Invoke(instance);

            return instance;
        }
    }
}
