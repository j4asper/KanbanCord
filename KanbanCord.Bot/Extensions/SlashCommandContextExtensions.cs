using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

namespace KanbanCord.Bot.Extensions;

public static class SlashCommandContextExtensions
{
    public static async Task SendSimplePaginatedMessage(this SlashCommandContext context, IReadOnlyList<Page> pages, IReadOnlyList<DiscordComponent>? additionalComponents = null)
    {
        var pageCount = pages.Count;
        var currentPage = 1;

        var pageEmbeds = pages.Select(x => x.Embed).ToArray();
        
        var forwardBtn = CreateForwardButton(Guid.NewGuid().ToString(), currentPage, pageCount);
        
        var backBtn = CreateBackButton(Guid.NewGuid().ToString(), currentPage);

        var pageLabel = CreatePageLabel(Guid.NewGuid().ToString(), currentPage, pageCount);
        
        List<DiscordComponent> components = [backBtn, pageLabel, forwardBtn];
        
        var message = await SendInitialResponseAsync(context, pageEmbeds, components, additionalComponents);
        
        var timedOut = false;
        
        while (!timedOut)
        {
            var response = await message.WaitForButtonAsync();
            
            if (response.TimedOut)
            {
                await SendTimeoutResponseAsync(
                    message,
                    pageEmbeds,
                    currentPage,
                    pageCount,
                    backBtn.CustomId,
                    pageLabel.CustomId,
                    forwardBtn.CustomId,
                    additionalComponents);
                
                timedOut = true;
            }
            else
            {
                if (response.Result.Id == backBtn.CustomId)
                    currentPage--;

                if (response.Result.Id == forwardBtn.CustomId)
                    currentPage++;

                var interactionResponse = new DiscordInteractionResponseBuilder()
                    .AddEmbed(pageEmbeds[currentPage - 1])
                    .AddComponents(
                        CreateBackButton(backBtn.CustomId, currentPage),
                        CreatePageLabel(pageLabel.CustomId, currentPage, pageCount),
                        CreateForwardButton(forwardBtn.CustomId, currentPage, pageCount));
            
                if (additionalComponents != null)
                    interactionResponse.AddComponents(additionalComponents);
                
                await response.Result.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, interactionResponse);
            }
        }
    }

    private static async Task<DiscordMessage> SendInitialResponseAsync(SlashCommandContext context, DiscordEmbed[] embeds, List<DiscordComponent> components, IReadOnlyList<DiscordComponent>? additionalComponents = null)
    {
        var responseBuilder = new DiscordInteractionResponseBuilder()
            .AddEmbed(embeds[0])
            .AddComponents(components);
        
        if (additionalComponents != null)
            responseBuilder.AddComponents(additionalComponents);
        
        await context.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, responseBuilder);
        
        var message = await context.Interaction.GetOriginalResponseAsync();

        return message;
    }
    
    private static async Task SendTimeoutResponseAsync(DiscordMessage message, DiscordEmbed[] embeds, int currentPage, int pageCount, string backBtnId, string pageLabelId, string forwardBtnId, IReadOnlyList<DiscordComponent>? additionalComponents = null)
    {
        var timedOutResponse = new DiscordMessageBuilder()
            .AddEmbed(embeds[currentPage - 1])
            .AddComponents(
                CreateBackButton(backBtnId, 1),
                CreatePageLabel(pageLabelId, currentPage, pageCount),
                CreateForwardButton(forwardBtnId, 1, 1));
                
        if (additionalComponents != null)
            timedOutResponse.AddComponents(additionalComponents);

        await message.ModifyAsync(timedOutResponse);
    }
    
    private static DiscordButtonComponent CreateBackButton(string id, int currentPage)
    {
        var isDisabled = currentPage == 1;
        
        return new DiscordButtonComponent(
            DiscordButtonStyle.Success,
            id,
            "<-", 
            isDisabled);
    }
    
    private static DiscordButtonComponent CreateForwardButton(string id, int currentPage, int pageCount)
    {
        var isDisabled = currentPage == pageCount;
        
        return new DiscordButtonComponent(
            DiscordButtonStyle.Success,
            id,
            "->", 
            isDisabled);
    }
    
    private static DiscordButtonComponent CreatePageLabel(string id, int currentPage, int pageCount)
    {
        return new DiscordButtonComponent(
            DiscordButtonStyle.Secondary,
            id,
            $"{currentPage}/{pageCount}",
            true);
    }
}