using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.VolumeRequest;

public sealed class HypervDetachVolumeRequest
{
    public Guid VMId { get; set; }
    public string VolumePath { get; set; }
    public string Host { get; set; }
}