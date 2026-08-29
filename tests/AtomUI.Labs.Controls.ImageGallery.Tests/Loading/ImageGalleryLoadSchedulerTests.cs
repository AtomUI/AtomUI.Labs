using AtomUI.Labs.Controls.ImageGallery.Loading;
using Shouldly;
using Xunit;

namespace AtomUI.Labs.Controls.ImageGallery.Tests.Loading;

#pragma warning disable xUnit1051 // Scheduler callbacks must observe the scheduler-provided linked token.

public sealed class ImageGalleryLoadSchedulerTests
{
    [Fact]
    public async Task Regular_concurrency_is_six_and_current_image_gets_one_escape_slot()
    {
        using var scheduler = new ImageGalleryLoadScheduler();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sixStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var escapeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var maximum = 0;

        async ValueTask<int> BlockRegular(CancellationToken token)
        {
            var now = Interlocked.Increment(ref active);
            UpdateMaximum(ref maximum, now);
            if (now == ImageGalleryLoadScheduler.MaximumRegularConcurrency)
            {
                sixStarted.TrySetResult();
            }

            await release.Task.WaitAsync(token);
            Interlocked.Decrement(ref active);
            return 1;
        }

        var regular = Enumerable.Range(0, ImageGalleryLoadScheduler.MaximumRegularConcurrency)
            .Select(_ => scheduler.ScheduleAsync(
                ImageGalleryLoadPriority.VisibleThumbnail,
                BlockRegular))
            .ToArray();
        await sixStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var queued = scheduler.ScheduleAsync(
            ImageGalleryLoadPriority.VisibleThumbnail,
            BlockRegular);
        var current = scheduler.ScheduleAsync(
            ImageGalleryLoadPriority.CurrentMainImage,
            async token =>
            {
                var now = Interlocked.Increment(ref active);
                UpdateMaximum(ref maximum, now);
                escapeStarted.TrySetResult();
                await release.Task.WaitAsync(token);
                Interlocked.Decrement(ref active);
                return 2;
            });

        await escapeStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        maximum.ShouldBe(7);
        queued.IsCompleted.ShouldBeFalse();

        release.TrySetResult();
        await Task.WhenAll(regular.Append(queued).Append(current));
    }

    [Fact]
    public async Task Pending_work_starts_in_priority_then_fifo_order()
    {
        using var scheduler = new ImageGalleryLoadScheduler();
        var blockers = Enumerable.Range(0, ImageGalleryLoadScheduler.MaximumRegularConcurrency)
            .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        var started = 0;
        var allStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = blockers.Select(blocker => scheduler.ScheduleAsync(
            ImageGalleryLoadPriority.VisibleThumbnail,
            async token =>
            {
                if (Interlocked.Increment(ref started) == blockers.Length)
                {
                    allStarted.TrySetResult();
                }

                await blocker.Task.WaitAsync(token);
                return 0;
            })).ToArray();
        await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var order = new List<string>();
        var speculative = scheduler.ScheduleAsync(
            ImageGalleryLoadPriority.Speculative,
            _ =>
            {
                lock (order) order.Add("speculative");
                return ValueTask.FromResult(0);
            });
        var firstVisible = scheduler.ScheduleAsync(
            ImageGalleryLoadPriority.VisibleThumbnail,
            _ =>
            {
                lock (order) order.Add("visible-1");
                return ValueTask.FromResult(0);
            });
        var secondVisible = scheduler.ScheduleAsync(
            ImageGalleryLoadPriority.VisibleThumbnail,
            _ =>
            {
                lock (order) order.Add("visible-2");
                return ValueTask.FromResult(0);
            });

        blockers[0].TrySetResult();
        await Task.WhenAll(firstVisible, secondVisible, speculative)
            .WaitAsync(TimeSpan.FromSeconds(5));
        order.ShouldBe(["visible-1", "visible-2", "speculative"]);

        foreach (var blocker in blockers)
        {
            blocker.TrySetResult();
        }

        await Task.WhenAll(running);
    }

    private static void UpdateMaximum(ref int location, int value)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref location);
            if (observed >= value)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(ref location, value, observed) != observed);
    }
}

#pragma warning restore xUnit1051
