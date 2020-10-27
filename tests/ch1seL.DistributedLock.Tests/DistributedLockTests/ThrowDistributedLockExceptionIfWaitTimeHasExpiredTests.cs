﻿using System;
using System.Threading.Tasks;
using ch1seL.DistributedLock.Tests.Base;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace ch1seL.DistributedLock.Tests.DistributedLockTests
{
    public class ThrowDistributedLockExceptionIfWaitTimeHasExpiredTests : LockTestsBase
    {
        [Theory]
        [MemberData(nameof(LockServiceImplementationsTestsData.LockServiceTypes),
            MemberType = typeof(LockServiceImplementationsTestsData))]
        public async Task Test(Type lockServiceType)
        {
            Init(LockServiceImplementationsTestsData.RegistrationByServiceType[lockServiceType]);

            Func<Task> act = () => Task.WhenAll(
                AddIntervalTaskWithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1)),
                AddIntervalTaskWithLock(TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1))
            );

            var exception = await act.Should().ThrowExactlyAsync<DistributedLockException>();
            exception.Which.Status.Should().Be(DistributedLockBadStatus.Conflicted);
            Intervals.Should().HaveCount(1);
        }
    }
}