using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.VirtualMachine;

public sealed class HypervVirtualMachineInfo
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Host { get; set; }
}