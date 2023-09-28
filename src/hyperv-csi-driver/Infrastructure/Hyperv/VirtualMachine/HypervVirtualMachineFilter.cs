using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.VirtualMachine;

public sealed class HypervVirtualMachineFilter
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    //maybe public string Volume { get; set; }
}