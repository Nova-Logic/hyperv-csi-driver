namespace HypervCsiDriver.Infrastructure.Hyperv.VolumeRequest;

public sealed class HypervCreateVolumeRequest
{
    public string Name { get; set; } //required

    public string Storage { get; set; } = string.Empty;

    public bool Shared { get; set; } = false;

    public ulong SizeBytes { get; set; } = 10 * 1024 * 1024 * 1024UL; //10GB

    public uint BlockSizeBytes { get; set; } = 1024 * 1024; //1M

    public bool Dynamic { get; set; } = true;

    //todo public Guid Parent { get; set; } = Guid.Empty;
}