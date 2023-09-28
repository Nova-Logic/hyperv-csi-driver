using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.Volume;

public sealed class HypervVolumeDetailResult
{
    public HypervVolumeInfo Info { get; init; }

    public HypervVolumeDetail? Detail { get; init; }

    public Exception? Error { get; init; }

    public string[] Nodes { get; init; } = Array.Empty<string>();
}