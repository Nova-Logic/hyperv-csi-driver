using System.Threading;
using System.Threading.Tasks;

namespace HypervCsiDriver.Infrastructure.Hyperv.Node;

public interface IHypervNodeService
{
    Task MountDeviceAsync(HypervNodeMountRequest request, CancellationToken cancellationToken = default);

    Task UnmountDeviceAsync(HypervNodeUnmountRequest request, CancellationToken cancellationToken = default);

    Task PublishDeviceAsync(HypervNodePublishRequest request, CancellationToken cancellationToken = default);

    Task UnpublishDeviceAsync(HypervNodeUnpublishRequest request, CancellationToken cancellationToken = default);

}