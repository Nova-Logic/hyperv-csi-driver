namespace HypervCsiDriver.Infrastructure.Hyperv.Node;

public sealed class HypervNodePublishRequest
{
    public string Name { get; set; }

    public string StagingTargetPath { get; set; }

    public string PublishTargetPath { get; set; }
}