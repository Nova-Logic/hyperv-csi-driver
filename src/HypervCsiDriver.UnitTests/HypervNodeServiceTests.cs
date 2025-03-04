using HypervCsiDriver.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Runspaces;
using System.Reactive.Linq;
using System.Threading.Tasks;
using HypervCsiDriver.Infrastructure.Hyperv.Node;
using Xunit;

namespace HypervCsiDriver.UnitTests
{
    public sealed class HypervNodeServiceFixture : IDisposable
    {
        public IConfiguration Configuration { get; set; }

        IHypervNodeService _service;

        public HypervNodeServiceFixture()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<HypervNodeServiceFixture>()
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public async Task<IHypervNodeService> GetNodeServiceAsync(string hostName)
        {
            if (_service is null)
            {
                //todo read config  Token=Configuration["somesection:somekey"]

                _service = new LinuxNodeService(hostName, "root", null)
                {
                };
                //await services.ConnectAsync();
            }
            return _service;
        }

        public void Dispose()
        {
            (_service as IDisposable)?.Dispose();
        }
    }


    [Trait("Type", "Integration")]
    [Trait("Category", "HypervNodeService")]
    public sealed class HypervNodeServiceTests : IClassFixture<HypervNodeServiceFixture>
    {
        public HypervNodeServiceFixture Fixture { get; }

        public HypervNodeServiceTests(HypervNodeServiceFixture fixture)
        {
            Fixture = fixture;
        }

        [Theory]
        //[InlineData("lnx1514", "test-volume-longname12345", 0, 4, "/mnt/testvol")]
        [InlineData("lnx1521", "test", "0B6E78B9-2A04-49DE-B4B1-5FEE759D7106",  0, 1, "/drivetest")]
        //[InlineData("lnx1519", "grafana-01", 0, 2, "/mnt/grafana-01")]
        //[InlineData("lnx1519", "influxdb-01", 0, 3, "/mnt/influxdb-01")]
        //[InlineData("lnx1519", "mssql-01", 0, 1, "/mnt/mssql-01")]
        public async Task mount_device(string hostName, string name, Guid vhdId, int countrollerNumber, int controllerLocation, string targetPath)
        {
            var service = await Fixture.GetNodeServiceAsync(hostName);

            await service.MountDeviceAsync(new HypervNodeMountRequest
            {
                Name = name,
                VhdId = vhdId,
                ControllerNumber = countrollerNumber,
                ControllerLocation = controllerLocation,
                FSType = "ext4",
                Options = Array.Empty<string>(),
                Readonly = false,
                TargetPath = targetPath,
                ValidateLabel = false
            });
        }

        [Theory]
        //[InlineData("lnx1521", "test", "/drivetest")]
        [InlineData("lnx1514", "test-volume-longname12345", "/mnt/testvol")]
        public async Task unmount_device(string hostName, string name, string targetPath)
        {
            var service = await Fixture.GetNodeServiceAsync(hostName);

            await service.UnmountDeviceAsync(new HypervNodeUnmountRequest
            {
                Name = name,
                TargetPath = targetPath
            });
        }

        [Theory]
        [InlineData("lnx1521", "test", "/drivetest", "/publishtest")]
        public async Task publish_device(string hostName, string name, string stagePath, string publishPath)
        {
            var service = await Fixture.GetNodeServiceAsync(hostName);

            await service.PublishDeviceAsync(new HypervNodePublishRequest
            {
                Name = name,
                StagingTargetPath = stagePath,
                PublishTargetPath = publishPath
            });
        }

        [Theory]
        [InlineData("lnx1521", "test", "/drivetest", "/publishtest")]
        public async Task unpublish_device(string hostName, string name, string stagePath, string publishPath)
        {
            var service = await Fixture.GetNodeServiceAsync(hostName);

            await service.UnpublishDeviceAsync(new HypervNodeUnpublishRequest
            {
                Name = name, 
                TargetPath = publishPath
            });
        }


    }
}
