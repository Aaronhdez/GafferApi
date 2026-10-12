using System.Diagnostics;
using AwesomeAssertions;
using Gaffer.Api.Infrastructure;
using Gaffer.Api.Startups;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gaffer.Api.Tests.Unit.HealthChecks;

public class DatabaseHealthCheckShould
{
    private const string SensitiveDetail = "Server=db-host;Uid=gaffer-user;Pwd=s3cr3t; MySqlException: Access denied";
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(200);

    public enum ProbeOutcome { Reachable, Unreachable, Throws, Blocked, CancelledByCaller }

    private static IEnumerable<TestCaseData> Matrix()
    {
        foreach (var outcome in Enum.GetValues<ProbeOutcome>())
        {
            // The fallback cannot recover without a restart (Q12): the probe is never consulted.
            yield return Case(DatabaseProviderReason.DummyByFallback, outcome, HealthStatus.Unhealthy);
            // Dummy on purpose (disabled or no connection string, Q13 a)).
            yield return Case(DatabaseProviderReason.DummyByConfiguration, outcome, HealthStatus.Degraded);
        }
        yield return Case(DatabaseProviderReason.Sql, ProbeOutcome.Reachable, HealthStatus.Healthy);
        yield return Case(DatabaseProviderReason.Sql, ProbeOutcome.Unreachable, HealthStatus.Unhealthy);
        yield return Case(DatabaseProviderReason.Sql, ProbeOutcome.Throws, HealthStatus.Unhealthy);
        yield return Case(DatabaseProviderReason.Sql, ProbeOutcome.Blocked, HealthStatus.Unhealthy);
    }

    private static TestCaseData Case(DatabaseProviderReason reason, ProbeOutcome outcome, HealthStatus expected)
        => new TestCaseData(reason, outcome, expected).SetName($"Report{expected}With{reason}AndA{outcome}Probe");

    [TestCaseSource(nameof(Matrix))]
    public async Task ReportTheStatusOfTheChosenProvider(DatabaseProviderReason reason, ProbeOutcome outcome, HealthStatus expected)
    {
        var probe = new FakeProbe(outcome);
        var check = new DatabaseHealthCheck(new DatabaseProviderSelection(reason), probe, ShortTimeout);
        using var caller = new CancellationTokenSource();
        if (outcome == ProbeOutcome.CancelledByCaller) caller.Cancel();

        var result = await check.CheckHealthAsync(new HealthCheckContext(), caller.Token);

        result.Status.Should().Be(expected);
        probe.Calls.Should().Be(reason == DatabaseProviderReason.Sql ? 1 : 0);
        ShouldNotExposeAnything(result);
    }

    [Test]
    public async Task ReportUnhealthyWhenTheFallbackDummyIsUsedEvenIfTheDatabaseAnswersAgain()
    {
        var probe = new FakeProbe(ProbeOutcome.Reachable);
        var check = new DatabaseHealthCheck(new DatabaseProviderSelection(DatabaseProviderReason.DummyByFallback), probe);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        probe.Calls.Should().Be(0);
    }

    [Test]
    public async Task ReportUnhealthyWhenTheSqlProviderHasNoProbe()
    {
        var check = new DatabaseHealthCheck(new DatabaseProviderSelection(DatabaseProviderReason.Sql), probe: null);

        (await check.CheckHealthAsync(new HealthCheckContext())).Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task StopWithTheRequestWhenItsTokenIsCancelledDuringTheProbe()
    {
        var probe = new FakeProbe(ProbeOutcome.CancelledByCaller);
        var check = new DatabaseHealthCheck(new DatabaseProviderSelection(DatabaseProviderReason.Sql), probe);
        using var caller = new CancellationTokenSource();
        var stopwatch = Stopwatch.StartNew();
        caller.CancelAfter(TimeSpan.FromMilliseconds(100));

        var checkHealth = async () => await check.CheckHealthAsync(new HealthCheckContext(), caller.Token);

        await checkHealth.Should().ThrowAsync<OperationCanceledException>();
        stopwatch.Stop();
        TestContext.Out.WriteLine($"Cancelled after {stopwatch.ElapsedMilliseconds} ms");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
        probe.ReceivedToken.IsCancellationRequested.Should().BeTrue();
    }

    [Test]
    public async Task GiveUpAsUnhealthyWithinFiveSecondsWhenTheProbeDoesNotAnswer()
    {
        var probe = new FakeProbe(ProbeOutcome.Blocked);
        var check = new DatabaseHealthCheck(new DatabaseProviderSelection(DatabaseProviderReason.Sql), probe);
        var stopwatch = Stopwatch.StartNew();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        stopwatch.Stop();
        TestContext.Out.WriteLine($"Gave up after {stopwatch.ElapsedMilliseconds} ms (timeout {DatabaseHealthCheck.DefaultTimeout})");
        DatabaseHealthCheck.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(5));
        result.Status.Should().Be(HealthStatus.Unhealthy);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5.5));
        probe.ReceivedToken.IsCancellationRequested.Should().BeTrue("the probe is told to stop when the time is up");
        ShouldNotExposeAnything(result);
    }

    private static void ShouldNotExposeAnything(HealthCheckResult result)
    {
        result.Exception.Should().BeNull();
        result.Data.Should().BeEmpty();
        foreach (var fragment in new[] { "db-host", "gaffer-user", "s3cr3t", "Pwd", "MySqlException", "Access denied" })
        {
            (result.Description ?? "").Should().NotContain(fragment);
        }
    }

    private sealed class FakeProbe(ProbeOutcome outcome) : IDatabaseProbe
    {
        public int Calls { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }

        public async Task<bool> IsReachableAsync(CancellationToken cancellationToken)
        {
            Calls++;
            ReceivedToken = cancellationToken;
            switch (outcome)
            {
                case ProbeOutcome.Reachable:
                    return true;
                case ProbeOutcome.Unreachable:
                    return false;
                case ProbeOutcome.Throws:
                    throw new InvalidOperationException(SensitiveDetail);
                case ProbeOutcome.Blocked:
                    // Ignores the token on purpose: the check must not wait for it.
                    return await new TaskCompletionSource<bool>().Task;
                default:
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                    return true;
            }
        }
    }
}
