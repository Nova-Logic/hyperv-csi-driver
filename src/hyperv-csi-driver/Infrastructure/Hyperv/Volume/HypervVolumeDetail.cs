using System;

namespace HypervCsiDriver.Infrastructure.Hyperv.Volume;

public sealed class HypervVolumeDetail
{
    public Guid Id { get; set; } //DiskIdentifier

    public string Name { get; set; }

    public string Storage { get; set; }

    public string Path { get; set; }

    public bool Shared { get; set; }

    public ulong FileSizeBytes { get; set; }

    public ulong SizeBytes { get; set; }

    public uint BlockSizeBytes { get; set; }

    public bool Dynamic { get; set; }

    public bool Attached { get; set; }

    //todo public Guid Parent { get; }

    //maybe FragmentationPercentage
}