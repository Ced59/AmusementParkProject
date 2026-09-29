using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.LiveData;

public sealed class LiveDataPollingSettings : ILiveOperationalMutationAvailability
{
    public const string SectionName = "LiveDataPolling";

    public bool Enabled { get; set; }

    public bool PublicReadEnabled { get; set; }

    public bool OperationalMutationsEnabled { get; set; } = true;

    public bool IsEnabled => this.OperationalMutationsEnabled;

    public int LoopDelaySeconds { get; set; } = 15;

    public int LeaseDurationSeconds { get; set; } = 60;

    public List<LiveDataPollingTargetSettings> Targets { get; set; } = new List<LiveDataPollingTargetSettings>();

    public TimeSpan LoopDelay => TimeSpan.FromSeconds(this.LoopDelaySeconds);

    public TimeSpan LeaseDuration => TimeSpan.FromSeconds(this.LeaseDurationSeconds);

    public static LiveDataPollingSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        LiveDataPollingSettings settings =
            configuration.GetSection(SectionName).Get<LiveDataPollingSettings>()
            ?? new LiveDataPollingSettings();
        settings.Validate();
        return settings;
    }

    internal IReadOnlyCollection<LivePollingTarget> BuildEnabledTargets()
    {
        return this.Targets
            .Where(static target => target.Enabled)
            .Select(static target => target.BuildTarget())
            .ToArray();
    }

    internal void Validate()
    {
        if (this.LoopDelaySeconds is < 5 or > 60)
        {
            throw new InvalidOperationException("LoopDelaySeconds must be between 5 and 60.");
        }

        if (this.LeaseDurationSeconds is < 15 or >= 300)
        {
            throw new InvalidOperationException("LeaseDurationSeconds must be between 15 and 299.");
        }

        if (this.Targets is null)
        {
            throw new InvalidOperationException("Targets cannot be null.");
        }

        if (this.Targets.Count > 1)
        {
            throw new InvalidOperationException("The live pilot accepts at most one polling target.");
        }

        int enabledTargetCount = 0;
        foreach (LiveDataPollingTargetSettings target in this.Targets)
        {
            ArgumentNullException.ThrowIfNull(target);
            if (!target.Enabled)
            {
                continue;
            }

            enabledTargetCount++;
            _ = target.BuildTarget();
        }

        if (this.Enabled && enabledTargetCount != 1)
        {
            throw new InvalidOperationException(
                "Exactly one enabled live polling target is required when the pilot is enabled.");
        }
    }
}
