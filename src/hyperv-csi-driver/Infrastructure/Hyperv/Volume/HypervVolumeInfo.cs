namespace HypervCsiDriver.Infrastructure.Hyperv.Volume;

public sealed class HypervVolumeInfo
{
    public string Name { get; set; }

    public string Storage { get; set; }

    public string Path { get; set; }

    public long FileSizeBytes { get; set; }

    public bool Shared { get; set; }
}