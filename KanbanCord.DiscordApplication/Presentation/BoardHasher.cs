using System.Security.Cryptography;
using System.Text;
using KanbanCord.Core.Services;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardHasher
{
    public static string Hash(IReadOnlyList<BoardColumn> columns)
    {
        var builder = new StringBuilder();

        foreach (var column in columns)
        {
            builder.Append(column.Status).Append('|');

            foreach (var entry in column.Entries)
                builder.Append(entry.Number).Append(':').Append(entry.Task.Id).Append(':').Append(entry.Task.Title)
                    .Append(':').Append(entry.Task.AssigneeId).Append(':').Append(entry.Task.LastUpdatedAt.Ticks).Append(';');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));

        return Convert.ToHexStringLower(bytes)[..8];
    }
}
