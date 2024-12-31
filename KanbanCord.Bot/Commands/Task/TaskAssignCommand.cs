using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using KanbanCord.Bot.Providers;
using KanbanCord.Bot.Extensions;
using MongoDB.Bson;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("assign")]
    [Description("Assign a task to a user.")]
    public async ValueTask TaskAssignCommand(
        SlashCommandContext context,
        [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task,
        [Description("The user to assign this task to")] DiscordUser assignee)
    {
        var taskItem = await _taskItemRepository.GetTaskItemByObjectIdOrDefaultAsync(new ObjectId(task));
        
        if (taskItem is null)
        {
            var notFoundEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithDescription(
                    "The selected task was not found, please try again.");
            
            await context.RespondAsync(notFoundEmbed);
            return;
        }

        taskItem.AssigneeId = assignee.Id;
        taskItem.LastUpdatedAt = DateTime.UtcNow;
        
        await _taskItemRepository.UpdateTaskItemAsync(taskItem);

        bool directMessageSentToAssignee;

        try
        {
            var assigneeEmbed = new DiscordEmbedBuilder()
                .WithDefaultColor()
                .WithAuthor(context.Guild!.Name, iconUrl: context.Guild.IconUrl)
                .WithTitle("You have been assigned to a Task!")
                .AddField("Task Name", taskItem.Title)
                .AddField("Task Description", taskItem.Description);
            
            await assignee.SendMessageAsync(assigneeEmbed);

            directMessageSentToAssignee = true;
        }
        catch (Exception)
        {
            directMessageSentToAssignee = false;
        }

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithDescription(
                $"The task \"{taskItem.Title}\" has been assigned to {assignee.Mention}.");
        
        var response = new DiscordInteractionResponseBuilder()
            .AddMention(new UserMention(assignee))
            .AddEmbed(embed);
        
        if (!directMessageSentToAssignee)
            response.WithContent(assignee.Mention);
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, response);
    }
}