﻿using AmbientServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestAmbientServices
{
    /// <summary>
    /// A class that holds test cases for the ambient bottleneck detector.
    /// </summary>
    [TestClass]
    public class TestServiceProfiler
    {
        private static readonly ServiceReference<IAmbientServiceProfiler> _ServiceProfilerAccessor = Service.GetReference<IAmbientServiceProfiler>();

        private static AsyncLocal<string> _localContextId = new AsyncLocal<string>();
        private static AsyncLocal<string> _local = new AsyncLocal<string>(c => System.Diagnostics.Debug.WriteLine($"Thread:{System.Threading.Thread.CurrentThread.ManagedThreadId},Context:{_localContextId.Value ?? Guid.Empty.ToString("N")},Previous:{c.PreviousValue ?? "<null>"},Current:{c.CurrentValue ?? "<null>"},ThreadContextChanged:{c.ThreadContextChanged}"));
        [TestMethod]
        public async Task AsyncLocalTest1()
        {
            _localContextId.Value = Guid.NewGuid().ToString("N");
            DumpState(1, 0);
            await Task.Delay(100);
            DumpState(1, 1);
            _local.Value = "AsyncLocalTest1";
            DumpState(1, 2);
            await Task.Delay(100);
            DumpState(1, 3);
            await AsyncLocalTest2();
            DumpState(1, 4);
            await Task.Delay(100);
            DumpState(1, 5);
        }

        private async Task AsyncLocalTest2()
        {
            DumpState(2, 0);
            await Task.Delay(100);
            DumpState(2, 1);
            _local.Value = "AsyncLocalTest2";
            DumpState(2, 2);
            await Task.Delay(100);
            DumpState(2, 3);
            await AsyncLocalTest3();
            DumpState(2, 4);
            await Task.Delay(100);
            DumpState(2, 5);
        }

        private async Task AsyncLocalTest3()
        {
            DumpState(3, 0);
            await Task.Delay(100);
            DumpState(3, 1);
            _local.Value = "AsyncLocalTest3";
            DumpState(3, 2);
            await Task.Delay(100);
            DumpState(3, 3);
        }

        private void DumpState(int major, int minor)
        {
            System.Diagnostics.Debug.WriteLine($"Thread:{System.Threading.Thread.CurrentThread.ManagedThreadId},AsyncLocalTest{major}.{minor}: " + _local.Value ?? "<null>");
        }
        /*
    Thread:13,AsyncLocalTest1.0: 
    Thread:9,AsyncLocalTest1.1: 
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest1,ThreadContextChanged:False
    Thread:9,AsyncLocalTest1.2: AsyncLocalTest1
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest1,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest1,ThreadContextChanged:True
    Thread:9,AsyncLocalTest1.3: AsyncLocalTest1
    Thread:9,AsyncLocalTest2.0: AsyncLocalTest1
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest1,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest1,ThreadContextChanged:True
    Thread:9,AsyncLocalTest2.1: AsyncLocalTest1
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest1,Current:AsyncLocalTest2,ThreadContextChanged:False
    Thread:9,AsyncLocalTest2.2: AsyncLocalTest2
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest2,Current:AsyncLocalTest1,ThreadContextChanged:True
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest1,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest2,ThreadContextChanged:True
    Thread:9,AsyncLocalTest2.3: AsyncLocalTest2
    Thread:9,AsyncLocalTest3.0: AsyncLocalTest2
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest2,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest2,ThreadContextChanged:True
    Thread:9,AsyncLocalTest3.1: AsyncLocalTest2
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest2,Current:AsyncLocalTest3,ThreadContextChanged:False
    Thread:9,AsyncLocalTest3.2: AsyncLocalTest3
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest3,Current:AsyncLocalTest2,ThreadContextChanged:True
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest2,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest3,ThreadContextChanged:True
    Thread:9,AsyncLocalTest3.3: AsyncLocalTest3
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest3,Current:AsyncLocalTest2,ThreadContextChanged:True
    Thread:9,AsyncLocalTest2.4: AsyncLocalTest2
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest2,Current:AsyncLocalTest3,ThreadContextChanged:True
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest3,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest2,ThreadContextChanged:True
    Thread:9,AsyncLocalTest2.5: AsyncLocalTest2
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest2,Current:AsyncLocalTest1,ThreadContextChanged:True
    Thread:9,AsyncLocalTest1.4: AsyncLocalTest1
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:AsyncLocalTest1,Current:AsyncLocalTest2,ThreadContextChanged:True
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest2,Current:<null>,ThreadContextChanged:True
    Thread:9,Context:002d421118804fb3b6f40941df50e056,Previous:<null>,Current:AsyncLocalTest1,ThreadContextChanged:True
    Thread:9,AsyncLocalTest1.5: AsyncLocalTest1
    Thread:9,Context:00000000000000000000000000000000,Previous:AsyncLocalTest1,Current:<null>,ThreadContextChanged:True

         */
        [TestMethod]
        public void ServiceProfilerBasic()
        {
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> o = new LocalProviderScopedOverride<IAmbientServiceProfiler>(new BasicAmbientServiceProfiler()))
            using (AmbientClock.Pause())
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            using (IAmbientServiceProfile processProfile = factory.CreateProcessProfiler(nameof(ServiceProfilerBasic)))
            using (IDisposable timeWindowProfile = factory.CreateTimeWindowProfiler(nameof(ServiceProfilerBasic), TimeSpan.FromMilliseconds(100), p => Task.CompletedTask))
            using (IAmbientServiceProfile scopeProfile = factory.CreateCallContextProfiler(nameof(ServiceProfilerBasic)))
            {
                _ServiceProfilerAccessor.Provider?.SwitchSystem("ServiceProfilerBasic1");
                Assert.AreEqual(nameof(ServiceProfilerBasic), processProfile.ScopeName);
                foreach (AmbientServiceProfilerAccumulator stats in processProfile.ProfilerStatistics)
                {
                    if (string.IsNullOrEmpty(stats.Group))
                    {
                        Assert.AreEqual("", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(0, stats.TotalStopwatchTicksUsed);
                    }
                    else
                    {
                        Assert.AreEqual("ServiceProfilerBasic1", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(0, stats.TotalStopwatchTicksUsed);
                    }
                }

                Assert.AreEqual(nameof(ServiceProfilerBasic), scopeProfile.ScopeName);
                foreach (AmbientServiceProfilerAccumulator stats in scopeProfile.ProfilerStatistics)
                {
                    if (string.IsNullOrEmpty(stats.Group))
                    {
                        Assert.AreEqual("", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(0, stats.TotalStopwatchTicksUsed);
                    }
                    else
                    {
                        Assert.AreEqual("ServiceProfilerBasic1", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(0, stats.TotalStopwatchTicksUsed);
                    }
                }

                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(null);

                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(113));

                _ServiceProfilerAccessor.Provider?.SwitchSystem("ServiceProfilerBasic2");

                foreach (AmbientServiceProfilerAccumulator stats in scopeProfile.ProfilerStatistics)
                {
                    if (string.IsNullOrEmpty(stats.Group))
                    {
                        Assert.AreEqual("", stats.Group);
                        Assert.AreEqual(2, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(113), stats.TimeUsed);
                    }
                    else if (stats.Group.EndsWith("1"))
                    {
                        Assert.AreEqual("ServiceProfilerBasic1", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(100), stats.TimeUsed);
                    }
                    else if (stats.Group.EndsWith("2"))
                    {
                        Assert.AreEqual("ServiceProfilerBasic2", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(0), stats.TimeUsed);
                    }
                }

                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(null);

                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(113));

                _ServiceProfilerAccessor.Provider?.SwitchSystem("ServiceProfilerBasic3");

                foreach (AmbientServiceProfilerAccumulator stats in scopeProfile.ProfilerStatistics)
                {
                    if (string.IsNullOrEmpty(stats.Group))
                    {
                        Assert.AreEqual("", stats.Group);
                        Assert.AreEqual(3, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(226), stats.TimeUsed);
                    }
                    else if (stats.Group.EndsWith("1"))
                    {
                        Assert.AreEqual("ServiceProfilerBasic1", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(100), stats.TimeUsed);
                    }
                    else if (stats.Group.EndsWith("2"))
                    {
                        Assert.AreEqual("ServiceProfilerBasic2", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(100), stats.TimeUsed);
                    }
                    else if (stats.Group.EndsWith("3"))
                    {
                        Assert.AreEqual("ServiceProfilerBasic3", stats.Group);
                        Assert.AreEqual(1, stats.ExecutionCount);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(0), stats.TimeUsed);
                    }
                }

                _ServiceProfilerAccessor.Provider?.SwitchSystem(null);
            }
        }
        [TestMethod]
        public void ServiceProfilerCloseSampleWithRepeat()
        {
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> o = new LocalProviderScopedOverride<IAmbientServiceProfiler>(new BasicAmbientServiceProfiler()))
            using (AmbientClock.Pause())
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            using (IDisposable timeWindowProfile = factory.CreateTimeWindowProfiler(nameof(ServiceProfilerCloseSampleWithRepeat), TimeSpan.FromMilliseconds(100), p => Task.CompletedTask))
            {
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100)); // this should trigger the first window and it should have only the default "" entry

                _ServiceProfilerAccessor.Provider?.SwitchSystem(nameof(ServiceProfilerCloseSampleWithRepeat) + "1");
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(10));
                _ServiceProfilerAccessor.Provider?.SwitchSystem(null);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(10));
                _ServiceProfilerAccessor.Provider?.SwitchSystem("ServiceProfilerCloseSampleWithRepeat1");
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(80)); // this should trigger the second window, which should close with an augmentation of the first ServiceProfilerCloseSampleWithRepeat1 entry
                _ServiceProfilerAccessor.Provider?.SwitchSystem(null);
            }
        }
        [TestMethod]
        public void ServiceProfilerNull()
        {
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> o = new LocalProviderScopedOverride<IAmbientServiceProfiler>(null))
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            using (IAmbientServiceProfile processProfile = factory.CreateProcessProfiler(nameof(ServiceProfilerNull)))
            using (IDisposable timeWindowProfile = factory.CreateTimeWindowProfiler(nameof(ServiceProfilerNull), TimeSpan.FromMilliseconds(100), p => Task.CompletedTask))
            using (IAmbientServiceProfile scopeProfile = factory.CreateCallContextProfiler(nameof(ServiceProfilerNull)))
            using (AmbientClock.Pause())
            {
                _ServiceProfilerAccessor.Provider?.SwitchSystem(nameof(ServiceProfilerNull));
            }
        }
        [TestMethod]
        public void ServiceProfilerNullOnWindowComplete()
        {
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> o = new LocalProviderScopedOverride<IAmbientServiceProfiler>(new BasicAmbientServiceProfiler()))
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            {
                Assert.ThrowsException<ArgumentNullException>(
                    () =>
                    {
                        using (IDisposable timeWindowProfile = factory.CreateTimeWindowProfiler(nameof(ServiceProfilerNull), TimeSpan.FromMilliseconds(100), null))
                        {
                        }
                    });
            }
        }
        [TestMethod]
        public void ServiceProfilerNoListener()
        {
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> o = new LocalProviderScopedOverride<IAmbientServiceProfiler>(new BasicAmbientServiceProfiler()))
            using (AmbientClock.Pause())
            {
                _ServiceProfilerAccessor.Provider?.SwitchSystem(nameof(ServiceProfilerNull));
            }
        }
        [TestMethod]
        public void AmbientServiceProfilerSystemChangedEvent()
        {
            AmbientServiceProfilerSystemSwitchedEvent a1 = new AmbientServiceProfilerSystemSwitchedEvent(nameof(AmbientServiceProfilerSystemChangedEvent) + "New", 0, 1, nameof(AmbientServiceProfilerSystemChangedEvent) + "Old");
            AmbientServiceProfilerSystemSwitchedEvent a2 = new AmbientServiceProfilerSystemSwitchedEvent(nameof(AmbientServiceProfilerSystemChangedEvent) + "New", 0, 2, nameof(AmbientServiceProfilerSystemChangedEvent) + "Old");
            AmbientServiceProfilerSystemSwitchedEvent a3 = new AmbientServiceProfilerSystemSwitchedEvent(nameof(AmbientServiceProfilerSystemChangedEvent) + "New", 1, 1, nameof(AmbientServiceProfilerSystemChangedEvent) + "Old");
            AmbientServiceProfilerSystemSwitchedEvent b = new AmbientServiceProfilerSystemSwitchedEvent(nameof(AmbientServiceProfilerSystemChangedEvent) + "New-B", 0, 1, nameof(AmbientServiceProfilerSystemChangedEvent) + "Old-B");
            AmbientServiceProfilerSystemSwitchedEvent c = new AmbientServiceProfilerSystemSwitchedEvent(nameof(AmbientServiceProfilerSystemChangedEvent) + "New-C", 0, 1, nameof(AmbientServiceProfilerSystemChangedEvent) + "Old-C");

            Assert.AreNotEqual(a1.GetHashCode(), a2.GetHashCode());
            Assert.AreNotEqual(a1, a2);
            Assert.AreNotEqual(a1, a3);
            Assert.AreNotEqual(a1, b);
            Assert.AreNotEqual(a1, c);

            Assert.IsFalse(a1.Equals(new DateTime()));
            Assert.IsFalse(a1 == a2);
            Assert.IsFalse(a1 == a3);
            Assert.IsFalse(a1 == b);
            Assert.IsTrue(a1 != a2);
            Assert.IsTrue(a1 != a3);
            Assert.IsTrue(a1 != b);
        }

        [TestMethod]
        public void AmbientServiceProfilerFactorySettings()
        {
            string system1 = "DynamoDB/Table:My-table/Partition:342644/Result:Success";
            string system2 = "S3/Bucket:My-bucket/Prefix:abcdefg/Result:Retry";
            string system3 = "SQL/Database:My-database/Table:User/Result:Failed";
            BasicAmbientSettingsProvider settingsProvider = new BasicAmbientSettingsProvider(nameof(AmbientServiceProfilerFactorySettings));
            settingsProvider.ChangeSetting(nameof(AmbientServiceProfilerFactory) + "-DefaultSystemGroupTransform", "(?:([^:/]+)(?:(/Database:[^:/]*)|(/Bucket:[^:/]*)|(/Result:[^:/]*)|(?:/[^/]*))*)");
            using (LocalProviderScopedOverride<IAmbientSettingsProvider> o = new LocalProviderScopedOverride<IAmbientSettingsProvider>(settingsProvider))
            using (AmbientClock.Pause())
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            using (IAmbientServiceProfile scopeProfile = factory.CreateCallContextProfiler(nameof(ServiceProfilerBasic)))
            {
                _ServiceProfilerAccessor.Provider?.SwitchSystem(system1);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(5));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system2);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(200));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system3);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(3000));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system1);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(5));

                foreach (AmbientServiceProfilerAccumulator stats in scopeProfile.ProfilerStatistics)
                {
                    if (stats.Group.StartsWith("DynamoDB"))
                    {
                        Assert.AreEqual("DynamoDB/Result:Success", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(10), stats.TimeUsed);
                        Assert.AreEqual(2, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("S3"))
                    {
                        Assert.AreEqual("S3/Bucket:My-bucket/Result:Retry", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(200), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("SQL"))
                    {
                        Assert.AreEqual("SQL/Database:My-database/Result:Failed", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(3000), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                }
            }
        }
        [TestMethod]
        public void AmbientServiceProfilerFactorySettingsNonCurrent()
        {
            string system1 = "DynamoDB/Table:My-table/Partition:342644/Result:Success";
            string system2 = "S3/Bucket:My-bucket/Prefix:abcdefg/Result:Retry";
            string system3 = "SQL/Database:My-database/Table:User/Result:Failed";
            BasicAmbientSettingsProvider settingsProvider = new BasicAmbientSettingsProvider(nameof(AmbientServiceProfilerFactorySettingsNonCurrent));
            settingsProvider.ChangeSetting(nameof(AmbientServiceProfilerFactory) + "-DefaultSystemGroupTransform", "(?:([^:/]+)(?:(/Database:[^:/]*)|(/Bucket:[^:/]*)|(/Result:[^:/]*)|(?:/[^/]*))*)");
            using (LocalProviderScopedOverride<IAmbientSettingsProvider> o = new LocalProviderScopedOverride<IAmbientSettingsProvider>(settingsProvider))
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> p = new LocalProviderScopedOverride<IAmbientServiceProfiler>(new BasicAmbientServiceProfiler()))
            using (AmbientClock.Pause())
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            using (IAmbientServiceProfile scopeProfile = factory.CreateCallContextProfiler(nameof(ServiceProfilerBasic)))
            {
                _ServiceProfilerAccessor.Provider?.SwitchSystem(system1);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(5));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system2);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(200));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system3);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(3000));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system1);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(5));

                _ServiceProfilerAccessor.Provider?.SwitchSystem("noreport");

                foreach (AmbientServiceProfilerAccumulator stats in scopeProfile.ProfilerStatistics)
                {
                    if (stats.Group.StartsWith("DynamoDB"))
                    {
                        Assert.AreEqual("DynamoDB/Result:Success", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(10), stats.TimeUsed);
                        Assert.AreEqual(2, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("S3"))
                    {
                        Assert.AreEqual("S3/Bucket:My-bucket/Result:Retry", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(200), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("SQL"))
                    {
                        Assert.AreEqual("SQL/Database:My-database/Result:Failed", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(3000), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                }
            }
        }
        [TestMethod]
        public void AmbientServiceProfilerFactoryOverrideGroupTransform()
        {
            string system1 = "DynamoDB/Table:My-table/Partition:342644/Result:Success";
            string system2 = "S3/Bucket:My-bucket/Prefix:abcdefg/Result:Retry";
            string system3 = "SQL/Database:My-database/Table:User/Result:Failed";
            string groupTransform = "(?:([^:/]+)(?:(/Database:[^:/]*)|(/Bucket:[^:/]*)|(/Result:[^:/]*)|(?:/[^/]*))*)";
            IAmbientServiceProfile timeWindowProfile = null;
            using (AmbientClock.Pause())
            using (LocalProviderScopedOverride<IAmbientServiceProfiler> o = new LocalProviderScopedOverride<IAmbientServiceProfiler>(new BasicAmbientServiceProfiler()))
            using (AmbientServiceProfilerFactory factory = new AmbientServiceProfilerFactory())
            using (IAmbientServiceProfile processProfile = factory.CreateProcessProfiler(nameof(AmbientServiceProfilerFactoryOverrideGroupTransform), groupTransform))
            using (IDisposable timeWindowProfiler = factory.CreateTimeWindowProfiler(nameof(AmbientServiceProfilerFactoryOverrideGroupTransform), TimeSpan.FromMilliseconds(10000), p => { timeWindowProfile = p; return Task.CompletedTask; }, groupTransform))
            using (IAmbientServiceProfile scopeProfile = factory.CreateCallContextProfiler(nameof(AmbientServiceProfilerFactoryOverrideGroupTransform), groupTransform))
            {
                _ServiceProfilerAccessor.Provider?.SwitchSystem(system1);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(5));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system2);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(200));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system3);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(3000));

                _ServiceProfilerAccessor.Provider?.SwitchSystem(system1);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(5));

                _ServiceProfilerAccessor.Provider?.SwitchSystem("noreport");

                foreach (AmbientServiceProfilerAccumulator stats in scopeProfile.ProfilerStatistics)
                {
                    if (stats.Group.StartsWith("DynamoDB"))
                    {
                        Assert.AreEqual("DynamoDB/Result:Success", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(10), stats.TimeUsed);
                        Assert.AreEqual(2, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("S3"))
                    {
                        Assert.AreEqual("S3/Bucket:My-bucket/Result:Retry", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(200), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("SQL"))
                    {
                        Assert.AreEqual("SQL/Database:My-database/Result:Failed", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(3000), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                }
                foreach (AmbientServiceProfilerAccumulator stats in processProfile.ProfilerStatistics)
                {
                    if (stats.Group.StartsWith("DynamoDB"))
                    {
                        Assert.AreEqual("DynamoDB/Result:Success", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(10), stats.TimeUsed);
                        Assert.AreEqual(2, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("S3"))
                    {
                        Assert.AreEqual("S3/Bucket:My-bucket/Result:Retry", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(200), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("SQL"))
                    {
                        Assert.AreEqual("SQL/Database:My-database/Result:Failed", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(3000), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                }
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(10000));
                foreach (AmbientServiceProfilerAccumulator stats in timeWindowProfile.ProfilerStatistics)
                {
                    if (stats.Group.StartsWith("DynamoDB"))
                    {
                        Assert.AreEqual("DynamoDB/Result:Success", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(10), stats.TimeUsed);
                        Assert.AreEqual(2, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("S3"))
                    {
                        Assert.AreEqual("S3/Bucket:My-bucket/Result:Retry", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(200), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                    else if (stats.Group.StartsWith("SQL"))
                    {
                        Assert.AreEqual("SQL/Database:My-database/Result:Failed", stats.Group);
                        Assert.AreEqual(TimeSpan.FromMilliseconds(3000), stats.TimeUsed);
                        Assert.AreEqual(1, stats.ExecutionCount);
                    }
                }
            }
        }
    }
}
