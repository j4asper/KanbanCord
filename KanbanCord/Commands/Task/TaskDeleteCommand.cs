using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using KanbanCord.Extensions;
using KanbanCord.Providers;
using MongoDB.Bson;

namespace KanbanCord.Commands.Task;

partial class TaskCommandGroup
{
    [Command("delete")]
    [Description("Delete a task completely, this will skip the archive and never be accessible again.")]
    public async ValueTask TaskDeleteCommand(SlashCommandContext context, [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task)
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
        
        var deleteButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, Guid.NewGuid().ToString(), "Delete");
        
        var author = await context.Client.GetUserAsync(taskItem.AuthorId);

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor()
            .WithAuthor("Delete Task")
            .WithDescription("Are you sure you want to delete this task?")
            .AddField("Title:", taskItem.Title)
            .AddField("Description:", taskItem.Description)
            .AddField("Author:", author.Mention);
        
        var responseMessage = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddComponents(deleteButton);
        
        await context.RespondAsync(responseMessage);
        
        var message = await context.Interaction.GetOriginalResponseAsync();

        var response = await message.WaitForButtonAsync();

        switch (response.TimedOut)
        {
            case false when response.Result.Id == deleteButton.CustomId && response.Result.User.Id == context.User.Id:
            {
                await _taskItemRepository.RemoveTaskItemAsync(taskItem);
            
                var deletedEmbed = new DiscordEmbedBuilder()
                    .WithDefaultColor()
                    .WithDescription($"The task \"{taskItem.Title}\" has been deleted.");
            
                await response.Result.Interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(deletedEmbed));

                return;
            }
            case true:
            {
                deleteButton.Disable();
            
                var timedOutMessage = new DiscordMessageBuilder()
                    .AddEmbed(embed)
                    .AddComponents(deleteButton);
            
                await message.ModifyAsync(timedOutMessage);
                break;
            }
        }
    }
}