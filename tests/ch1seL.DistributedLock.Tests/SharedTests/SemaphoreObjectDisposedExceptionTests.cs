using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ch1seL.DistributedLock.Tests.Base;
using FluentAssertions;
using Xunit;

namespace ch1seL.DistributedLock.Tests.SharedTests
{
    public class SemaphoreObjectDisposedExceptionTests : IntervalsWithLockTestsBase
    {
        [Theory]
        [MemberData(nameof(TestsData.LockServiceTypes), MemberType = typeof(TestsData))]
        public async Task Test(Type lockServiceType)
        {
            Init(TestsData.RegistrationByServiceType[lockServiceType]);
            const int repeat = 100;
            IList<Task> tasks = new List<Task>();
            var random = new Random();

            Parallel.For(0, repeat, _ =>
            {
                tasks.Add(Task.Run(async () =>
                {
                    using IDisposable @lock = await DistributedLock.CreateLockAsync(random.Next(0, 10).ToString());
                }));
            });

            Func<Task> act = () => Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }
    }
}