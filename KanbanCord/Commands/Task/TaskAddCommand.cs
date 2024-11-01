using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using KanbanCord.Extensions;
using KanbanCord.Helpers;
using KanbanCord.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("add")]
    [Description("Add a task to the backlog")]
    public async ValueTask TaskAddCommand(SlashCommandContext context)
    {
        var modalId = Guid.NewGuid().ToString();
        
        var modal = new DiscordInteractionResponseBuilder()
            .WithCustomId(modalId)
            .WithTitle("Add a new Task")
            .AddComponents(new DiscordTextInputComponent(
                "Title:",
                "titleField",
                "Title of the task",
                max_length: 30))
            .AddComponents(new DiscordTextInputComponent(
                "Description:",
                "descriptionField",
                "Description of the task",
                max_length: 4000,
                style: DiscordTextInputStyle.Paragraph));
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal, modal);
        
        var interaction = context.Client.ServiceProvider.GetRequiredService<InteractivityExtension>();

        var response = await interaction.WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(10));
        
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
                    $"The task has been added to the backlog. View it using {commands.GetMention(["board"])}.");
            
            await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}