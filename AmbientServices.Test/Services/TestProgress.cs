﻿using AmbientServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmbientServices.Test
{
    /// <summary>
    /// A class that holds tests for <see cref="IAmbientProgressService"/>.
    /// </summary>
    [TestClass]
    public class TestProgress
    {
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void Progress()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            progress?.Update(0.01f);
            Assert.AreEqual(0.01f, progress?.PortionComplete ?? float.NaN);
            Assert.AreEqual("", progress?.ItemCurrentlyBeingProcessed);
            progress?.Update(0.02f, "test");
            Assert.AreEqual(0.02f, progress?.PortionComplete ?? float.NaN);
            Assert.AreEqual("test", progress?.ItemCurrentlyBeingProcessed);
            progress?.Update(0.03f);
            Assert.AreEqual(0.03f, progress?.PortionComplete ?? float.NaN);
            Assert.AreEqual("test", progress?.ItemCurrentlyBeingProcessed);
            progress?.Update(0.04f, "");
            Assert.AreEqual(0.04f, progress?.PortionComplete ?? float.NaN);
            Assert.AreEqual("", progress?.ItemCurrentlyBeingProcessed);
            using (progress?.TrackPart(0.05f, 0.10f, null, true))
            {
                IAmbientProgress subprogress = AmbientProgressService.GlobalProgress;
                subprogress?.ResetCancellation(TimeSpan.FromMilliseconds(5));
            }
            using (progress?.TrackPart(0.10f, 0.15f, null, false))
            {
                IAmbientProgress subprogress = AmbientProgressService.GlobalProgress;
                subprogress?.ResetCancellation(TimeSpan.FromMilliseconds(5));
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void ProgressIndependence()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            OtherThreadContext c = new OtherThreadContext();
            Thread t = new Thread(c.OtherThread);
            t.Start();
            // wait until the other thread executes
            if (!c.Done.Wait(30000)) throw new TimeoutException();
            Assert.IsFalse(c.OtherThreadProgressMatches);
        }
        class OtherThreadContext
        {
            public IAmbientProgress MainProgress { get; set; }
            public bool OtherThreadProgressMatches { get; set; }
            public IAmbientProgress OtherProgress { get; set; }
            public SemaphoreSlim Done { get; } = new SemaphoreSlim(0);
            public void OtherThread()
            {
                OtherProgress = AmbientProgressService.GlobalProgress;
                OtherThreadProgressMatches = (OtherProgress == MainProgress);
                Done.Release();
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void ProgressPartStack()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.05f, 0.10f))
            {
                Assert.AreNotEqual(progress, AmbientProgressService.GlobalProgress);
            }
            Assert.AreEqual(progress, AmbientProgressService.GlobalProgress);
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void ProgressPart()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.05f, 0.05f, "prefix-"))
            {
                IAmbientProgress subprogress = AmbientProgressService.GlobalProgress;
                Assert.AreEqual(0.0f, subprogress?.PortionComplete);
                Assert.AreNotEqual(progress, AmbientProgressService.GlobalProgress);
                subprogress?.Update(.5f, "subitem");
                Assert.AreEqual(0.5f, subprogress?.PortionComplete);
                Assert.AreEqual("subitem", subprogress?.ItemCurrentlyBeingProcessed);
                Assert.AreEqual(0.075f, progress?.PortionComplete);
                Assert.AreEqual("prefix-subitem", progress?.ItemCurrentlyBeingProcessed);
            }
            Assert.AreEqual(progress, AmbientProgressService.GlobalProgress);
            Assert.AreEqual(0.10f, progress?.PortionComplete);
            Assert.AreEqual("prefix-subitem", progress?.ItemCurrentlyBeingProcessed);
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void ProgressThread()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            progress?.Update(0.25f, "main");
            Thread thread = new Thread(new ParameterizedThreadStart(o =>
                {
                    // the progress here should be a SEPARATE progress because it's a separate execution thread
                    IAmbientProgress threadProgress = AmbientProgressService.GlobalProgress;
                    threadProgress?.Update(0.75f, "thread");
                    threadProgress?.Update(0.33f, "cross-thread");
                }));
            thread.Start();
            thread.Join();
            Assert.AreEqual(0.33f, progress?.PortionComplete ?? 0);
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PortionCompleteTooLowError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            progress?.Update(-.01f);
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PortionCompleteTooHighError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            progress?.Update(1.01f);
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PartPortionCompleteTooLowError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.01f, 0.02f))
            {
                IAmbientProgress subprogress = AmbientProgressService.GlobalProgress;
                subprogress?.Update(-.01f);
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PartPortionCompleteTooHighError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.01f, 0.02f))
            {
                IAmbientProgress subprogress = AmbientProgressService.GlobalProgress;
                subprogress?.Update(1.01f);
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void StartPortionTooLowError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(-0.01f, 1.0f))
            {
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void StartPortionTooHighError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.0f, 1.01f))
            {
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PortionPartTooLowError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(1.0f, -0.01f))
            {
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PortionPartTooHighError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(1.0f, 1.01f))
            {
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PortionTooLargeError()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.5f, 0.73f))
            {
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void PartStackCorruption()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            IDisposable subProgress1 = progress?.TrackPart(0.05f, 0.13f);
            IDisposable subProgress2 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            IDisposable subProgress3 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            Assert.ThrowsException<InvalidOperationException>(() => subProgress1?.Dispose());
            Assert.ThrowsException<InvalidOperationException>(() => subProgress3?.Dispose());
            Assert.ThrowsException<InvalidOperationException>(() => subProgress2?.Dispose());
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void PartStackCorruption2()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            IDisposable subProgress1 = progress?.TrackPart(0.05f, 0.13f);
            IDisposable subProgress2 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            IDisposable subProgress3 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            Assert.ThrowsException<InvalidOperationException>(() => subProgress1?.Dispose());
            Assert.ThrowsException<InvalidOperationException>(() => subProgress2?.Dispose());
            Assert.ThrowsException<InvalidOperationException>(() => subProgress3?.Dispose());
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void PartStackCorruption3()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            IDisposable subProgress1 = progress?.TrackPart(0.05f, 0.13f);
            IDisposable subProgress2 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            IDisposable subProgress3 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            Assert.ThrowsException<InvalidOperationException>(() => subProgress2?.Dispose());
            Assert.ThrowsException<InvalidOperationException>(() => subProgress3?.Dispose());
            subProgress1?.Dispose();
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void PartStackCorruption4()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            IDisposable subProgress1 = progress?.TrackPart(0.05f, 0.13f);
            IDisposable subProgress2 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            IDisposable subProgress3 = AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.24f);
            Assert.ThrowsException<InvalidOperationException>(() => subProgress2?.Dispose());
            subProgress1?.Dispose();
            Assert.ThrowsException<InvalidOperationException>(() => subProgress3?.Dispose());
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void GetCancellationToken()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            CancellationToken token = progress?.CancellationToken ?? default(CancellationToken);
            Assert.IsNotNull(token);

            using (ScopedLocalServiceOverride<IAmbientProgressService> LocalServiceOverride = new ScopedLocalServiceOverride<IAmbientProgressService>(null))
            {
                IAmbientProgress noProgress = AmbientProgressService.Progress;
                CancellationToken cancel = noProgress?.CancellationToken ?? default(CancellationToken);
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void CancellationTokenSource()
        {
            IAmbientProgress progress = AmbientProgressService.Progress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (AmbientClock.Pause())
            {
                progress?.ResetCancellation(TimeSpan.FromMilliseconds(102));
                using (AmbientCancellationTokenSource tokenSource = progress?.CancellationTokenSource)
                {
                    Assert.IsNotNull(tokenSource);
                    Assert.IsFalse(tokenSource?.IsCancellationRequested ?? true);
                    Assert.AreEqual(tokenSource?.Token, progress?.CancellationToken);
                    AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(111));
                    Assert.IsTrue(tokenSource?.IsCancellationRequested ?? false);
                    Assert.AreEqual(tokenSource?.Token, progress?.CancellationToken);
                    Assert.IsTrue(progress?.CancellationToken.IsCancellationRequested ?? false);

                    progress?.ResetCancellation();
                    AmbientCancellationTokenSource newTokenSource = progress?.CancellationTokenSource;
                    Assert.IsNotNull(newTokenSource);
                    Assert.IsFalse(newTokenSource?.IsCancellationRequested ?? true);
                    Assert.AreEqual(newTokenSource?.Token, progress?.CancellationToken);
                    AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100));
                    Assert.IsFalse(newTokenSource?.IsCancellationRequested ?? true);
                    Assert.IsTrue(newTokenSource?.Token.CanBeCanceled ?? false);
                    Assert.AreEqual(newTokenSource?.Token, progress?.CancellationToken);
                    Assert.IsFalse(progress?.CancellationToken.IsCancellationRequested ?? true);

                    progress?.Update(0.5f);
                    progress?.ThrowIfCancelled();

                    using (CancellationTokenSource ts = new CancellationTokenSource())
                    {
                        progress?.ResetCancellation(ts);
                        newTokenSource = progress?.CancellationTokenSource;
                        Assert.IsNotNull(newTokenSource);
                        Assert.IsFalse(newTokenSource?.IsCancellationRequested ?? true);
                        Assert.AreEqual(newTokenSource?.Token, progress?.CancellationToken);
                        Assert.IsTrue(newTokenSource?.Token.CanBeCanceled ?? false);
                        ts.Cancel();
                        Assert.IsTrue(newTokenSource?.IsCancellationRequested ?? false);
                        Assert.AreEqual(newTokenSource?.Token, progress?.CancellationToken);
                        Assert.IsTrue(progress?.CancellationToken.IsCancellationRequested ?? false);
                    }
                    using (AmbientCancellationTokenSource ts = new AmbientCancellationTokenSource(null, null))
                    {
                        Assert.IsFalse(ts.IsCancellationRequested);
                        Assert.IsFalse(ts.Token.IsCancellationRequested);

                        ts.Dispose();

                        Assert.IsTrue(ts.IsCancellationRequested);
                        Assert.IsTrue(ts.Token.IsCancellationRequested);
                    }
                    using (AmbientCancellationTokenSource ts = new AmbientCancellationTokenSource(null, TimeSpan.FromMilliseconds(int.MaxValue)))
                    {
                        Assert.IsFalse(ts.IsCancellationRequested);     // theoretically, this could fail if this part of the test takes more than 30 days to execute
                        Assert.IsFalse(ts.Token.IsCancellationRequested);

                        ts.Dispose();

                        Assert.IsTrue(ts.IsCancellationRequested);
                        Assert.IsTrue(ts.Token.IsCancellationRequested);
                    }
                    AmbientCancellationTokenSource dispose;
                    using (AmbientCancellationTokenSource ts = new AmbientCancellationTokenSource())
                    {
                        dispose = ts;
                    }
                    dispose.CancelAfter(1);
                    System.Threading.Thread.Sleep(500);
                    AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(500));
                    dispose.Cancel(false);
                }
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void SubProgressCancellationTokenSource()
        {
            IAmbientProgress progress = AmbientProgressService.Progress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.0f, 1.0f))
            {
                IAmbientProgress subprogress = AmbientProgressService.Progress;
                using (AmbientClock.Pause())
                {
                    subprogress?.ResetCancellation(TimeSpan.FromMilliseconds(100));
                    AmbientCancellationTokenSource tokenSource = subprogress?.CancellationTokenSource;
                    Assert.IsNotNull(tokenSource);
                    Assert.IsFalse(tokenSource?.IsCancellationRequested ?? true);
                    Assert.AreEqual(tokenSource?.Token, subprogress?.CancellationToken);
                    AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100));
                    Assert.IsTrue(tokenSource?.IsCancellationRequested ?? false);
                    Assert.AreEqual(tokenSource?.Token, subprogress?.CancellationToken);
                    Assert.IsTrue(subprogress?.CancellationToken.IsCancellationRequested ?? false);

                    subprogress?.ResetCancellation(null);
                    AmbientCancellationTokenSource newTokenSource = subprogress?.CancellationTokenSource;
                    Assert.IsNotNull(newTokenSource);
                    Assert.IsFalse(newTokenSource?.IsCancellationRequested ?? true);
                    Assert.AreEqual(newTokenSource?.Token, subprogress?.CancellationToken);
                    AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100));
                    Assert.IsFalse(newTokenSource?.IsCancellationRequested ?? true);
                    Assert.IsTrue(newTokenSource?.Token.CanBeCanceled ?? false);
                    Assert.AreEqual(newTokenSource?.Token, subprogress?.CancellationToken);
                    Assert.IsFalse(subprogress?.CancellationToken.IsCancellationRequested ?? true);

                    subprogress?.Update(0.5f);
                    subprogress?.ThrowIfCancelled();
                }
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void Cancellation()
        {
            IAmbientProgress progress = AmbientProgressService.Progress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (AmbientClock.Pause())
            {
                progress?.ResetCancellation(TimeSpan.FromMilliseconds(100));
                AmbientCancellationTokenSource tokenSource = progress?.CancellationTokenSource;
                Assert.IsNotNull(tokenSource);
                Assert.IsFalse(tokenSource?.IsCancellationRequested ?? true);
                AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(100));
                Assert.IsTrue(tokenSource?.IsCancellationRequested ?? false);
                Assert.ThrowsException<OperationCanceledException>(() => AmbientProgressService.Progress?.ThrowIfCancelled());
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void SubProgressCancellation()
        {
            IAmbientProgress progress = AmbientProgressService.Progress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            progress?.CancellationToken.ThrowIfCancellationRequested();
            using (progress?.TrackPart(0.0f, 1.0f))
            {
                IAmbientProgress subprogress = AmbientProgressService.Progress;
                using (AmbientClock.Pause())
                {
                    subprogress?.ResetCancellation(TimeSpan.FromMilliseconds(104));
                    AmbientCancellationTokenSource tokenSource = subprogress?.CancellationTokenSource;
                    Assert.IsNotNull(tokenSource);
                    Assert.IsFalse(tokenSource?.IsCancellationRequested ?? true);
                    AmbientClock.SkipAhead(TimeSpan.FromMilliseconds(106));
                    Assert.IsTrue(tokenSource?.IsCancellationRequested ?? false);
                    Assert.ThrowsException<OperationCanceledException>(() => AmbientProgressService.Progress?.ThrowIfCancelled());
                }
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void CancellationToken()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            if (progress?.GetType().Name == "SubProgress")
            {
                Assert.Fail("Progress: " + progress.ItemCurrentlyBeingProcessed + "(" + progress.PortionComplete + ")"); 
            }
            Assert.AreEqual("Progress", progress?.GetType().Name);
            CancellationToken token = progress?.CancellationToken ?? default(CancellationToken);
            Assert.IsFalse(token.IsCancellationRequested);
            IDisposable subProgress1 = progress?.TrackPart(0.05f, 0.11f);
            using (AmbientProgressService.GlobalProgress?.TrackPart(0.05f, 0.07f))
            {
                token = AmbientProgressService.GlobalProgress?.CancellationToken ?? default(CancellationToken);
                Assert.IsFalse(token.IsCancellationRequested);
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void Dispose()
        {
            using (Progress progress = new Progress(new BasicAmbientProgress()))
            {
                progress.Dispose(); // dispose here so we can test double-dispose
                progress.ResetCancellation();
            }
            using (Progress progress = new Progress(new BasicAmbientProgress()))
            {
                progress.Dispose(); // dispose here so we can test double-dispose
                progress.ResetCancellation();
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void TopProgressInheritCancellationSource()
        {
            BasicAmbientProgress ambientProgress = new BasicAmbientProgress();
            using (Progress progress = new Progress(ambientProgress, null, 0, 0.01f))
            {
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void SubProgressDispose()
        {
            BasicAmbientProgress ambientProgress = new BasicAmbientProgress();
            using (Progress progress = new Progress(ambientProgress))
            {
                using (Progress subprogress = new Progress(ambientProgress, progress, 0.0f, 1.0f))
                {
                    subprogress.Dispose(); // dispose here so we can test double-dispose
                }
            }
        }
        /// <summary>
        /// Performs tests on <see cref="IAmbientProgressService"/>.
        /// </summary>
        [TestMethod]
        public void DisposeTopLevelProgress()
        {
            IAmbientProgress progress = AmbientProgressService.GlobalProgress;
            using (progress as IDisposable)
            {
            }
            // this should create a new progress
            progress = AmbientProgressService.GlobalProgress;
            progress?.ResetCancellation(); // make a new cancellation in case the source was canceled in this execution context during a previous test
            using (progress?.TrackPart(0.05f, 0.13f))
            {
            }
        }
    }
}
