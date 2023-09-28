﻿namespace HypervCsiDriver.Hosting;

public sealed class HypervCsiDriverOptions
{
    public HypervCsiDriverType Type { get; set; }

    public string HostName { get; set; }

    public string UserName { get; set; } = "Administrator";

    public string KeyFile { get; set; }

    public string DefaultStorage { get; set; }

    //public ControllerSettings Controller { get; set; }
        
    //public NodeSettings Node { get; set; }

    //public sealed class ControllerSettings
    //{
    //}

    //public sealed class NodeSettings
    //{
    //}
}