﻿// Copyright (c) Polyrific, Inc 2018. All rights reserved.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Polyrific.Catapult.Engine.Core;
using Polyrific.Catapult.Engine.Core.JobTasks;
using Polyrific.Catapult.Shared.Dto.ExternalService;
using Polyrific.Catapult.Shared.Dto.ExternalServiceType;
using Polyrific.Catapult.Shared.Dto.Provider;
using Polyrific.Catapult.Shared.Dto.Project;
using Polyrific.Catapult.Shared.Service;
using Xunit;

namespace Polyrific.Catapult.Engine.UnitTests.Core.JobTasks
{
    public class PullTaskTests
    {
        private readonly Mock<IProjectService> _projectService;
        private readonly Mock<IProjectMemberService> _projectMemberService;
        private readonly Mock<IExternalServiceService> _externalServiceService;
        private readonly Mock<IExternalServiceTypeService> _externalServiceTypeService;
        private readonly Mock<IProviderService> _providerService;
        private readonly Mock<IPluginManager> _pluginManager;
        private readonly Mock<ILogger<PullTask>> _logger;

        public PullTaskTests()
        {
            _projectService = new Mock<IProjectService>();
            _projectService.Setup(s => s.GetProject(It.IsAny<int>()))
                .ReturnsAsync((int id) => new ProjectDto { Id = id, Name = $"Project {id}" });

            _externalServiceService = new Mock<IExternalServiceService>();

            _pluginManager = new Mock<IPluginManager>();
            _pluginManager.Setup(p => p.GetPlugins(It.IsAny<string>())).Returns(new List<PluginItem>
            {
                new PluginItem("FakeCodeRepositoryProvider", "path/to/FakeCodeRepositoryProvider.dll", new string[] { })
            });

            _logger = new Mock<ILogger<PullTask>>();
            
            _externalServiceTypeService = new Mock<IExternalServiceTypeService>();
            _externalServiceTypeService.Setup(s => s.GetExternalServiceTypes(It.IsAny<bool>()))
                .ReturnsAsync(new List<ExternalServiceTypeDto>
                {
                    new ExternalServiceTypeDto
                    {
                        Name = "GitHub",
                        ExternalServiceProperties = new List<ExternalServicePropertyDto>
                        {
                            new ExternalServicePropertyDto
                            {
                                Name = "AuthToken",
                                IsSecret = true
                            }
                        }
                    }
                });
            _providerService = new Mock<IProviderService>();
            _providerService.Setup(s => s.GetProviderAdditionalConfigByProviderName(It.IsAny<string>()))
                .ReturnsAsync(new List<TaskProviderAdditionalConfigDto>
                {
                    new TaskProviderAdditionalConfigDto
                    {
                        Name = "ConnectionString",
                        IsSecret = true
                    }
                });

            _projectMemberService = new Mock<IProjectMemberService>();
        }

        [Fact]
        public async void RunMainTask_Success()
        {
            _pluginManager.Setup(p => p.InvokeTaskProvider(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string pluginDll, string pluginArgs, string secretPluginArgs) => new Dictionary<string, object>
                {
                    {"repositoryLocation", "c:\\test"}
                });

            var config = new Dictionary<string, string>();
                        
            var task = new PullTask(_projectService.Object, _projectMemberService.Object, _externalServiceService.Object, _externalServiceTypeService.Object, _providerService.Object, _pluginManager.Object, _logger.Object);
            task.SetConfig(config, "working");
            task.Provider = "FakeCodeRepositoryProvider";

            var result = await task.RunMainTask(new Dictionary<string, string>());

            Assert.True(result.IsSuccess);
            Assert.Equal("c:\\test", result.ReturnValue);
            Assert.Equal("The repository in c:\\test has been pulled to the latest update", result.TaskRemarks);
        }

        [Fact]
        public async void RunMainTask_Failed()
        {
            _pluginManager.Setup(p => p.InvokeTaskProvider(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string pluginDll, string pluginArgs, string secretPluginArgs) => new Dictionary<string, object>
                {
                    {"errorMessage", "error-message"}
                });

            var config = new Dictionary<string, string>();

            var task = new PullTask(_projectService.Object, _projectMemberService.Object, _externalServiceService.Object, _externalServiceTypeService.Object, _providerService.Object, _pluginManager.Object, _logger.Object);
            task.SetConfig(config, "working");
            task.Provider = "FakeCodeRepositoryProvider";

            var result = await task.RunMainTask(new Dictionary<string, string>());

            Assert.False(result.IsSuccess);
            Assert.Equal("error-message", result.ErrorMessage);
        }

        [Fact]
        public async void RunMainTask_NoProvider()
        {
            var config = new Dictionary<string, string>();

            var task = new PullTask(_projectService.Object, _projectMemberService.Object, _externalServiceService.Object, _externalServiceTypeService.Object, _providerService.Object, _pluginManager.Object, _logger.Object);
            task.SetConfig(config, "working");
            task.Provider = "NotExistRepositoryProvider";

            var result = await task.RunMainTask(new Dictionary<string, string>());

            Assert.False(result.IsSuccess);
            Assert.Equal("Code repository provider \"NotExistRepositoryProvider\" could not be found.", result.ErrorMessage);
        }

        [Fact]
        public async void RunMainTask_AdditionalConfigSecured()
        {
            _pluginManager.Setup(p => p.InvokeTaskProvider(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string pluginDll, string pluginArgs, string secretPluginArgs) => new Dictionary<string, object>
                {
                    {"repositoryLocation", "c:\\test"}
                });
            _pluginManager.Setup(p => p.GetPlugins(It.IsAny<string>())).Returns(new List<PluginItem>
            {
                new PluginItem("FakeCodeRepositoryProvider", "path/to/FakeCodeRepositoryProvider.dll", new string[] { "GitHub" })
            });
            _externalServiceService.Setup(p => p.GetExternalServiceByName(It.IsAny<string>())).ReturnsAsync((string name) => new ExternalServiceDto
            {
                Name = name,
                Config = new Dictionary<string, string>
                {
                    { "AuthToken", "123" }
                }
            });

            var config = new Dictionary<string, string>
            {
                { "GitHubExternalService", "github-test" }
            };

            var task = new PullTask(_projectService.Object, _projectMemberService.Object, _externalServiceService.Object, _externalServiceTypeService.Object, _providerService.Object, _pluginManager.Object, _logger.Object);
            task.SetConfig(config, "working");
            task.Provider = "FakeCodeRepositoryProvider";
            task.AdditionalConfigs = new Dictionary<string, string>
            {
                { "ConnectionString", "Server=localhost;Database=TestProject;User ID=sa;Password=samprod;" }
            };

            var result = await task.RunMainTask(new Dictionary<string, string>());

            Assert.True(result.IsSuccess);
            Assert.Equal("c:\\test", result.ReturnValue);
            Assert.Equal("The repository in c:\\test has been pulled to the latest update", result.TaskRemarks);

            Assert.Equal(2, task.AdditionalConfigs.Count);
            Assert.Equal(2, task.SecuredAdditionalConfigs.Count);
            Assert.Equal("***", task.SecuredAdditionalConfigs["AuthToken"]);
            Assert.Equal("***", task.SecuredAdditionalConfigs["ConnectionString"]);
            Assert.Equal("123", task.AdditionalConfigs["AuthToken"]);
            Assert.Equal("Server=localhost;Database=TestProject;User ID=sa;Password=samprod;", task.AdditionalConfigs["ConnectionString"]);
        }
    }
}
