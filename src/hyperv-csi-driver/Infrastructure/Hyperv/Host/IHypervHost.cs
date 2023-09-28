using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HypervCsiDriver.Infrastructure.Hyperv.VirtualMachine;
using HypervCsiDriver.Infrastructure.Hyperv.Volume;
using HypervCsiDriver.Infrastructure.Hyperv.VolumeRequest;

namespace HypervCsiDriver.Infrastructure.Hyperv.Host;

public interface IHypervHost
{
    Task<HypervVolumeDetail> CreateVolumeAsync(HypervCreateVolumeRequest request, CancellationToken cancellationToken = default);

    Task DeleteVolumeAsync(HypervDeleteVolumeRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<HypervVolumeInfo> GetVolumesAsync(HypervVolumeFilter filter);

    Task<HypervVolumeDetail> GetVolumeAsync(string path, CancellationToken cancellationToken = default);

    IAsyncEnumerable<HypervVirtualMachineInfo> GetVirtualMachinesAsync(HypervVirtualMachineFilter filter);

    Task<HypervVirtualMachineVolumeInfo> AttachVolumeAsync(HypervAttachVolumeRequest request, CancellationToken cancellationToken = default);

    Task DetachVolumeAsync(HypervDetachVolumeRequest request, CancellationToken cancellationToken = default);

    IAsyncEnumerable<HypervVirtualMachineVolumeInfo> GetVirtualMachineVolumesAsync(Guid vmId, HypervVirtualMachineVolumeFilter filter);

    IAsyncEnumerable<HypervVolumeFlowInfo> GetVolumeFlowsAsnyc(HypervVolumeFlowFilter filter);
}