using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using KanbanCord.Bot.Providers;
using KanbanCord.Bot.Extensions;
using MongoDB.Bson;

namespace KanbanCord.Bot.Commands.Task;

partial class TaskCommandGroup
{
    [Command("delete")]
    [Description("Delete a task completely, this will skip the archive and never be accessible again.")]
    public async ValueTask TaskDeleteCommand(SlashCommandContext context, [Description("Search for the task to select")] [SlashAutoCompleteProvider<AllTaskItemsAutoCompleteProvider>] string task)
    {
        var taskItem = await _taskItemRepository.GetTaskItemByObjectIdOrDefaultAsync(new ObjectId(task));

        var embed = new DiscordEmbedBuilder()
            .WithDefaultColor();
        
        if (taskItem is null)
        {
            embed.WithDescription("The selected task was not found, please try again.");
            
            await context.RespondAsync(embed);
            return;
        }
        
        var deleteButton = new DiscordButtonComponent(DiscordButtonStyle.Danger, Guid.NewGuid().ToString(), "Delete");
        
        var author = await context.Client.GetUserAsync(taskItem.AuthorId);

        embed
            .WithAuthor("Delete Task")
            .WithDescription("Are you sure you want to delete this task?")
            .AddField("Title:", taskItem.Title)
            .AddField("Description:", taskItem.Description)
            .AddField("Author:", author.Mention);
        
        var responseMessage = new DiscordMessageBuilder()
            .AddEmbed(embed)
            .AddActionRowComponent(deleteButton);
        
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
                    .AddActionRowComponent(deleteButton);
            
                await message.ModifyAsync(timedOutMessage);
                break;
            }
        }
    }
}