using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.VirtualMachine;

public sealed class HypervVirtualMachineVolumeInfo
{
    public Guid VMId { get; set; }

    public string VMName { get; set; }

    public string VolumeName { get; set; }

    public string VolumePath { get; set; }

    public string Host { get; set; }

    public int ControllerNumber { get; set; }

    public int ControllerLocation { get; set; }
}