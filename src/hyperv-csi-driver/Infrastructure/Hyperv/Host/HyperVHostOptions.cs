namespace HypervCsiDriver.Infrastructure.Hyperv.Host;

public sealed class HyperVHostOptions
{
    public string HostName { get; set; }
        
    public string UserName { get; set; }

    public string KeyFile { get; set; }

    public string DefaultStorage { get; set; } = string.Empty;
}