using HypervCsiDriver.Automation;
using HypervCsiDriver.Infrastructure.Hyperv.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using HypervCsiDriver.Automation;
using HypervCsiDriver.Infrastructure.Hyperv.Host;
using Microsoft.Extensions.Logging;
using HypervCsiDriver.Automation;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace HypervCsiDriver.Infrastructure.Cluster;

public class Cluster
{
    readonly ILogger _logger;
    readonly PNetPowerShell _power;

    public string Name;
    public HypervHost[] ClusterNodes;
    public string SharedVolumesRoot;
    public string Domain;
    /*
    public Cluster(IOptions<HypervHost> options, ILogger<Cluster>? logger)
    {
        var opt = options.Value;

        _power = new PNetPowerShell(opt.HostName, opt.UserName, opt.KeyFile);
        _hostName = opt.HostName;
        _defaultStorage = opt.DefaultStorage;

        _logger = logger ?? (ILogger)NullLogger.Instance;
    }
    */
} 