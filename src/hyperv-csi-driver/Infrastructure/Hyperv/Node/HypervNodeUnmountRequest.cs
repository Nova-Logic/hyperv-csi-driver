namespace HypervCsiDriver.Infrastructure.Hyperv.Node;

public sealed class HypervNodeUnmountRequest
{
    public string Name { get; set; }

    public string TargetPath { get; set; }
}