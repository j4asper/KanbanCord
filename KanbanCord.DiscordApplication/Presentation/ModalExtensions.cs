using NetCord;

namespace KanbanCord.DiscordApplication.Presentation;

public static class ModalExtensions
{
    public static string GetText(this IReadOnlyList<IModalComponent> components, string customId) =>
        components
            .OfType<Label>()
            .Select(label => label.Component)
            .OfType<TextInput>()
            .First(input => input.CustomId == customId)
            .Value;

    /// <summary>The first selected value of a string select menu in the modal, or null when nothing is selected.</summary>
    public static string? GetSelectedValue(this IReadOnlyList<IModalComponent> components, string customId)
    {
        var menu = components
            .OfType<Label>()
            .Select(label => label.Component)
            .OfType<StringMenu>()
            .FirstOrDefault(candidate => candidate.CustomId == customId);

        return menu is { SelectedValues.Count: > 0 } ? menu.SelectedValues[0] : null;
    }
}
