using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.Volume;

public sealed class HypervVolumeFlowFilter
{
    public Guid VMId { get; set; }

    public string VMName { get; set; }

    public string VolumePath { get; set; }
}