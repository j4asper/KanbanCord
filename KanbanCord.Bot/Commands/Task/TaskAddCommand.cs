using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Bot.Extensions;
using KanbanCord.Bot.Helpers;
using KanbanCord.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("add")]
    [Description("Add a task to the backlog")]
    public async ValueTask TaskAddCommand(SlashCommandContext context)
    {
        var modal = new DiscordInteractionResponseBuilder()
            .WithCustomId(Guid.NewGuid().ToString())
            .WithTitle("Add a new Task")
            .AddComponents(new DiscordTextInputComponent(
                "Title:",
                "titleField",
                "Title of the task",
                max_length: 40))
            .AddComponents(new DiscordTextInputComponent(
                "Description:",
                "descriptionField",
                "Description of the task",
                max_length: 600,
                style: DiscordTextInputStyle.Paragraph));
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        
        var interaction = context.Client.ServiceProvider.GetRequiredService<InteractivityExtension>();

        var response = await interaction.WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(5));
        
        if (!response.TimedOut)
        {
            var modalInteraction = response.Result.Values;
            
            var newTask = new TaskItem
            {
                GuildId = context.Guild!.Id,
                Title = modalInteraction["titleField"],
                Description = modalInteraction["descriptionField"],
                AuthorId = context.User.Id,
            };
            
            await _taskItemRepository.AddTaskItemAsync(newTask);

            var commands = await context.Client.GetGlobalApplicationCommandsAsync();
            
            var embed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    $"The task \"{newTask.Title}\" has been added to the backlog. View it using {commands.GetMention(["board"])}.");
            
            await response.Result.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AddEmbed(embed));
        }
    }
}