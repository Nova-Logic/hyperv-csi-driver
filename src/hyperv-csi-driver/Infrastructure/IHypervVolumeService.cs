using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HypervCsiDriver.Infrastructure.Hyperv.VirtualMachine;
using HypervCsiDriver.Infrastructure.Hyperv.Volume;
using HypervCsiDriver.Infrastructure.Hyperv.VolumeRequest;

namespace HypervCsiDriver.Infrastructure;

public interface IHypervVolumeService
{
    IAsyncEnumerable<HypervVolumeInfo> GetVolumesAsync(HypervVolumeFilter filter);

    Task<HypervVolumeDetail> GetVolumeAsync(string path, CancellationToken cancellationToken = default);

    Task<HypervVolumeDetail> GetVolumeAsync(string path, string? hostName, CancellationToken cancellationToken = default);

    Task<HypervVolumeDetail> CreateVolumeAsync(HypervCreateVolumeRequest request, CancellationToken cancellationToken = default);

    Task DeleteVolumeAsync(HypervDeleteVolumeRequest request, CancellationToken cancellationToken = default);

    Task<HypervVirtualMachineVolumeInfo> AttachVolumeAsync(HypervAttachVolumeRequest request, CancellationToken cancellationToken = default);

    Task DetachVolumeAsync(HypervDetachVolumeRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<HypervVirtualMachineVolumeInfo> GetVirtualMachineVolumesAsync(Guid vmId, HypervVirtualMachineVolumeFilter filter);

    IAsyncEnumerable<HypervVirtualMachineInfo> GetVirtualMachinesAsync(HypervVirtualMachineFilter filter, CancellationToken cancellationToken = default);
    IAsyncEnumerable<HypervVolumeFlowInfo> GetVolumeFlowsAsnyc(HypervVolumeFlowFilter filter);

    IAsyncEnumerable<HypervVolumeDetailResult> GetVolumeDetailsAsync(IEnumerable<HypervVolumeInfo> volumes, CancellationToken cancellationToken = default);
}