﻿// Copyright (c) Polyrific, Inc 2018. All rights reserved.

using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Polyrific.Catapult.Cli.Extensions;
using Polyrific.Catapult.Shared.Service;

namespace Polyrific.Catapult.Cli.Commands.Project
{
    [Command("remove", Description = "Remove a project")]
    public class RemoveCommand : BaseCommand
    {
        private readonly IProjectService _projectService;
        private readonly IJobDefinitionService _jobDefinitionService;

        public RemoveCommand(IConsole console, ILogger<RemoveCommand> logger, 
            IProjectService projectService, IJobDefinitionService jobDefinitionService) : base(console, logger)
        {
            _projectService = projectService;
            _jobDefinitionService = jobDefinitionService;
        }

        [Required]
        [Option("-n|--name <NAME>", "Name of the project", CommandOptionType.SingleValue)]
        public string Name { get; set; }

        [Option("-ac|--autoconfirm", "Execute the command without the need of confirmation prompt", CommandOptionType.NoValue)]
        public bool AutoConfirm { get; set; }

        [Option("-rs|--remove-resources", "Remove the related resources as well without the need of confirmation prompt", CommandOptionType.NoValue)]
        public bool RemoveResources { get; set; }

        public override string Execute()
        {
            if (!(AutoConfirm || Console.GetYesNo($"Are you sure you want to remove project {Name}?", false)))
                return string.Empty;

            Console.WriteLine($"Trying to remove project {Name}...");

            string message;

            var project = _projectService.GetProjectByName(Name).Result;
            if (project != null)
            {
                var deletionJobDefinition = _jobDefinitionService.GetDeletionJobDefinition(project.Id).Result;
                if (deletionJobDefinition != null && 
                    (RemoveResources || Console.GetYesNo("Do you want to remove the related resources as well?", false)))
                {
                    _projectService.MarkProjectDeleting(project.Id).Wait();
                    message = $"Project {Name} is being removed. You will be notified once the process has been done";
                }
                else
                {
                    _projectService.DeleteProject(project.Id).Wait();
                    message = $"Project {Name} has been removed successfully";
                }

                Logger.LogInformation(message);
            }
            else
            {
                message = $"Project {Name} was not found";
            }

            return message;
        }
    }
}
