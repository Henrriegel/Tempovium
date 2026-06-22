using Tempovium.Services;

namespace Tempovium.Tests;

public class PlaybackTimelineServiceTests
{
    [Fact]
    public void BackendStateUpdatesDisplayPositionWhenNotUserSeeking()
    {
        var timeline = new PlaybackTimelineService();

        timeline.ApplyBackendState(12.5, 90, isPlaying: true);

        Assert.Equal(12.5, timeline.PlaybackPositionSeconds);
        Assert.Equal(12.5, timeline.DisplayPositionSeconds);
        Assert.Equal(90, timeline.DurationSeconds);
        Assert.True(timeline.IsPlaying);
        Assert.False(timeline.IsUserSeeking);
        Assert.False(timeline.IsSeekPending);
    }

    [Fact]
    public void UserSeekBlocksBackendPollingOverwrite()
    {
        var timeline = new PlaybackTimelineService();
        timeline.ApplyBackendState(10, 100, isPlaying: true);

        timeline.BeginUserSeek();
        timeline.UpdateUserSeek(40);
        timeline.ApplyBackendState(12, 100, isPlaying: true);

        Assert.Equal(12, timeline.PlaybackPositionSeconds);
        Assert.Equal(40, timeline.DisplayPositionSeconds);
        Assert.True(timeline.IsUserSeeking);
        Assert.False(timeline.IsSeekPending);
    }

    [Fact]
    public void PendingSeekHoldsRequestedDisplayTarget()
    {
        var timeline = new PlaybackTimelineService();
        timeline.ApplyBackendState(10, 100, isPlaying: true);

        timeline.BeginUserSeek();
        timeline.UpdateUserSeek(50);
        var target = timeline.CommitUserSeek();
        timeline.ApplyBackendState(20, 100, isPlaying: true);

        Assert.Equal(50, target);
        Assert.Equal(20, timeline.PlaybackPositionSeconds);
        Assert.Equal(50, timeline.DisplayPositionSeconds);
        Assert.True(timeline.IsSeekPending);
        Assert.False(timeline.IsUserSeeking);
    }

    [Fact]
    public void PendingSeekClearsWhenBackendPositionIsCloseToTarget()
    {
        var timeline = new PlaybackTimelineService();
        timeline.ApplyBackendState(10, 100, isPlaying: true);

        timeline.BeginUserSeek();
        timeline.UpdateUserSeek(50);
        timeline.CommitUserSeek();
        timeline.ApplyBackendState(49.8, 100, isPlaying: true);

        Assert.Equal(49.8, timeline.PlaybackPositionSeconds);
        Assert.Equal(49.8, timeline.DisplayPositionSeconds);
        Assert.False(timeline.IsSeekPending);
        Assert.False(timeline.IsUserSeeking);
    }

    [Fact]
    public void InvalidBackendValuesAreSanitized()
    {
        var timeline = new PlaybackTimelineService();

        timeline.ApplyBackendState(double.NaN, double.PositiveInfinity, isPlaying: true);

        Assert.Equal(0, timeline.PlaybackPositionSeconds);
        Assert.Equal(0, timeline.DisplayPositionSeconds);
        Assert.Equal(0, timeline.DurationSeconds);
        Assert.True(timeline.IsPlaying);
    }

    [Fact]
    public void BackendPositionIsClampedToDuration()
    {
        var timeline = new PlaybackTimelineService();

        timeline.ApplyBackendState(120, 90, isPlaying: false);

        Assert.Equal(90, timeline.PlaybackPositionSeconds);
        Assert.Equal(90, timeline.DisplayPositionSeconds);
        Assert.Equal(90, timeline.DurationSeconds);
    }

    [Fact]
    public void CommitUserSeekClampsTargetToDurationBounds()
    {
        var timeline = new PlaybackTimelineService();
        timeline.ApplyBackendState(10, 60, isPlaying: false);

        timeline.BeginUserSeek();
        timeline.UpdateUserSeek(90);
        var highTarget = timeline.CommitUserSeek();

        timeline.BeginUserSeek();
        timeline.UpdateUserSeek(-10);
        var lowTarget = timeline.CommitUserSeek();

        Assert.Equal(60, highTarget);
        Assert.Equal(0, lowTarget);
        Assert.Equal(0, timeline.DisplayPositionSeconds);
        Assert.True(timeline.IsSeekPending);
    }

    [Fact]
    public void ResetClearsPlaybackAndSeekState()
    {
        var timeline = new PlaybackTimelineService();
        timeline.ApplyBackendState(10, 60, isPlaying: true);
        timeline.BeginUserSeek();
        timeline.UpdateUserSeek(45);
        timeline.CommitUserSeek();

        timeline.Reset();

        Assert.Equal(0, timeline.PlaybackPositionSeconds);
        Assert.Equal(0, timeline.DisplayPositionSeconds);
        Assert.Equal(0, timeline.DurationSeconds);
        Assert.False(timeline.IsPlaying);
        Assert.False(timeline.IsUserSeeking);
        Assert.False(timeline.IsSeekPending);
    }
}
