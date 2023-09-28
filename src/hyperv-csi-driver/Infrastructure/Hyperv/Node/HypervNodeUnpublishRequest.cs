namespace HypervCsiDriver.Infrastructure.Hyperv.Node;

public sealed class HypervNodeUnpublishRequest
{
    public string Name { get; set; }

    //public string StagePath { get; set; }

    public string TargetPath { get; set; }
}