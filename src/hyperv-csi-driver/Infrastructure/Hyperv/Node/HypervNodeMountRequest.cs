using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.Node;

public sealed class HypervNodeMountRequest
{
    public string Name { get; set; }

    public Guid VhdId { get; set; }

    public int ControllerNumber { get; set; }

    public int ControllerLocation { get; set; }

    public string FSType { get; set; }

    public bool Readonly { get; set; }

    public string[] Options { get; set; }

    public bool Raw { get; set; }

    public string TargetPath { get; set; }

    public bool ValidateLabel { get; set; } = true;
}