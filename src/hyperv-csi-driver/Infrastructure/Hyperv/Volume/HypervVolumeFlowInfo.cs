using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.Volume;

public sealed class HypervVolumeFlowInfo
{
    public Guid VMId { get; set; }

    public string VMName { get; set; }

    public string Host { get; set; }

    public string Path { get; set; }

    //todo Iops values 
}