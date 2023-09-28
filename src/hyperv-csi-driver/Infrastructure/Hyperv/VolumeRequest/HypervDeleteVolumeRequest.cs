using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.VolumeRequest;

public sealed class HypervDeleteVolumeRequest
{
    public Guid Id { get; set; }
    public string Path { get; set; }

    //maybe public bool/DateTimeOffset Retain { get; set; }
}