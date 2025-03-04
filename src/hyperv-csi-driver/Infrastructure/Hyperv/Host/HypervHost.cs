﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using HypervCsiDriver.Automation;
using HypervCsiDriver.Infrastructure.Hyperv.VirtualMachine;
using HypervCsiDriver.Infrastructure.Hyperv.Volume;
using HypervCsiDriver.Infrastructure.Hyperv.VolumeRequest;
using HypervCsiDriver.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HypervCsiDriver.Infrastructure.Hyperv.Host;

public sealed class HypervHost : IHypervHost, IDisposable
{
    readonly PNetPowerShell _power;

    readonly string _hostName;

    readonly ILogger _logger;

    readonly string _defaultStorage;

    public HypervHost(IOptions<HyperVHostOptions> options, ILogger<HypervHost>? logger)
    {
        var opt = options.Value;

        _power = new PNetPowerShell(opt.HostName, opt.UserName, opt.KeyFile);
        _hostName = opt.HostName;
        _defaultStorage = opt.DefaultStorage;

        _logger = logger ?? (ILogger)NullLogger.Instance;
    }

    public async Task<HypervVolumeDetail> CreateVolumeAsync(HypervCreateVolumeRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.Name))
            throw new ArgumentNullException(nameof(request.Name));

        using var scope = _logger.BeginScope("create volume {VolumeName} on {HostName}", request.Name, _hostName);

        //todo VHDSet switch
        if (request.Shared)
            throw new NotImplementedException("shared disk not implemented");

        //the smallest valid size for a virtual hard disk is 3MB.
        var sizeBytes = Math.Max(request.SizeBytes, 3 * 1024 * 1024);

        //align size to 4096
        sizeBytes = sizeBytes % 4096 > 0 ? sizeBytes + 4096 - (sizeBytes % 4096) : sizeBytes;

        var name = request.Name;
        var storage = request.Storage;

        //find free storage
        if (string.IsNullOrEmpty(storage))
            storage = await FindFreeStoragesAsync(sizeBytes).FirstOrDefaultAsync(cancellationToken);

        //use default storage
        if (string.IsNullOrEmpty(storage))
            storage = _defaultStorage;

        //storage required
        if (string.IsNullOrEmpty(storage))
            throw new InvalidOperationException("no storage found or specified");

        //todo check storage free space

        //handle windows Path under linux
        storage = storage.ToLower();

        var path = $@"{HypervDefaults.ClusterStoragePath}\{storage}\Volumes\{name}.vhdx";

        Command cmd;
        var commands = new List<Command>(2);

        _logger.LogInformation("creating volume '{VolumePath}' with size {VolumeSizeBytes}", path, sizeBytes);

        cmd = new Command("New-VHD");
        cmd.Parameters.Add("Path", path);
        cmd.Parameters.Add("SizeBytes", sizeBytes);
        cmd.Parameters.Add("Dynamic", request.Dynamic);
        cmd.Parameters.Add("BlockSizeBytes", request.BlockSizeBytes);
        cmd.Parameters.Add("LogicalSectorSizeBytes", 4096);
        cmd.Parameters.Add("PhysicalSectorSizeBytes", 4096);
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] { "DiskIdentifier", "Path", "FileSize", "Size", "BlockSize", "VhdType", "Attached" });
        //todo ParentPath, FragmentationPercentage, VHDFormat
        commands.Add(cmd);

        dynamic item = await _power.InvokeAsync(commands).ThrowOnError().FirstAsync(cancellationToken);

        return new HypervVolumeDetail
        {
            Id = Guid.Parse((string)item.DiskIdentifier),
            //Name = Path.GetFileNameWithoutExtension((string)item.Path),
            Name = name,
            Path = item.Path,
            FileSizeBytes = item.FileSize,
            SizeBytes = item.Size,
            Attached = item.Attached,
            BlockSizeBytes = item.BlockSize,
            Dynamic = item.VhdType switch
            {
                "Dynamic" => true,
                _ => false
            },
            //Storage = Directory.GetParent((string)item.Path).Parent.Name,
            Storage = storage,
            Shared = false //todo .vhds                    
        };
    }

    public async Task DeleteVolumeAsync(HypervDeleteVolumeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Id == Guid.Empty)
            throw new ArgumentNullException(nameof(request.Id));
        if (string.IsNullOrEmpty(request.Path))
            throw new ArgumentNullException(nameof(request.Path));

        using var scope = _logger.BeginScope("delete volume {VolumePath} on {HostName}", request.Path, _hostName);

        //maybe check path in storage 

        Command cmd;
        var commands = new List<Command>(3);

        _logger.LogInformation("deleting volume '{VolumePath}'", request.Path);

        //todo VHDSet switch
        //todo Snapshots check

        cmd = new Command("Get-VHD");
        cmd.Parameters.Add("Path", request.Path);
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] { "DiskIdentifier", "Path", "FileSize", "Size", "BlockSize", "VhdType", "Attached" });
        //todo ParentPath, FragmentationPercentage, VHDFormat
        commands.Add(cmd);

        cmd = new Command("Remove-Item");
        commands.Add(cmd);

        var result = await _power.InvokeAsync(commands).ThrowOnError().FirstOrDefaultAsync(cancellationToken);

        //todo check result
    }

    public async Task<HypervVolumeDetail> GetVolumeAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        using var scope = _logger.BeginScope("get volume {VolumePath} on {HostName}", path, _hostName);

        //maybe check path in storage 

        Command cmd;
        var commands = new List<Command>(2);

        _logger.LogDebug("get volume '{VolumePath}'", path);

        //todo VHDSet switch

        cmd = new Command("Get-VHD");
        cmd.Parameters.Add("Path", path);
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] { "DiskIdentifier", "Path", "FileSize", "Size", "BlockSize", "VhdType", "Attached" });
        //todo ParentPath, FragmentationPercentage, VHDFormat
        commands.Add(cmd);

        try
        {
            dynamic item = await _power.InvokeAsync(commands).ThrowOnError()
                .FirstAsync(cancellationToken);

            return new HypervVolumeDetail
            {
                Id = Guid.Parse((string)item.DiskIdentifier),
                Name = HypervUtils.GetFileNameWithoutExtension((string)item.Path),
                Path = item.Path,
                FileSizeBytes = item.FileSize,
                SizeBytes = item.Size,
                Attached = item.Attached,
                BlockSizeBytes = item.BlockSize,
                Dynamic = item.VhdType switch
                {
                    "Dynamic" => true,
                    _ => false
                },
                Storage = HypervUtils.GetStorageNameFromPath((string)item.Path),
                Shared = false //todo .vhds                    
            };
        } 
        catch(RemoteException ex)
        {
            //todo
            //Getting the mounted storage instance for the path 'C:\ClusterStorage\hv05\Volumes\pvc-XXX.vhdx' failed.
            //The operation cannot be performed while the object is in use.

            throw;
        }            
    }

    public IAsyncEnumerable<HypervVolumeInfo> GetVolumesAsync(HypervVolumeFilter filter = null)
    {
        using var scope = _logger.BeginScope("get volumes on {HostName}", _hostName);

        Command cmd;
        var commands = new List<Command>(5);

        _logger.LogDebug("get volumes");

        cmd = new Command("Get-ChildItem");
        cmd.Parameters.Add("Path", HypervDefaults.ClusterStoragePath);
        if (!string.IsNullOrEmpty(filter?.Storage))
            cmd.Parameters.Add("Filter", filter.Storage);
        commands.Add(cmd);

        cmd = new Command("Get-ChildItem");
        cmd.Parameters.Add("Filter", "Volumes");
        commands.Add(cmd);

        cmd = new Command("Get-ChildItem");
        if (!string.IsNullOrEmpty(filter?.Name))
            cmd.Parameters.Add("Filter", $"{filter.Name}.*");
        commands.Add(cmd);

        cmd = new Command("Where-Object"); //maybe over script to include .vhds
        cmd.Parameters.Add("Property", "Extension");
        cmd.Parameters.Add("eq");
        cmd.Parameters.Add("Value", ".vhdx");
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] { "BaseName", "FullName", "Length" });
        commands.Add(cmd);

        foreach (var command in commands)
        {
           var paramsString = string.Join(" ", command.Parameters.Select(p => $"-{p.Name} {p.Value}"));
           _logger.LogDebug($"Command is: {command}, Params are: {paramsString} ");
        }
        
        return _power.InvokeAsync(commands).ThrowOnError()
            .Select((dynamic n) => new HypervVolumeInfo
            {
                Name = n.BaseName,
                Path = n.FullName,
                FileSizeBytes = n.Length,
                Storage = HypervUtils.GetStorageNameFromPath(n.FullName),
                Shared = false //todo .vhds                    
            });
    }

    public IAsyncEnumerable<HypervVirtualMachineInfo> GetVirtualMachinesAsync(HypervVirtualMachineFilter filter)
    {
        using var scope = _logger.BeginScope("get virtual machines on {HostName}", _hostName);

        Command cmd;
        var commands = new List<Command>(2);

        _logger.LogDebug("get virtual machine");

        cmd = new Command("Get-VM");
        if (filter?.Id != Guid.Empty)
            cmd.Parameters.Add("Id", filter.Id);
        if (!string.IsNullOrEmpty(filter?.Name))
            cmd.Parameters.Add("Name", filter.Name);
        cmd.Parameters.Add("ErrorAction", "SilentlyContinue");
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] { "Id", "Name", "ComputerName" });
        commands.Add(cmd);

        return _power.InvokeAsync(commands).ThrowOnError()
            .Select((dynamic n) => new HypervVirtualMachineInfo
            {
                Id = n.Id,
                Name = n.Name,
                Host = n.ComputerName
            });
    }

    public async Task<HypervVirtualMachineVolumeInfo> AttachVolumeAsync(HypervAttachVolumeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.VMId == Guid.Empty)
            throw new ArgumentNullException(nameof(request.VMId));
        if (string.IsNullOrEmpty(request.VolumePath))
            throw new ArgumentNullException(nameof(request.VolumePath));
        if (!string.IsNullOrEmpty(request.Host) && !StringComparer.OrdinalIgnoreCase.Equals(_hostName, request.Host))
            throw new ArgumentException(nameof(request.Host));

        using var scope = _logger.BeginScope("attach volume {VolumePath} to {VirtialMachineId} on {HostName}", request.VolumePath, request.VMId, _hostName);

        //maybe check path in storage 

        Command cmd;
        var commands = new List<Command>(3);

        _logger.LogInformation("attach volume {VolumePath} to {VirtialMachineId}", request.VolumePath, request.VMId);

        //todo VHDSet switch

        //Passthru not possible since pwsh 7
        //Error: Add-VMHardDiskDrive: The Update-ClusterVirtualMachineConfiguration command could not be completed.
        //Error: Set-VMHardDiskDrive: The Update-ClusterVirtualMachineConfiguration command could not be completed.

        //cmd = new Command("Get-VM");
        //cmd.Parameters.Add("Id", request.VMId);
        //commands.Add(cmd);

        //cmd = new Command("Add-VMHardDiskDrive");
        //cmd.Parameters.Add("Path", request.VolumePath);
        //cmd.Parameters.Add("Passthru");
        //commands.Add(cmd);

        //cmd = new Command("Select-Object");
        //cmd.Parameters.Add("Property", new[] {
        //    "VMId", "VMName", "ComputerName", "Path",
        //    "ControllerNumber", "ControllerLocation" 
        //    //todo VMSnapshotId, VMSnapshotName, MaximumIOPS, MinimumIOPS
        //});
        //commands.Add(cmd);


        //lookup free disk drive
            
        cmd = new Command("Get-VM");
        cmd.Parameters.Add("Id", request.VMId);
        commands.Add(cmd);

        cmd = new Command("Get-VMHardDiskDrive");
        cmd.Parameters.Add("ControllerType", "SCSI");
        commands.Add(cmd);

        cmd = new Command("Where-Object");
        cmd.Parameters.Add("Property", "Path");
        cmd.Parameters.Add("not");
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("First", 1);
        cmd.Parameters.Add("Property", new[] {
            "ControllerType", "ControllerLocation", "ControllerNumber"
        });
        commands.Add(cmd);

        dynamic freeDrive = await _power.InvokeAsync(commands)
            .FirstOrDefaultAsync(cancellationToken);

        commands.Clear();


        cmd = new Command("Get-VM");
        cmd.Parameters.Add("Id", request.VMId);
        commands.Add(cmd);

        if (freeDrive is null)
        {
            cmd = new Command("Add-VMHardDiskDrive");
            cmd.Parameters.Add("Path", request.VolumePath);
            cmd.Parameters.Add("ErrorAction", "SilentlyContinue");
            //MaximumIOPS, MinimumIOPS
            commands.Add(cmd);
        } 
        else
        {
            cmd = new Command("Get-VMHardDiskDrive");
            cmd.Parameters.Add("ControllerType", freeDrive.ControllerType);
            cmd.Parameters.Add("ControllerNumber", freeDrive.ControllerNumber);
            cmd.Parameters.Add("ControllerLocation", freeDrive.ControllerLocation);
            //MaximumIOPS, MinimumIOPS
            commands.Add(cmd);

            cmd = new Command("Set-VMHardDiskDrive");
            cmd.Parameters.Add("Path", request.VolumePath);
            cmd.Parameters.Add("ErrorAction", "SilentlyContinue");
            //MaximumIOPS, MinimumIOPS
            commands.Add(cmd);
        }

        _ = await _power.InvokeAsync(commands).LastOrDefaultAsync(cancellationToken);



        commands.Clear();

        cmd = new Command("Get-VM");
        cmd.Parameters.Add("Id", request.VMId);
        commands.Add(cmd);

        cmd = new Command("Get-VMHardDiskDrive");
        commands.Add(cmd);

        cmd = new Command("Where-Object");
        cmd.Parameters.Add("Property", "Path");
        cmd.Parameters.Add("eq");
        cmd.Parameters.Add("Value", request.VolumePath);
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("First", 1);
        cmd.Parameters.Add("Property", new[] {
            "VMId", "VMName", "ComputerName", "Path",
            "ControllerNumber", "ControllerLocation" 
            //todo VMSnapshotId, VMSnapshotName, MaximumIOPS, MinimumIOPS
        });
        commands.Add(cmd);

        //bug: return of invalid location (before free disk drive usage)
        //add-hard-disk on a SCSI controller with already 64 disks attached succeeded
        //and get-hard-disk returned ControllerLocation 63,
        //expected invalid ControllerLocation was 64 (SCSI max is 63)
        //on manual expection the drive path was empty
        //result: volumeattachments[Attached: true, Controller Number: 0, Controller Location:  63]  


        //sometimes attached disk can't be found immediately
        var retry = 5;

        do
        {
            dynamic? item = await _power.InvokeAsync(commands).ThrowOnError()
                .FirstOrDefaultAsync(cancellationToken);
                
            if(item is not null)
                return new HypervVirtualMachineVolumeInfo
                {
                    VMId = item.VMId,
                    VMName = item.VMName,
                    VolumeName = HypervUtils.GetFileNameWithoutExtension((string)item.Path),
                    VolumePath = item.Path,
                    Host = item.ComputerName,
                    ControllerNumber = item.ControllerNumber,
                    ControllerLocation = item.ControllerLocation
                };

            await Task.Delay(3000);
        }
        while (--retry >= 0 && !cancellationToken.IsCancellationRequested);

        throw new TaskCanceledException("disk attach in progress");
    }

    public async Task DetachVolumeAsync(HypervDetachVolumeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.VMId == Guid.Empty)
            throw new ArgumentNullException(nameof(request.VMId));
        if (string.IsNullOrEmpty(request.VolumePath))
            throw new ArgumentNullException(nameof(request.VolumePath));

        using var scope = _logger.BeginScope("detach volume {VolumePath} to {VirtialMachineId} on {HostName}", request.VolumePath, request.VMId, _hostName);

        //maybe check path in storage 

        Command cmd;
        var commands = new List<Command>(4);

        _logger.LogInformation("detach volume {VolumePath} to {VirtialMachineId}", request.VolumePath, request.VMId);

        //todo VHDSet switch

        cmd = new Command("Get-VM");
        cmd.Parameters.Add("Id", request.VMId);
        commands.Add(cmd);

        cmd = new Command("Get-VMHardDiskDrive");
        commands.Add(cmd);

        cmd = new Command("Where-Object"); //maybe over script to include .vhds
        cmd.Parameters.Add("Property", "Path");
        cmd.Parameters.Add("eq");
        cmd.Parameters.Add("Value", request.VolumePath);
        commands.Add(cmd);

        //since pwsh 7
        //Error: Remove-VMHardDiskDrive: The Update-ClusterVirtualMachineConfiguration command could not be completed.

        cmd = new Command("Remove-VMHardDiskDrive");
        cmd.Parameters.Add("ErrorAction", "SilentlyContinue");
        commands.Add(cmd);

        _ = await _power.InvokeAsync(commands).LastOrDefaultAsync(cancellationToken);

        //workaround Update-ClusterVirtualMachineConfiguration error
        commands.Clear();

        cmd = new Command("Get-VM");
        cmd.Parameters.Add("Id", request.VMId);
        commands.Add(cmd);

        cmd = new Command("Get-VMHardDiskDrive");
        commands.Add(cmd);

        cmd = new Command("Where-Object"); //maybe over script to include .vhds
        cmd.Parameters.Add("Property", "Path");
        cmd.Parameters.Add("eq");
        cmd.Parameters.Add("Value", request.VolumePath);
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] { "Path",
            "ControllerNumber", "ControllerLocation"
        });
        commands.Add(cmd);


        var retry = 5;

        do
        {
            var result = await _power.InvokeAsync(commands).LastOrDefaultAsync(cancellationToken);

            if (result is null)
                return;

            await Task.Delay(3000);
        }
        while (--retry >= 0 && !cancellationToken.IsCancellationRequested);

        throw new TaskCanceledException("disk detach in progress");
    }

    public IAsyncEnumerable<HypervVirtualMachineVolumeInfo> GetVirtualMachineVolumesAsync(Guid vmId, HypervVirtualMachineVolumeFilter filter)
    {
        if (vmId == Guid.Empty)
            throw new ArgumentNullException(nameof(vmId));

        using var scope = _logger.BeginScope("get virtual machine volumes {VirtialMachineId} on {HostName}", vmId, _hostName);


        //maybe check path in storage 
        //todo VHDSet switch

        Command cmd;
        var commands = new List<Command>(4);

        _logger.LogDebug("get virtual machine volumes of {VirtialMachineId}", vmId);

        cmd = new Command("Get-VM");
        cmd.Parameters.Add("Id", vmId);
        commands.Add(cmd);

        cmd = new Command("Get-VMHardDiskDrive");
        commands.Add(cmd);

        if (!string.IsNullOrEmpty(filter?.VolumePath))
        {
            cmd = new Command("Where-Object");
            cmd.Parameters.Add("Property", "Path");
            cmd.Parameters.Add("eq");
            cmd.Parameters.Add("Value", filter.VolumePath);
            commands.Add(cmd);
        }

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] {
            "VMId", "VMName", "ComputerName", "Path",
            "ControllerNumber", "ControllerLocation" 
            //todo VMSnapshotId, VMSnapshotName, MaximumIOPS, MinimumIOPS
        });
        commands.Add(cmd);

        return _power.InvokeAsync(commands).ThrowOnError()
            .Select((dynamic n) => new HypervVirtualMachineVolumeInfo
            {
                VMId = n.VMId,
                VMName = n.VMName,
                VolumeName = HypervUtils.GetFileNameWithoutExtension((string)n.Path),
                VolumePath = n.Path,
                Host = n.ComputerName,
                ControllerNumber = n.ControllerNumber,
                ControllerLocation = n.ControllerLocation
            });
    }

    public IAsyncEnumerable<HypervVolumeFlowInfo> GetVolumeFlowsAsnyc(HypervVolumeFlowFilter filter)
    {
        using var scope = _logger.BeginScope("get volume flows on {HostName}", _hostName);

        Command cmd;
        var commands = new List<Command>(2);

        _logger.LogDebug("get volume flows");

        cmd = new Command("Get-StorageQoSFlow");
        if (filter != null && filter.VMId != Guid.Empty)
            cmd.Parameters.Add("InitiatorId", filter.VMId);
        if (!string.IsNullOrEmpty(filter?.VMName))
            cmd.Parameters.Add("InitiatorName", filter.VMName);
        if (!string.IsNullOrEmpty(filter?.VolumePath))
            cmd.Parameters.Add("FilePath", filter.VolumePath);
        //maybe cmd.Parameters.Add("Status", "Ok");
        commands.Add(cmd);

        cmd = new Command("Select-Object");
        cmd.Parameters.Add("Property", new[] {
            "InitiatorId", "InitiatorName", "InitiatorNodeName", "FilePath"
            //todo IOPS, MaximumIOPS, MinimumIOPS
        });
        commands.Add(cmd);

        return _power.InvokeAsync(commands).ThrowOnError()
            .Select((dynamic n) => new HypervVolumeFlowInfo
            {
                VMId = n.InitiatorId,
                VMName = n.InitiatorName,
                Host = n.InitiatorNodeName,
                Path = n.FilePath
            });
    }

    async IAsyncEnumerable<string> FindFreeStoragesAsync(ulong requiredSize)
    {
        //todo cluster query
        /*
        Invoke-WinCommand -ScriptBlock {
            Get-ClusterSharedVolume | Select-Object Name,OwnerNode -ExpandProperty SharedVolumeInfo | ForEach-Object {
                $csv = $_
                New-Object PSObject -Property @{
                    Name = $csv.Name
                    Owner = $csv.OwnerNode
                    Path = $csv.FriendlyVolumeName
                    Size = $csv.Partition.Size
                    FreeSpace = $csv.Partition.FreeSpace
                    UsedSpace = $csv.Partition.UsedSpace
                    PercentFree = $csv.Partition.PercentFree
                }
            }
        }

        //todo filter by csv.State=Online,
        //csv.SharedVolumeInfo { MaintenanceMode=False, FaultState=NoFaults }
        */

        yield break; //todo free storage lookup
    }

    public void Dispose()
    {
        _power.Dispose();
    }
}