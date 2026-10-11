# KanbanCord NetCord Rewrite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the DSharpPlus bot (`KanbanCord.Bot`) with a NetCord bot (`KanbanCord.DiscordApplication`) in which every command, component handler, gateway handler and autocomplete provider talks to services, never repositories, and every message is Components V2.

**Architecture:** `KanbanCord.Core` gains NetCord-free services (`ITaskService`, `IBoardService`, `ISettingsService`) that wrap the existing repositories and throw `KanbanCordException` for user-facing failures. `KanbanCord.DiscordApplication` holds thin NetCord modules, stateless component/modal handlers (context travels in the `customId`), a Presentation layer that renders models to V2 messages, and central result handlers that turn exceptions into ephemeral V2 errors.

**Tech Stack:** .NET 10, NetCord + NetCord.Hosting.Services `1.0.0-beta.28` (same as BumpBuddy), ASP.NET Core minimal host (health endpoint only), MongoDB.Driver, Serilog, xUnit + Moq.

**Spec:** `docs/superpowers/specs/2026-10-11-netcord-rewrite-design.md`
**Reference implementation of NetCord patterns:** `/home/jasper/Projects/BumpBuddy/DiscordApplications/BumpBuddy.DiscordApp.BumpBuddy` (copy idioms, not code that is BumpBuddy-API specific).

## Global Constraints

- `KanbanCord.Bot` is the behavioural reference and is **not modified** until Task 13 deletes it.
- `KanbanCord.Core` must not reference NetCord.
- Nothing in `KanbanCord.DiscordApplication` may take `ITaskItemRepository` or `ISettingsRepository` as a constructor parameter (enforced by a test in Task 13). Registering them in DI is allowed.
- All bot messages use Components V2 (`MessageFlags.IsComponentsV2`): no `content`, no embeds. Errors and "nothing to do" replies are also `MessageFlags.Ephemeral`.
- Default accent colour is `new Color(255, 117, 20)` (the old `WithDefaultColor`).
- `TreatWarningsAsErrors` is on (`Directory.Build.props`): every task must end with `dotnet build` clean.
- No data migration; Mongo models and collection names are unchanged. Limits come from `KanbanCord.Core.Constants.Limits` (title 40, description 600, comment 10–600).
- Command names, `/task` subcommand names and descriptions are unchanged (copy the description strings from the old files).
- Code style follows the existing KanbanCord code: explicit constructors, `_camelCase` private fields, file-scoped namespaces, `// Arrange / Act / Assert` in tests, test names `Method_ShouldX_WhenY`.
- Test conventions (inspired by BumpBuddy's `Tests/`): shared test data comes from `KanbanCord.Tests/Factories/` (`TaskItemFactory`, `SettingsFactory` with `Create()` / `CreateMany()`), service tests use the in-memory repository fake or Moq, repository tests run against a real Mongo through Mongo2Go (as the existing ones do). Keep the project's existing `Method_ShouldX_WhenY` naming and Moq; do not introduce NSubstitute or a second naming scheme.
- Organisation AI policy (spec §9): commits co-authored by Claude; the final PR gets an **AI-assisted** label; Snyk scan and peer review happen before merge (Task 13 lists them); comment non-trivial AI-solved sections with "AI-assisted (Claude)".
- Commit trailer for every commit: `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- Working directory for all commands: `/home/jasper/Projects/KanbanCord`. Use absolute paths or `git -C`/`dotnet … <path>`; do not `cd` inside parallel tool calls.

## Deviations from the spec (apply when executing; Task 4 and Task 13 update the spec text)

1. `ITaskService` also owns `ClearBoardAsync` / `ResetBoardAsync`; `ISettingsService` only has `RemoveGuildDataAsync` (it composes `ITaskService` + `ISettingsRepository`). Nothing else touches settings today.
2. Autocomplete providers call `IBoardService.GetColumnsAsync` (which owns the `n - Title` numbering) instead of a separate `ITaskService.ListByStatusAsync`.
3. Task services take the task id as the **string** that crosses the Discord boundary and parse it themselves (one place, one "not found" error).
4. `/task view`, `/task me`, `/task user` are open to everyone (old code explicitly overrides the group's Manage Messages requirement); all other `/task` subcommands, `/clear` and `/reset` require Manage Messages. The old "Add Comment" button on `/task view` was open to everyone and stays so.
5. `IMongoDatabase` is registered as a singleton from one `MongoClient` (the old bot built a new client per scope).
6. The old `/board` refresh button was commented out in the command (the handler was dead code). The spec lists a refresh flow, so it is enabled here.
7. `/help` is a single V2 message (19 short entries fit), not paginated. Bot-avatar thumbnails are dropped from `/stats`.
8. Config: `Discord:SupportInvite` becomes `KanbanCord:SupportInvite` (the WIP already deleted `DiscordOptions`). `Discord:Token` is unchanged (NetCord.Hosting reads the same section). README/compose are updated in Task 13.

## Review Focus

Failure modes the spec implies but a straightforward implementation would miss; each has a test or explicit manual check in the named task.

1. **Task id from another guild** (tampered autocomplete value / `customId`) must read as "not found", never leak or mutate another guild's task. → Task 3 (`Mutations_ShouldRejectTaskFromOtherGuild`, `GetAsync_ShouldThrow_WhenTaskBelongsToOtherGuild`).
2. **Malformed task id** (`""`, `"abc"`, wrong length) must produce the normal "task not found" reply, not an unhandled `FormatException`. → Task 3.
3. **Board longer than a V2 message allows** (4000 chars of text): columns must truncate with "…and N more" instead of failing the interaction. → Task 6 (`BoardFormatter` tests).
4. **Blank / over-length / too-short input** reaching the service (modal limits can be bypassed by a crafted interaction): title/description blank or too long, comment under 10 chars. → Task 3.
5. **Foreign or stale buttons and unreachable users**: another user clicking `reset-confirm`/`task-delete-confirm`, a delete-confirm after the task was already deleted, an assignee with DMs closed (must ping in channel instead), an author whose account no longer resolves ("Unknown User"). → Tasks 7, 11, 12 (precondition + manual checklist) and Task 6 (`Unknown User` formatter test).

---

## File Structure

```
KanbanCord.Core/
  Exceptions/KanbanCordException.cs                       (create, Task 2)
  Services/ITaskService.cs, TaskService.cs                (create, Tasks 2-3)
  Services/TaskResults.cs                                 (create, Task 3)
  Services/IBoardService.cs, BoardService.cs, BoardModels.cs   (create, Task 4)
  Services/ISettingsService.cs, SettingsService.cs        (create, Task 5)
KanbanCord.DiscordApplication/
  KanbanCord.DiscordApplication.csproj                    (modify, Task 1)
  Program.cs                                              (modify, Task 1)
  appsettings.json                                        (create, Task 1)
  Extensions/ServiceCollectionExtensions.cs               (modify, Tasks 1, 5, 6, 8)
  Extensions/HostBuilderExtensions.cs                     (modify, Task 1)
  HealthChecks/DiscordConnectivityHealthCheck.cs          (modify, Task 1)
  Presentation/Colors.cs, MessageFactory.cs               (create, Task 6)
  Presentation/BoardFormatter.cs, BoardHasher.cs, BoardView.cs, BoardScope.cs   (create, Task 6)
  Presentation/TaskView.cs, TaskModals.cs, ModalExtensions.cs    (create, Tasks 10-11)
  Presentation/ErrorMessageFactory.cs                     (create, Task 7)
  Services/IUserNameResolver.cs, UserNameResolver.cs      (create, Task 6)
  Services/ICommandMentionService.cs, CommandMentionService.cs (create, Task 6)
  Attributes/RequireInteractionOwnerAttribute.cs          (create, Task 7)
  Commands/ApplicationCommandResultHandler.cs             (create, Task 7)
  Components/InteractionIds.cs                            (create, Task 7)
  Components/ComponentInteractionResultHandler.cs         (create, Task 7)
  Events/GuildCreatedEventHandler.cs, GuildDeletedEventHandler.cs (create, Task 8)
  Commands/BoardCommand.cs, ArchiveCommand.cs, StatsCommand.cs, RepositoryCommand.cs, HelpCommand.cs (Task 9)
  Components/BoardHandlers.cs                             (create, Task 9)
  Providers/*.cs, Commands/ColumnChoice.cs, PriorityChoice.cs (Task 10)
  Commands/Task/TaskCommandGroup.cs + one partial per subcommand (Tasks 10-11)
  Components/TaskModalHandlers.cs, TaskButtonHandlers.cs  (Task 11)
  Commands/ClearCommand.cs, ResetCommand.cs, Components/BoardConfirmationHandlers.cs (Task 12)
KanbanCord.Tests/
  Factories/TaskItemFactory.cs, SettingsFactory.cs              (create, Task 2)
  Fakes/InMemoryTaskItemRepository.cs, FixedTimeProvider.cs     (create, Task 2)
  ServiceTests/TaskServiceTests.cs, BoardServiceTests.cs, SettingsServiceTests.cs   (Tasks 2-5)
  RepositoryTests/TaskItemRepositoryTests.cs, SettingsRepositoryTests.cs            (extend, Task 5a)
  ServiceTests/TaskServiceMongoTests.cs                                             (create, Task 5a)
  PresentationTests/BoardFormatterTests.cs, BoardHasherTests.cs                     (Task 6)
  ArchitectureTests/NoRepositoryDependencyTests.cs                                  (Task 13)
```

---

### Task 1: Fix the host scaffold (builds and serves `/health`)

**Files:**
- Modify: `KanbanCord.DiscordApplication/KanbanCord.DiscordApplication.csproj`
- Modify: `KanbanCord.DiscordApplication/Program.cs`
- Create: `KanbanCord.DiscordApplication/appsettings.json`
- Modify: `KanbanCord.DiscordApplication/Extensions/HostBuilderExtensions.cs`
- Modify: `KanbanCord.DiscordApplication/Extensions/ServiceCollectionExtensions.cs`
- Modify: `KanbanCord.DiscordApplication/HealthChecks/DiscordConnectivityHealthCheck.cs`
- Modify: `Directory.Packages.props` (NetCord version only)

**Interfaces:**
- Produces: `AddHostDependencies()` (still the single DI entry point), `HostBuilderExtensions.UseSerilog()`, `IMongoDatabase` singleton. Later tasks add registrations inside the private `AddServices`/`AddDiscordConfiguration` methods shown here.

- [ ] **Step 1: Inspect the staged scaffold you are about to commit**

Run: `git status --short` and `git diff --cached --stat`
Expected: the staged WIP listed in the spec §2 (DiscordApplication files, `KanbanCord.Core/Options/KanbanCordOptions.cs`, deleted `DiscordOptions.cs`, `Directory.*.props`, `Dockerfile`, `KanbanCord.sln`). This task's commit intentionally includes them. If anything unrelated is staged, unstage it.

- [ ] **Step 2: Bump NetCord to the BumpBuddy version**

In `Directory.Packages.props` change:
```xml
<PackageVersion Include="NetCord.Hosting.Services" Version="1.0.0-beta.28" />
```
Leave the DSharpPlus lines (the old bot still compiles against them until Task 13). Remove the `Microsoft.AspNetCore.OpenApi` line.

- [ ] **Step 3: Replace the csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <!-- Properties inherited from Directory.Build.props -->
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="NetCord.Hosting.Services" />
        <PackageReference Include="Serilog" />
        <PackageReference Include="Serilog.Exceptions" />
        <PackageReference Include="Serilog.Extensions.Logging" />
        <PackageReference Include="Serilog.Settings.Configuration" />
        <PackageReference Include="Serilog.Sinks.Console" />
        <PackageReference Include="Serilog.Sinks.File" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\KanbanCord.Core\KanbanCord.Core.csproj" />
    </ItemGroup>

    <ItemGroup>
        <None Update="appsettings.json" CopyToOutputDirectory="Always" />
    </ItemGroup>

</Project>
```

- [ ] **Step 4: Create `appsettings.json`** (copy of the old bot's file; the same Serilog block and `Urls` so the Dockerfile healthcheck on :5000 keeps working)

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Exceptions"],
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": { "path": "/logs/kanbancord-log.txt", "fileSizeLimitBytes": 5000000, "rollOnFileSizeLimit": true }
      }
    ],
    "Enrich": ["WithExceptionDetails"]
  },
  "Database": { "Name": "KanbanCord" },
  "Urls": "http://localhost:5000"
}
```

- [ ] **Step 5: Replace `Program.cs`**

```csharp
using KanbanCord.DiscordApplication.Extensions;
using NetCord.Hosting.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host
    .UseDefaultServiceProvider()
    .UseSerilog();

builder.Services.AddHostDependencies();

var app = builder.Build();

// NetCord registers every ApplicationCommandModule / ComponentInteractionModule in this assembly
app.AddModules(typeof(Program).Assembly);

app.MapHealthChecks("/health");

await app.RunAsync();
```

- [ ] **Step 6: Replace `HostBuilderExtensions.cs`** (adds the old bot's `UseSerilog`)

```csharp
using Serilog;

namespace KanbanCord.DiscordApplication.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureLogging((hostContext, logging) =>
        {
            logging.ClearProviders();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(hostContext.Configuration)
                .CreateLogger();

            logging.AddSerilog();
        });

        return hostBuilder;
    }

    public static IHostBuilder UseDefaultServiceProvider(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        return hostBuilder;
    }
}
```

- [ ] **Step 7: Replace `ServiceCollectionExtensions.cs`**

```csharp
using KanbanCord.Core.Options;
using KanbanCord.Core.Repositories;
using KanbanCord.DiscordApplication.BackgroundServices;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostDependencies(this IServiceCollection services)
    {
        services
            .AddOptions()
            .AddMongoDatabase()
            .AddRepositories()
            .AddServices()
            .AddBackgroundServices()
            .AddDiscordConfiguration()
            .AddApplicationHealthChecks()
            ;

        services
            .AddHttpClient()
            .AddMemoryCache()
            .AddSingleton(TimeProvider.System)
            ;

        return services;
    }

    private static IServiceCollection AddOptions(this IServiceCollection services)
    {
        services
            .AddOptionsWithValidateOnStart<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.Database)
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<UptimeMonitorOptions>()
            .BindConfiguration(UptimeMonitorOptions.UptimeMonitor)
            .ValidateDataAnnotations();

        services.AddOptionsWithValidateOnStart<KanbanCordOptions>()
            .BindConfiguration(KanbanCordOptions.KanbanCord)
            .ValidateDataAnnotations();

        return services;
    }

    private static IServiceCollection AddMongoDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IMongoClient>()
                .GetDatabase(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.Name));

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services
            .AddScoped<ITaskItemRepository, TaskItemRepository>()
            .AddScoped<ISettingsRepository, SettingsRepository>()
            ;

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Task 5 registers ITaskService / IBoardService / ISettingsService here
        // Task 6 registers IUserNameResolver / ICommandMentionService here
        return services;
    }

    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        services
            .AddHostedService<DatabaseSetupBackgroundService>()
            .AddHostedService<UptimeMonitorBackgroundService>()
            ;

        return services;
    }

    private static IServiceCollection AddDiscordConfiguration(this IServiceCollection services)
    {
        services
            .AddDiscordShardedGateway(options =>
            {
                options.Intents = GatewayIntents.Guilds;

                options.Presence = new PresenceProperties(UserStatusType.Online)
                    .WithActivities([new UserActivityProperties("Watching out for work", UserActivityType.Watching)]);
            })
            .AddApplicationCommands()
            .AddShardedGatewayHandlers(typeof(Program).Assembly)
            ;

        return services;
    }

    private static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddKanbanCordHealthChecks()
            ;

        return services;
    }
}
```

- [ ] **Step 8: Rewrite `DiscordConnectivityHealthCheck.cs` for NetCord**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.HealthChecks;

public class DiscordConnectivityHealthCheck : IHealthCheck
{
    private readonly RestClient _restClient;
    private readonly ILogger<DiscordConnectivityHealthCheck> _logger;

    public DiscordConnectivityHealthCheck(RestClient restClient, ILogger<DiscordConnectivityHealthCheck> logger)
    {
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _restClient.GetCurrentApplicationAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Discord health check failed.");
            return HealthCheckResult.Unhealthy("Discord health check failed.", ex);
        }
    }
}
```
If `GetCurrentApplicationAsync` has a different parameter shape in beta.28 (`RestRequestProperties? properties` instead of `cancellationToken`), use `new RestRequestProperties { CancellationToken = cancellationToken }`; the compiler will tell you.

- [ ] **Step 9: Build the whole solution**

Run: `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln`
Expected: 0 warnings, 0 errors (the old `KanbanCord.Bot` still builds against DSharpPlus). Fix any NetCord API-shape compile errors by reading the BumpBuddy equivalents; do not add `#pragma`/`NoWarn`.

- [ ] **Step 10: Smoke-run the host**

Run (needs a Mongo and a token; skip the Discord part if unavailable): `Discord__Token=dummy Database__ConnectionString=mongodb://localhost:27017 dotnet run --project KanbanCord.DiscordApplication`
Expected: process starts, binds `http://localhost:5000`; `curl -s -o /dev/null -w "%{http_code}" localhost:5000/health` returns 503 or 200 (503 with a dummy token is fine — the point is that the endpoint exists and the DI graph validates on build). Stop the process.

- [ ] **Step 11: Commit**

```bash
git add Directory.Packages.props Directory.Build.props Dockerfile KanbanCord.sln KanbanCord.Core KanbanCord.DiscordApplication KanbanCord.Bot/KanbanCord.Bot.csproj
git status --short   # confirm only intended files; KanbanCord.DscordApplication / OldBot stub deletions may show here — that is expected
git commit -m "Scaffold NetCord host: trim web host, fix DI, port health check" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Service foundation — exception, test fakes, task read/add/edit/delete

**Files:**
- Create: `KanbanCord.Core/Exceptions/KanbanCordException.cs`
- Create: `KanbanCord.Core/Services/ITaskService.cs`, `KanbanCord.Core/Services/TaskService.cs`
- Create: `KanbanCord.Tests/Fakes/InMemoryTaskItemRepository.cs`, `KanbanCord.Tests/Fakes/FixedTimeProvider.cs`
- Create: `KanbanCord.Tests/Factories/TaskItemFactory.cs`, `KanbanCord.Tests/Factories/SettingsFactory.cs`
- Test: `KanbanCord.Tests/ServiceTests/TaskServiceTests.cs`

**Interfaces:**
- Produces:
  - `KanbanCord.Core.Exceptions.KanbanCordException(string message)`
  - `ITaskService.GetAsync(ulong guildId, string taskId) : Task<TaskItem>` — throws `KanbanCordException("The selected task was not found, please try again.")` when the id is malformed, missing, or belongs to another guild.
  - `ITaskService.AddAsync(ulong guildId, ulong authorId, string title, string description) : Task<TaskItem>`
  - `ITaskService.EditAsync(ulong guildId, string taskId, string title, string description) : Task<TaskItem>`
  - `ITaskService.DeleteAsync(ulong guildId, string taskId) : Task<TaskItem>` (returns the deleted task for the confirmation text)
  - `TaskService(ITaskItemRepository repository, TimeProvider timeProvider)`
  - Test helpers `InMemoryTaskItemRepository` (public `List<TaskItem> Items`; returns clones so unsaved mutations are not visible) and `FixedTimeProvider(DateTimeOffset now)`.

- [ ] **Step 1: Create the exception**

`KanbanCord.Core/Exceptions/KanbanCordException.cs`
```csharp
namespace KanbanCord.Core.Exceptions;

/// <summary>A failure whose message is safe and intended to be shown to the Discord user.</summary>
public class KanbanCordException : Exception
{
    public KanbanCordException(string message) : base(message)
    {
    }
}
```

- [ ] **Step 2a: Create the test data factories** (BumpBuddy-style `Create()` / `CreateMany()`; every service and repository test uses these instead of building models inline)

`KanbanCord.Tests/Factories/TaskItemFactory.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.Tests.Factories;

public static class TaskItemFactory
{
    public const ulong DefaultGuildId = 123456789012345678; // a realistic Discord snowflake

    public static TaskItem Create(
        ulong guildId = DefaultGuildId,
        string title = "Test Task",
        BoardStatus status = BoardStatus.Backlog,
        ulong authorId = 987654321098765432,
        ulong? assigneeId = null) =>
        new()
        {
            GuildId = guildId,
            Title = title,
            Description = "Test Task Description",
            AuthorId = authorId,
            AssigneeId = assigneeId,
            Status = status,
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LastUpdatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

    /// <summary>One task per column for the default guild, plus one task in another guild.</summary>
    public static IReadOnlyList<TaskItem> CreateMany() =>
    [
        Create(title: "Backlog Task", status: BoardStatus.Backlog),
        Create(title: "In Progress Task", status: BoardStatus.InProgress),
        Create(title: "Completed Task", status: BoardStatus.Completed),
        Create(title: "Archived Task", status: BoardStatus.Archived),
        Create(guildId: 1, title: "Other Guild Task"),
    ];
}
```

`KanbanCord.Tests/Factories/SettingsFactory.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.Tests.Factories;

public static class SettingsFactory
{
    public static Settings Create(ulong guildId = TaskItemFactory.DefaultGuildId, TimeSpan? archiveAfter = null) =>
        new() { GuildId = guildId, ArchiveAfter = archiveAfter };

    public static IReadOnlyList<Settings> CreateMany() =>
    [
        Create(1, TimeSpan.FromDays(7)),
        Create(2),
        Create(3, TimeSpan.FromDays(30)),
    ];
}
```

- [ ] **Step 2: Create the test fakes**

`KanbanCord.Tests/Fakes/FixedTimeProvider.cs`
```csharp
namespace KanbanCord.Tests.Fakes;

public class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public void Advance(TimeSpan by) => _now += by;

    public override DateTimeOffset GetUtcNow() => _now;
}
```

`KanbanCord.Tests/Fakes/InMemoryTaskItemRepository.cs`
```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using MongoDB.Bson;

namespace KanbanCord.Tests.Fakes;

public class InMemoryTaskItemRepository : ITaskItemRepository
{
    public List<TaskItem> Items { get; } = [];

    public Task<IReadOnlyList<TaskItem>> GetAllTaskItemsByGuildIdAsync(ulong guildId) =>
        Task.FromResult<IReadOnlyList<TaskItem>>(Items.Where(x => x.GuildId == guildId).Select(Clone).ToList());

    public Task<TaskItem?> GetTaskItemByObjectIdOrDefaultAsync(ObjectId objectId)
    {
        var item = Items.FirstOrDefault(x => x.Id == objectId);

        return Task.FromResult(item is null ? null : Clone(item));
    }

    public Task AddTaskItemAsync(TaskItem task)
    {
        // The Mongo driver assigns an id on insert when it is empty; mimic that
        if (task.Id == ObjectId.Empty)
            task.Id = ObjectId.GenerateNewId();

        Items.Add(Clone(task));

        return Task.CompletedTask;
    }

    public Task UpdateTaskItemAsync(TaskItem task)
    {
        var index = Items.FindIndex(x => x.Id == task.Id);

        if (index >= 0)
            Items[index] = Clone(task);

        return Task.CompletedTask;
    }

    public Task RemoveTaskItemAsync(TaskItem task)
    {
        Items.RemoveAll(x => x.Id == task.Id);

        return Task.CompletedTask;
    }

    public Task RemoveAllTaskItemsByIdAsync(ulong guildId)
    {
        Items.RemoveAll(x => x.GuildId == guildId);

        return Task.CompletedTask;
    }

    private static TaskItem Clone(TaskItem source) => new()
    {
        Id = source.Id,
        GuildId = source.GuildId,
        Title = source.Title,
        Description = source.Description,
        CreatedAt = source.CreatedAt,
        LastUpdatedAt = source.LastUpdatedAt,
        AuthorId = source.AuthorId,
        AssigneeId = source.AssigneeId,
        Comments = source.Comments.Select(c => new Comment { AuthorId = c.AuthorId, Text = c.Text, CreatedAt = c.CreatedAt }).ToList(),
        Status = source.Status,
        Priority = source.Priority,
    };
}
```

- [ ] **Step 3: Write the failing tests**

`KanbanCord.Tests/ServiceTests/TaskServiceTests.cs`
```csharp
using KanbanCord.Core.Constants;
using KanbanCord.Core.Exceptions;
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.Tests.Factories;
using KanbanCord.Tests.Fakes;

namespace KanbanCord.Tests.ServiceTests;

public class TaskServiceTests
{
    private const ulong GuildId = 1;
    private const ulong OtherGuildId = 2;
    private const string NotFound = "The selected task was not found, please try again.";

    private readonly InMemoryTaskItemRepository _repository = new();
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(_repository, _time);
    }

    private async Task<TaskItem> SeedAsync(ulong guildId = GuildId, BoardStatus status = BoardStatus.Backlog, string title = "Task")
    {
        var task = TaskItemFactory.Create(guildId, title, status);

        await _repository.AddTaskItemAsync(task);

        return task;
    }

    [Fact]
    public async Task GetAsync_ShouldReturnTask_WhenTaskBelongsToGuild()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        var result = await _service.GetAsync(GuildId, seeded.Id.ToString());

        // Assert
        Assert.Equal(seeded.Id, result.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldThrow_WhenTaskBelongsToOtherGuild()
    {
        // Arrange
        var seeded = await SeedAsync(OtherGuildId);

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, seeded.Id.ToString()));

        // Assert
        Assert.Equal(NotFound, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-id")]
    [InlineData("123")]
    [InlineData("zzzzzzzzzzzzzzzzzzzzzzzz")]
    public async Task GetAsync_ShouldThrowNotFound_WhenIdIsMalformed(string taskId)
    {
        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, taskId));

        // Assert
        Assert.Equal(NotFound, exception.Message);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, "507f1f77bcf86cd799439011"));

        // Assert
        Assert.Equal(NotFound, exception.Message);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistTrimmedTaskInBacklog()
    {
        // Act
        var result = await _service.AddAsync(GuildId, 42, "  Fix bug  ", " Details ");

        // Assert
        var stored = Assert.Single(_repository.Items);
        Assert.Equal(result.Id, stored.Id);
        Assert.Equal("Fix bug", stored.Title);
        Assert.Equal("Details", stored.Description);
        Assert.Equal(GuildId, stored.GuildId);
        Assert.Equal(42UL, stored.AuthorId);
        Assert.Equal(BoardStatus.Backlog, stored.Status);
    }

    [Theory]
    [InlineData("", "desc")]
    [InlineData("   ", "desc")]
    [InlineData("title", "")]
    [InlineData("title", "   ")]
    public async Task AddAsync_ShouldThrow_WhenTitleOrDescriptionIsBlank(string title, string description)
    {
        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddAsync(GuildId, 1, title, description));
        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenTitleOrDescriptionIsTooLong()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddAsync(GuildId, 1, new string('a', Limits.TaskTitleMaxLength + 1), "desc"));
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddAsync(GuildId, 1, "title", new string('a', Limits.TaskDescriptionMaxLength + 1)));
        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task EditAsync_ShouldUpdateTitleDescriptionAndLastUpdatedAt()
    {
        // Arrange
        var seeded = await SeedAsync();
        _time.Advance(TimeSpan.FromHours(1));

        // Act
        await _service.EditAsync(GuildId, seeded.Id.ToString(), "New title", "New description");

        // Assert
        var stored = Assert.Single(_repository.Items);
        Assert.Equal("New title", stored.Title);
        Assert.Equal("New description", stored.Description);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, stored.LastUpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTaskAndReturnIt()
    {
        // Arrange
        var seeded = await SeedAsync(title: "Doomed");

        // Act
        var deleted = await _service.DeleteAsync(GuildId, seeded.Id.ToString());

        // Assert
        Assert.Equal("Doomed", deleted.Title);
        Assert.Empty(_repository.Items);
    }
}
```

- [ ] **Step 4: Run to confirm failure**

Run: `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~TaskServiceTests"`
Expected: build FAIL — `TaskService`/`KanbanCord.Core.Services` do not exist.

- [ ] **Step 5: Implement `ITaskService` (this task's slice) and `TaskService`**

`KanbanCord.Core/Services/ITaskService.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public interface ITaskService
{
    Task<TaskItem> GetAsync(ulong guildId, string taskId);

    Task<TaskItem> AddAsync(ulong guildId, ulong authorId, string title, string description);

    Task<TaskItem> EditAsync(ulong guildId, string taskId, string title, string description);

    Task<TaskItem> DeleteAsync(ulong guildId, string taskId);
}
```

`KanbanCord.Core/Services/TaskService.cs`
```csharp
using KanbanCord.Core.Constants;
using KanbanCord.Core.Exceptions;
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using MongoDB.Bson;

namespace KanbanCord.Core.Services;

public class TaskService : ITaskService
{
    private const string TaskNotFoundMessage = "The selected task was not found, please try again.";

    private readonly ITaskItemRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TaskService(ITaskItemRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<TaskItem> GetAsync(ulong guildId, string taskId)
    {
        if (!ObjectId.TryParse(taskId, out var objectId))
            throw new KanbanCordException(TaskNotFoundMessage);

        var task = await _repository.GetTaskItemByObjectIdOrDefaultAsync(objectId);

        // A task from another guild is indistinguishable from a missing one
        if (task is null || task.GuildId != guildId)
            throw new KanbanCordException(TaskNotFoundMessage);

        return task;
    }

    public async Task<TaskItem> AddAsync(ulong guildId, ulong authorId, string title, string description)
    {
        var task = new TaskItem
        {
            GuildId = guildId,
            AuthorId = authorId,
            Title = RequireText(title, Limits.TaskTitleMaxLength, "title"),
            Description = RequireText(description, Limits.TaskDescriptionMaxLength, "description"),
        };

        await _repository.AddTaskItemAsync(task);

        return task;
    }

    public async Task<TaskItem> EditAsync(ulong guildId, string taskId, string title, string description)
    {
        var task = await GetAsync(guildId, taskId);

        task.Title = RequireText(title, Limits.TaskTitleMaxLength, "title");
        task.Description = RequireText(description, Limits.TaskDescriptionMaxLength, "description");

        await SaveAsync(task);

        return task;
    }

    public async Task<TaskItem> DeleteAsync(ulong guildId, string taskId)
    {
        var task = await GetAsync(guildId, taskId);

        await _repository.RemoveTaskItemAsync(task);

        return task;
    }

    private async Task SaveAsync(TaskItem task)
    {
        task.LastUpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _repository.UpdateTaskItemAsync(task);
    }

    private static string RequireText(string? value, int maxLength, string fieldName, int minLength = 1)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
            throw new KanbanCordException($"The {fieldName} cannot be empty.");

        if (trimmed.Length < minLength)
            throw new KanbanCordException($"The {fieldName} must be at least {minLength} characters long.");

        if (trimmed.Length > maxLength)
            throw new KanbanCordException($"The {fieldName} cannot be longer than {maxLength} characters.");

        return trimmed;
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~TaskServiceTests"`
Expected: PASS (all). If `ObjectId.TryParse("")` or a null throws in this driver version, wrap in the service: `if (string.IsNullOrEmpty(taskId) || !ObjectId.TryParse(...))`.

- [ ] **Step 7: Commit**

```bash
git add KanbanCord.Core/Exceptions KanbanCord.Core/Services KanbanCord.Tests/Fakes KanbanCord.Tests/Factories KanbanCord.Tests/ServiceTests/TaskServiceTests.cs
git commit -m "Add KanbanCordException and TaskService read/add/edit/delete" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Task service — transitions, priority, assignment, comments, clear/reset

**Files:**
- Create: `KanbanCord.Core/Services/TaskResults.cs`
- Modify: `KanbanCord.Core/Services/ITaskService.cs`, `KanbanCord.Core/Services/TaskService.cs`
- Test: `KanbanCord.Tests/ServiceTests/TaskServiceTests.cs` (append)

**Interfaces:**
- Consumes: everything from Task 2.
- Produces (exact signatures later tasks call):
```csharp
public sealed record StatusChange(TaskItem Task, BoardStatus From);
public sealed record PriorityChange(TaskItem Task, Priority? From);
public sealed record AssignmentChange(TaskItem Task, ulong? PreviousAssigneeId);

Task<StatusChange>     MoveAsync(ulong guildId, string taskId, BoardStatus to);      // throws if already in column
Task<StatusChange>     StartAsync(ulong guildId, string taskId);                     // -> InProgress
Task<StatusChange>     CompleteAsync(ulong guildId, string taskId);                  // -> Completed
Task<StatusChange>     ArchiveAsync(ulong guildId, string taskId);                   // -> Archived
Task<PriorityChange>   SetPriorityAsync(ulong guildId, string taskId, Priority priority);   // throws if same
Task<AssignmentChange> AssignAsync(ulong guildId, string taskId, ulong? assigneeId); // null = unassign; throws if nobody assigned
Task<TaskItem>         AddCommentAsync(ulong guildId, string taskId, ulong authorId, string text);
Task<int>              ClearBoardAsync(ulong guildId);   // archives every non-archived task, returns count
Task                   ResetBoardAsync(ulong guildId);   // deletes every task of the guild
```
- Before Step 1 read `KanbanCord.Bot/Commands/Task/TaskCompleteCommand.cs` and copy any status-specific behaviour/message wording into the notes for Task 11 (it mirrors `TaskStartCommand`).

- [ ] **Step 1: Append failing tests** to `TaskServiceTests` (inside the class)

```csharp
    [Fact]
    public async Task MoveAsync_ShouldChangeStatusAndReturnPreviousColumn()
    {
        // Arrange
        var seeded = await SeedAsync(status: BoardStatus.Backlog);

        // Act
        var change = await _service.MoveAsync(GuildId, seeded.Id.ToString(), BoardStatus.Completed);

        // Assert
        Assert.Equal(BoardStatus.Backlog, change.From);
        Assert.Equal(BoardStatus.Completed, Assert.Single(_repository.Items).Status);
    }

    [Fact]
    public async Task MoveAsync_ShouldThrow_WhenTaskAlreadyInColumn()
    {
        // Arrange
        var seeded = await SeedAsync(status: BoardStatus.InProgress);

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.MoveAsync(GuildId, seeded.Id.ToString(), BoardStatus.InProgress));

        // Assert
        Assert.Equal("The selected task is already in column **In Progress**.", exception.Message);
    }

    [Fact]
    public async Task StartCompleteArchive_ShouldSetTargetStatuses()
    {
        // Arrange
        var seeded = await SeedAsync();
        var id = seeded.Id.ToString();

        // Act & Assert
        Assert.Equal(BoardStatus.Backlog, (await _service.StartAsync(GuildId, id)).From);
        Assert.Equal(BoardStatus.InProgress, Assert.Single(_repository.Items).Status);

        Assert.Equal(BoardStatus.InProgress, (await _service.CompleteAsync(GuildId, id)).From);
        Assert.Equal(BoardStatus.Completed, Assert.Single(_repository.Items).Status);

        Assert.Equal(BoardStatus.Completed, (await _service.ArchiveAsync(GuildId, id)).From);
        Assert.Equal(BoardStatus.Archived, Assert.Single(_repository.Items).Status);
    }

    [Fact]
    public async Task SetPriorityAsync_ShouldUpdateAndReturnPreviousPriority()
    {
        // Arrange
        var seeded = await SeedAsync(); // default priority is Medium

        // Act
        var change = await _service.SetPriorityAsync(GuildId, seeded.Id.ToString(), Priority.High);

        // Assert
        Assert.Equal(Priority.Medium, change.From);
        Assert.Equal(Priority.High, Assert.Single(_repository.Items).Priority);
    }

    [Fact]
    public async Task SetPriorityAsync_ShouldThrow_WhenPriorityUnchanged()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.SetPriorityAsync(GuildId, seeded.Id.ToString(), Priority.Medium));

        // Assert
        Assert.Equal("The selected task already has the priority **Medium**.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldAssignThenUnassign()
    {
        // Arrange
        var seeded = await SeedAsync();
        var id = seeded.Id.ToString();

        // Act
        var assigned = await _service.AssignAsync(GuildId, id, 99);
        var unassigned = await _service.AssignAsync(GuildId, id, null);

        // Assert
        Assert.Null(assigned.PreviousAssigneeId);
        Assert.Equal(99UL, unassigned.PreviousAssigneeId);
        Assert.Null(Assert.Single(_repository.Items).AssigneeId);
    }

    [Fact]
    public async Task AssignAsync_ShouldThrow_WhenUnassigningTaskWithoutAssignee()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => _service.AssignAsync(GuildId, seeded.Id.ToString(), null));

        // Assert
        Assert.Equal("The selected task has no user assigned.", exception.Message);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldAppendCommentWithAuthorAndTimestamp()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act
        await _service.AddCommentAsync(GuildId, seeded.Id.ToString(), 7, "This is a valid comment.");

        // Assert
        var comment = Assert.Single(Assert.Single(_repository.Items).Comments);
        Assert.Equal(7UL, comment.AuthorId);
        Assert.Equal("This is a valid comment.", comment.Text);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, comment.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("too short")] // 9 characters, minimum is 10
    public async Task AddCommentAsync_ShouldThrow_WhenCommentBelowMinimumOrBlank(string text)
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddCommentAsync(GuildId, seeded.Id.ToString(), 7, text));
        Assert.Empty(Assert.Single(_repository.Items).Comments);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldThrow_WhenCommentTooLong()
    {
        // Arrange
        var seeded = await SeedAsync();

        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.AddCommentAsync(GuildId, seeded.Id.ToString(), 7, new string('a', Limits.TaskCommentMaxLength + 1)));
    }

    [Fact]
    public async Task ClearBoardAsync_ShouldArchiveEveryNonArchivedTaskOfTheGuildOnly()
    {
        // Arrange
        await SeedAsync(status: BoardStatus.Backlog);
        await SeedAsync(status: BoardStatus.Completed);
        await SeedAsync(status: BoardStatus.Archived);
        var foreign = await SeedAsync(OtherGuildId, BoardStatus.Backlog);

        // Act
        var archived = await _service.ClearBoardAsync(GuildId);

        // Assert
        Assert.Equal(2, archived);
        Assert.All(_repository.Items.Where(x => x.GuildId == GuildId), x => Assert.Equal(BoardStatus.Archived, x.Status));
        Assert.Equal(BoardStatus.Backlog, _repository.Items.Single(x => x.Id == foreign.Id).Status);
    }

    [Fact]
    public async Task ResetBoardAsync_ShouldDeleteOnlyTheGuildsTasks()
    {
        // Arrange
        await SeedAsync();
        await SeedAsync(status: BoardStatus.Archived);
        await SeedAsync(OtherGuildId);

        // Act
        await _service.ResetBoardAsync(GuildId);

        // Assert
        Assert.Equal(OtherGuildId, Assert.Single(_repository.Items).GuildId);
    }

    public static IEnumerable<object[]> Mutations() =>
    [
        [(Func<ITaskService, string, Task>)((s, id) => s.EditAsync(GuildId, id, "t", "d"))],
        [(Func<ITaskService, string, Task>)((s, id) => s.DeleteAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.MoveAsync(GuildId, id, BoardStatus.Completed))],
        [(Func<ITaskService, string, Task>)((s, id) => s.StartAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.CompleteAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.ArchiveAsync(GuildId, id))],
        [(Func<ITaskService, string, Task>)((s, id) => s.SetPriorityAsync(GuildId, id, Priority.High))],
        [(Func<ITaskService, string, Task>)((s, id) => s.AssignAsync(GuildId, id, 5))],
        [(Func<ITaskService, string, Task>)((s, id) => s.AddCommentAsync(GuildId, id, 5, "A perfectly fine comment"))],
    ];

    [Theory]
    [MemberData(nameof(Mutations))]
    public async Task Mutations_ShouldRejectTaskFromOtherGuild(Func<ITaskService, string, Task> mutation)
    {
        // Arrange
        var foreign = await SeedAsync(OtherGuildId, title: "Foreign");

        // Act
        var exception = await Assert.ThrowsAsync<KanbanCordException>(() => mutation(_service, foreign.Id.ToString()));

        // Assert
        Assert.Equal(NotFound, exception.Message);
        var stored = Assert.Single(_repository.Items);
        Assert.Equal("Foreign", stored.Title);
        Assert.Equal(BoardStatus.Backlog, stored.Status);
        Assert.Empty(stored.Comments);
    }
```

- [ ] **Step 2: Run to confirm failure**

Run: `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~TaskServiceTests"`
Expected: build FAIL (new members missing).

- [ ] **Step 3: Add result records**

`KanbanCord.Core/Services/TaskResults.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public sealed record StatusChange(TaskItem Task, BoardStatus From);

public sealed record PriorityChange(TaskItem Task, Priority? From);

public sealed record AssignmentChange(TaskItem Task, ulong? PreviousAssigneeId);
```

- [ ] **Step 4: Extend `ITaskService`** — add the nine signatures from the Interfaces block above (keep the Task 2 members).

- [ ] **Step 5: Implement in `TaskService`** (add inside the class)

```csharp
    public async Task<StatusChange> MoveAsync(ulong guildId, string taskId, BoardStatus to)
    {
        var task = await GetAsync(guildId, taskId);

        if (task.Status == to)
            throw new KanbanCordException($"The selected task is already in column **{to.ToFormattedString()}**.");

        return await ChangeStatusAsync(task, to);
    }

    public async Task<StatusChange> StartAsync(ulong guildId, string taskId) =>
        await ChangeStatusAsync(await GetAsync(guildId, taskId), BoardStatus.InProgress);

    public async Task<StatusChange> CompleteAsync(ulong guildId, string taskId) =>
        await ChangeStatusAsync(await GetAsync(guildId, taskId), BoardStatus.Completed);

    public async Task<StatusChange> ArchiveAsync(ulong guildId, string taskId) =>
        await ChangeStatusAsync(await GetAsync(guildId, taskId), BoardStatus.Archived);

    public async Task<PriorityChange> SetPriorityAsync(ulong guildId, string taskId, Priority priority)
    {
        var task = await GetAsync(guildId, taskId);

        if (task.Priority == priority)
            throw new KanbanCordException($"The selected task already has the priority **{priority}**.");

        var from = task.Priority;

        task.Priority = priority;

        await SaveAsync(task);

        return new PriorityChange(task, from);
    }

    public async Task<AssignmentChange> AssignAsync(ulong guildId, string taskId, ulong? assigneeId)
    {
        var task = await GetAsync(guildId, taskId);

        if (assigneeId is null && task.AssigneeId is null)
            throw new KanbanCordException("The selected task has no user assigned.");

        var previous = task.AssigneeId;

        task.AssigneeId = assigneeId;

        await SaveAsync(task);

        return new AssignmentChange(task, previous);
    }

    public async Task<TaskItem> AddCommentAsync(ulong guildId, string taskId, ulong authorId, string text)
    {
        var task = await GetAsync(guildId, taskId);

        task.Comments.Add(new Comment
        {
            AuthorId = authorId,
            Text = RequireText(text, Limits.TaskCommentMaxLength, "comment", Limits.TaskCommentMinLength),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        });

        await SaveAsync(task);

        return task;
    }

    public async Task<int> ClearBoardAsync(ulong guildId)
    {
        var tasks = await _repository.GetAllTaskItemsByGuildIdAsync(guildId);

        var archived = 0;

        foreach (var task in tasks.Where(x => x.Status != BoardStatus.Archived))
        {
            task.Status = BoardStatus.Archived;

            await SaveAsync(task);

            archived++;
        }

        return archived;
    }

    public async Task ResetBoardAsync(ulong guildId)
    {
        await _repository.RemoveAllTaskItemsByIdAsync(guildId);
    }

    private async Task<StatusChange> ChangeStatusAsync(TaskItem task, BoardStatus to)
    {
        var from = task.Status;

        task.Status = to;

        await SaveAsync(task);

        return new StatusChange(task, from);
    }
```

- [ ] **Step 6: Run tests**

Run: `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~TaskServiceTests"`
Expected: PASS. (`Priority.Medium` string interpolation yields "Medium"; `BoardStatus.InProgress.ToFormattedString()` yields "In Progress".)

- [ ] **Step 7: Commit**

```bash
git add KanbanCord.Core/Services KanbanCord.Tests/ServiceTests/TaskServiceTests.cs
git commit -m "Add task transitions, priority, assignment, comments and board clear/reset to TaskService" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 4: BoardService (grouping, numbering, filtering)

**Files:**
- Create: `KanbanCord.Core/Services/BoardModels.cs`, `IBoardService.cs`, `BoardService.cs`
- Test: `KanbanCord.Tests/ServiceTests/BoardServiceTests.cs`
- Modify: `docs/superpowers/specs/2026-10-11-netcord-rewrite-design.md` (record the plan's deviations, see Step 6)

**Interfaces:**
- Produces:
```csharp
public sealed record BoardEntry(int Number, TaskItem Task);                       // Number is 1-based within the column, counted over ALL tasks of that status (even when filtered by assignee)
public sealed record BoardColumn(BoardStatus Status, IReadOnlyList<BoardEntry> Entries);
Task<IReadOnlyList<BoardColumn>> IBoardService.GetColumnsAsync(ulong guildId, IReadOnlyCollection<BoardStatus> statuses, ulong? assigneeId = null);   // one column per requested status, in the requested order, empty columns included
```

- [ ] **Step 1: Write failing tests**

`KanbanCord.Tests/ServiceTests/BoardServiceTests.cs`
```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.Tests.Factories;
using KanbanCord.Tests.Fakes;

namespace KanbanCord.Tests.ServiceTests;

public class BoardServiceTests
{
    private const ulong GuildId = 1;

    private readonly InMemoryTaskItemRepository _repository = new();
    private readonly BoardService _service;

    public BoardServiceTests()
    {
        _service = new BoardService(_repository);
    }

    private async Task SeedAsync(string title, BoardStatus status, ulong guildId = GuildId, ulong? assigneeId = null) =>
        await _repository.AddTaskItemAsync(TaskItemFactory.Create(guildId, title, status, assigneeId: assigneeId));

    [Fact]
    public async Task GetColumnsAsync_ShouldReturnOneColumnPerRequestedStatusInOrder()
    {
        // Arrange
        await SeedAsync("a", BoardStatus.Completed);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed]);

        // Assert
        Assert.Equal([BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed], columns.Select(c => c.Status));
        Assert.Empty(columns[0].Entries);
        Assert.Single(columns[2].Entries);
    }

    [Fact]
    public async Task GetColumnsAsync_ShouldNumberEntriesPerColumnStartingAtOne()
    {
        // Arrange
        await SeedAsync("b1", BoardStatus.Backlog);
        await SeedAsync("p1", BoardStatus.InProgress);
        await SeedAsync("b2", BoardStatus.Backlog);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog, BoardStatus.InProgress]);

        // Assert
        Assert.Equal([(1, "b1"), (2, "b2")], columns[0].Entries.Select(e => (e.Number, e.Task.Title)));
        Assert.Equal([(1, "p1")], columns[1].Entries.Select(e => (e.Number, e.Task.Title)));
    }

    [Fact]
    public async Task GetColumnsAsync_ShouldKeepOriginalNumbersWhenFilteringByAssignee()
    {
        // Arrange
        await SeedAsync("unassigned", BoardStatus.Backlog);
        await SeedAsync("mine", BoardStatus.Backlog, assigneeId: 5);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog], assigneeId: 5);

        // Assert
        var entry = Assert.Single(columns[0].Entries);
        Assert.Equal(2, entry.Number); // matches the number shown on the unfiltered board
        Assert.Equal("mine", entry.Task.Title);
    }

    [Fact]
    public async Task GetColumnsAsync_ShouldIgnoreOtherGuildsAndNotIncludeUnrequestedStatuses()
    {
        // Arrange
        await SeedAsync("mine", BoardStatus.Backlog);
        await SeedAsync("foreign", BoardStatus.Backlog, guildId: 2);
        await SeedAsync("old", BoardStatus.Archived);

        // Act
        var columns = await _service.GetColumnsAsync(GuildId, [BoardStatus.Backlog]);

        // Assert
        Assert.Equal("mine", Assert.Single(Assert.Single(columns).Entries).Task.Title);
    }
}
```

- [ ] **Step 2: Run to confirm failure** — `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~BoardServiceTests"` → build FAIL.

- [ ] **Step 3: Implement**

`KanbanCord.Core/Services/BoardModels.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public sealed record BoardEntry(int Number, TaskItem Task);

public sealed record BoardColumn(BoardStatus Status, IReadOnlyList<BoardEntry> Entries);
```

`KanbanCord.Core/Services/IBoardService.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.Core.Services;

public interface IBoardService
{
    Task<IReadOnlyList<BoardColumn>> GetColumnsAsync(ulong guildId, IReadOnlyCollection<BoardStatus> statuses, ulong? assigneeId = null);
}
```

`KanbanCord.Core/Services/BoardService.cs`
```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;

namespace KanbanCord.Core.Services;

public class BoardService : IBoardService
{
    private readonly ITaskItemRepository _repository;

    public BoardService(ITaskItemRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<BoardColumn>> GetColumnsAsync(ulong guildId, IReadOnlyCollection<BoardStatus> statuses, ulong? assigneeId = null)
    {
        var tasks = await _repository.GetAllTaskItemsByGuildIdAsync(guildId);

        return statuses
            .Select(status => new BoardColumn(
                status,
                tasks
                    .Where(task => task.Status == status)
                    // Number before filtering so the numbers match the unfiltered board
                    .Select((task, index) => new BoardEntry(index + 1, task))
                    .Where(entry => assigneeId is null || entry.Task.AssigneeId == assigneeId)
                    .ToList()))
            .ToList();
    }
}
```

- [ ] **Step 4: Run tests** — same filter → PASS.

- [ ] **Step 5: Update the spec's service table** to match the "Deviations" section at the top of this plan (ITaskService owns clear/reset; ISettingsService only `RemoveGuildDataAsync`; providers call `IBoardService`; `/task view|me|user` are open). Edit §3 "Services" table and §5 "Permissions (parity)" in `docs/superpowers/specs/2026-10-11-netcord-rewrite-design.md`.

- [ ] **Step 6: Commit**

```bash
git add KanbanCord.Core/Services KanbanCord.Tests/ServiceTests/BoardServiceTests.cs docs/superpowers/specs
git commit -m "Add BoardService and align spec with plan" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 5: SettingsService + service DI registration

**Files:**
- Create: `KanbanCord.Core/Services/ISettingsService.cs`, `SettingsService.cs`
- Test: `KanbanCord.Tests/ServiceTests/SettingsServiceTests.cs`
- Modify: `KanbanCord.DiscordApplication/Extensions/ServiceCollectionExtensions.cs` (`AddServices`)

**Interfaces:**
- Produces: `Task ISettingsService.RemoveGuildDataAsync(ulong guildId)` — deletes the guild's tasks (via `ITaskService.ResetBoardAsync`) then its settings document. Scoped registrations for `ITaskService`, `IBoardService`, `ISettingsService`.

- [ ] **Step 1: Failing test**

`KanbanCord.Tests/ServiceTests/SettingsServiceTests.cs`
```csharp
using KanbanCord.Core.Repositories;
using KanbanCord.Core.Services;
using Moq;

namespace KanbanCord.Tests.ServiceTests;

public class SettingsServiceTests
{
    [Fact]
    public async Task RemoveGuildDataAsync_ShouldRemoveTasksAndSettingsOfThatGuild()
    {
        // Arrange
        var taskService = new Mock<ITaskService>();
        var settingsRepository = new Mock<ISettingsRepository>();
        var service = new SettingsService(taskService.Object, settingsRepository.Object);

        // Act
        await service.RemoveGuildDataAsync(123);

        // Assert
        taskService.Verify(x => x.ResetBoardAsync(123), Times.Once);
        settingsRepository.Verify(x => x.RemoveAsync(123), Times.Once);
        taskService.VerifyNoOtherCalls();
        settingsRepository.VerifyNoOtherCalls();
    }
}
```

- [ ] **Step 2: Run to confirm failure** → build FAIL.

- [ ] **Step 3: Implement**

`KanbanCord.Core/Services/ISettingsService.cs`
```csharp
namespace KanbanCord.Core.Services;

public interface ISettingsService
{
    Task RemoveGuildDataAsync(ulong guildId);
}
```

`KanbanCord.Core/Services/SettingsService.cs`
```csharp
using KanbanCord.Core.Repositories;

namespace KanbanCord.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly ITaskService _taskService;
    private readonly ISettingsRepository _settingsRepository;

    public SettingsService(ITaskService taskService, ISettingsRepository settingsRepository)
    {
        _taskService = taskService;
        _settingsRepository = settingsRepository;
    }

    public async Task RemoveGuildDataAsync(ulong guildId)
    {
        await _taskService.ResetBoardAsync(guildId);

        await _settingsRepository.RemoveAsync(guildId);
    }
}
```

- [ ] **Step 4: Register services** — in `AddServices()` of `ServiceCollectionExtensions.cs`, replace the placeholder comments' first line with:
```csharp
        services
            .AddScoped<ITaskService, TaskService>()
            .AddScoped<IBoardService, BoardService>()
            .AddScoped<ISettingsService, SettingsService>()
            ;
```
and add `using KanbanCord.Core.Services;`. Keep the "Task 6 registers…" comment.

- [ ] **Step 5: Build + all tests**

Run: `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` then `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj`
Expected: 0 warnings; all tests pass (repository tests need Mongo2Go's embedded binary; if they cannot start here, run only the service tests and note it).

- [ ] **Step 6: Commit**

```bash
git add KanbanCord.Core/Services KanbanCord.Tests/ServiceTests KanbanCord.DiscordApplication/Extensions
git commit -m "Add SettingsService and register services" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 5a: Repository edge-case tests and a service-over-real-Mongo test

The existing repository tests already cover every repository method (`SettingsRepositoryTests`: 5, `TaskItemRepositoryTests`: 7). These additions pin the behaviours the new services depend on, and guard against the in-memory fake used by the service tests drifting from the real driver.

**Files:**
- Modify: `KanbanCord.Tests/RepositoryTests/TaskItemRepositoryTests.cs`, `KanbanCord.Tests/RepositoryTests/SettingsRepositoryTests.cs`
- Create: `KanbanCord.Tests/ServiceTests/TaskServiceMongoTests.cs`
- Modify: `docs/superpowers/specs/2026-10-11-netcord-rewrite-design.md` (§7: mention factories, repository edge cases and the Mongo-backed service test)

**Interfaces:**
- Consumes: `TaskItemFactory`, `SettingsFactory` (Task 2), `TaskService` (Tasks 2–3), `TaskItemRepository(IMongoDatabase)`, `SettingsRepository(IMongoDatabase)`.
- Produces: nothing used later; if `GetAllTaskItemsByGuildIdAsync_ShouldReturnTasksInInsertionOrder` ever fails, `BoardService` (Task 4) must sort explicitly (`.OrderBy(task => task.Id)`) because board numbers and autocomplete labels rely on a stable order.

- [ ] **Step 1: Add task repository tests** — inside `TaskItemRepositoryTests` (next to the existing tests, same Mongo2Go fixture; add `using KanbanCord.Tests.Factories;`)

```csharp
        [Fact]
        public async Task AddTaskItemAsync_ShouldGenerateId_WhenIdIsEmpty()
        {
            // Arrange
            var task = TaskItemFactory.Create();
            Assert.Equal(ObjectId.Empty, task.Id);

            // Act
            await _repository.AddTaskItemAsync(task);

            // Assert
            Assert.NotEqual(ObjectId.Empty, task.Id);
        }

        [Fact]
        public async Task AddTaskItemAsync_ShouldPersistEveryField()
        {
            // Arrange
            var task = TaskItemFactory.Create(assigneeId: 555555555555555555);
            task.Priority = Priority.High;
            task.Comments.Add(new Comment
            {
                AuthorId = 444444444444444444,
                Text = "A persisted comment",
                CreatedAt = new DateTime(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc),
            });

            // Act
            await _repository.AddTaskItemAsync(task);
            var result = await _repository.GetTaskItemByObjectIdOrDefaultAsync(task.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TaskItemFactory.DefaultGuildId, result.GuildId);
            Assert.Equal(task.AuthorId, result.AuthorId);
            Assert.Equal(555555555555555555UL, result.AssigneeId);
            Assert.Equal(Priority.High, result.Priority);
            Assert.Equal(BoardStatus.Backlog, result.Status);
            Assert.Equal(task.CreatedAt, result.CreatedAt);
            var comment = Assert.Single(result.Comments);
            Assert.Equal(444444444444444444UL, comment.AuthorId);
            Assert.Equal("A persisted comment", comment.Text);
            Assert.Equal(new DateTime(2026, 2, 1, 8, 30, 0, DateTimeKind.Utc), comment.CreatedAt);
        }

        [Fact]
        public async Task GetAllTaskItemsByGuildIdAsync_ShouldReturnEmptyList_WhenGuildHasNoTasks()
        {
            // Arrange
            await _repository.AddTaskItemAsync(TaskItemFactory.Create(guildId: 1));

            // Act
            var result = await _repository.GetAllTaskItemsByGuildIdAsync(2);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllTaskItemsByGuildIdAsync_ShouldReturnTasksInInsertionOrder()
        {
            // Arrange: board numbers ("1 - Title") and autocomplete labels depend on this order
            var titles = Enumerable.Range(1, 25).Select(i => $"Task {i}").ToList();

            foreach (var title in titles)
                await _repository.AddTaskItemAsync(TaskItemFactory.Create(title: title));

            // Act
            var result = await _repository.GetAllTaskItemsByGuildIdAsync(TaskItemFactory.DefaultGuildId);

            // Assert
            Assert.Equal(titles, result.Select(x => x.Title));
        }

        [Fact]
        public async Task UpdateTaskItemAsync_ShouldNotInsertTask_WhenTaskDoesNotExist()
        {
            // Arrange
            var task = TaskItemFactory.Create();
            task.Id = ObjectId.GenerateNewId();

            // Act
            await _repository.UpdateTaskItemAsync(task);

            // Assert
            Assert.Empty(await _repository.GetAllTaskItemsByGuildIdAsync(task.GuildId));
        }

        [Fact]
        public async Task RemoveAllTaskItemsByIdAsync_ShouldNotRemoveTasksOfOtherGuilds()
        {
            // Arrange
            foreach (var task in TaskItemFactory.CreateMany())
                await _repository.AddTaskItemAsync(task);

            // Act
            await _repository.RemoveAllTaskItemsByIdAsync(TaskItemFactory.DefaultGuildId);

            // Assert
            Assert.Empty(await _repository.GetAllTaskItemsByGuildIdAsync(TaskItemFactory.DefaultGuildId));
            Assert.Single(await _repository.GetAllTaskItemsByGuildIdAsync(1));
        }
```

- [ ] **Step 2: Add settings repository tests** — inside `SettingsRepositoryTests` (add `using KanbanCord.Tests.Factories;`)

```csharp
    [Fact]
    public async Task AddAsync_ShouldPersistNullArchiveAfter()
    {
        // Arrange
        var settings = SettingsFactory.Create();

        // Act
        await _repository.AddAsync(settings);
        var result = await _repository.GetByIdOrDefaultAsync(settings.GuildId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ArchiveAfter);
    }

    [Fact]
    public async Task GetByIdOrDefaultAsync_ShouldReturnOnlyTheRequestedGuild()
    {
        // Arrange
        foreach (var settings in SettingsFactory.CreateMany())
            await _repository.AddAsync(settings);

        // Act
        var result = await _repository.GetByIdOrDefaultAsync(3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3UL, result.GuildId);
        Assert.Equal(TimeSpan.FromDays(30), result.ArchiveAfter);
    }

    [Fact]
    public async Task RemoveAsync_ShouldNotThrowOrAffectOthers_WhenSettingsDoNotExist()
    {
        // Arrange
        await _repository.AddAsync(SettingsFactory.Create(guildId: 1));

        // Act
        await _repository.RemoveAsync(999);

        // Assert
        Assert.NotNull(await _repository.GetByIdOrDefaultAsync(1));
    }
```

- [ ] **Step 3: Add the Mongo-backed service test** — `KanbanCord.Tests/ServiceTests/TaskServiceMongoTests.cs` (exercises `TaskService` + real `TaskItemRepository`, the same end-to-end path production uses)

```csharp
using KanbanCord.Core.Exceptions;
using KanbanCord.Core.Models;
using KanbanCord.Core.Repositories;
using KanbanCord.Core.Services;
using KanbanCord.Tests.Factories;
using Mongo2Go;
using MongoDB.Driver;

namespace KanbanCord.Tests.ServiceTests;

public class TaskServiceMongoTests : IDisposable
{
    private const ulong GuildId = TaskItemFactory.DefaultGuildId;

    private readonly MongoDbRunner _runner;
    private readonly TaskService _service;
    private readonly BoardService _boardService;

    public TaskServiceMongoTests()
    {
        _runner = MongoDbRunner.Start();

        var database = new MongoClient(_runner.ConnectionString).GetDatabase("KanbanCord");
        var repository = new TaskItemRepository(database);

        _service = new TaskService(repository, TimeProvider.System);
        _boardService = new BoardService(repository);
    }

    [Fact]
    public async Task TaskLifecycle_ShouldWorkAgainstRealMongo()
    {
        // Arrange & Act
        var added = await _service.AddAsync(GuildId, 1, "Ship it", "Release v2");
        var id = added.Id.ToString();

        await _service.EditAsync(GuildId, id, "Ship it now", "Release v2.0");
        await _service.AddCommentAsync(GuildId, id, 2, "Looks good to me!");
        await _service.StartAsync(GuildId, id);
        await _service.AssignAsync(GuildId, id, 3);
        await _service.SetPriorityAsync(GuildId, id, Priority.High);

        // Assert
        var stored = await _service.GetAsync(GuildId, id);
        Assert.Equal("Ship it now", stored.Title);
        Assert.Equal(BoardStatus.InProgress, stored.Status);
        Assert.Equal(3UL, stored.AssigneeId);
        Assert.Equal(Priority.High, stored.Priority);
        Assert.Equal("Looks good to me!", Assert.Single(stored.Comments).Text);

        var board = await _boardService.GetColumnsAsync(GuildId, [BoardStatus.Backlog, BoardStatus.InProgress]);
        Assert.Empty(board[0].Entries);
        Assert.Equal(1, Assert.Single(board[1].Entries).Number);
    }

    [Fact]
    public async Task ClearThenReset_ShouldArchiveThenDeleteOnlyTheGuildsTasks()
    {
        // Arrange
        await _service.AddAsync(GuildId, 1, "One", "d");
        await _service.AddAsync(GuildId, 1, "Two", "d");
        await _service.AddAsync(1, 1, "Foreign", "d");

        // Act & Assert
        Assert.Equal(2, await _service.ClearBoardAsync(GuildId));
        var archived = await _boardService.GetColumnsAsync(GuildId, [BoardStatus.Archived]);
        Assert.Equal(2, archived[0].Entries.Count);

        await _service.ResetBoardAsync(GuildId);
        Assert.Empty((await _boardService.GetColumnsAsync(GuildId, [BoardStatus.Archived]))[0].Entries);
        Assert.Single((await _boardService.GetColumnsAsync(1, [BoardStatus.Backlog]))[0].Entries);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowNotFound_ForTaskOfAnotherGuild()
    {
        // Arrange
        var foreign = await _service.AddAsync(1, 1, "Foreign", "d");

        // Act & Assert
        await Assert.ThrowsAsync<KanbanCordException>(() => _service.GetAsync(GuildId, foreign.Id.ToString()));
    }

    public void Dispose() => _runner.Dispose();
}
```

- [ ] **Step 4: Run** — `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~RepositoryTests|FullyQualifiedName~TaskServiceMongoTests"` → all PASS. If `GetAllTaskItemsByGuildIdAsync_ShouldReturnTasksInInsertionOrder` fails, add `.OrderBy(task => task.Id)` to the first line of `BoardService.GetColumnsAsync`'s task enumeration (`tasks.OrderBy(t => t.Id)`), re-run Task 4's tests, and keep this test. If Mongo2Go cannot start the embedded binary here, say so explicitly instead of reporting a pass.

- [ ] **Step 5: Update spec §7** — add: "Test data comes from `KanbanCord.Tests/Factories`; repository tests cover persistence edge cases the services rely on (id generation, full-document round trip, insertion order, no upsert on update, guild isolation); `TaskServiceMongoTests` runs the real services over Mongo2Go."

- [ ] **Step 6: Commit**

```bash
git add KanbanCord.Tests docs/superpowers/specs
git commit -m "Add repository edge-case tests and Mongo-backed service tests" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Presentation foundation (messages, board formatting, name + command-mention services)

**Files:**
- Modify: `KanbanCord.Tests/KanbanCord.Tests.csproj` (add `<ProjectReference Include="..\KanbanCord.DiscordApplication\KanbanCord.DiscordApplication.csproj" />`)
- Create: `KanbanCord.DiscordApplication/Presentation/Colors.cs`, `MessageFactory.cs`, `BoardFormatter.cs`, `BoardHasher.cs`, `BoardScope.cs`, `BoardView.cs`
- Create: `KanbanCord.DiscordApplication/Services/IUserNameResolver.cs`, `UserNameResolver.cs`, `ICommandMentionService.cs`, `CommandMentionService.cs`
- Test: `KanbanCord.Tests/PresentationTests/BoardFormatterTests.cs`, `BoardHasherTests.cs`
- Modify: `ServiceCollectionExtensions.cs` (`AddServices`)

**Interfaces:**
- Produces:
```csharp
public static class Colors { public static readonly Color Default = new(255, 117, 20); }

public static class MessageFactory
{
    public static InteractionMessageProperties Text(string text, bool ephemeral = false);                 // one container with a TextDisplay
    public static InteractionMessageProperties Container(IEnumerable<IComponentContainerComponentProperties> components, bool ephemeral = false);
    public static InteractionMessageProperties WithAllowedUsers(this InteractionMessageProperties m, params ulong[] userIds);   // opt-in pings; every other message sets AllowedMentions.None
}

public static class BoardFormatter
{
    public const string UnknownUser = "Unknown User";
    public static string FormatColumn(IReadOnlyList<BoardEntry> entries, Func<ulong, string> authorName, int maxLength);
}

public static class BoardHasher { public static string Hash(IReadOnlyList<BoardColumn> columns); }          // 8 hex chars, stable

public static class BoardScope
{
    public const string All = "all";
    public static IReadOnlyList<BoardStatus> Parse(string scope);     // "all" -> Backlog, InProgress, Completed; "Archived"/"Backlog"/... -> that status; garbage -> same as All
    public static string ToScope(BoardStatus? status);                 // null -> "all"
}

public static class BoardView
{
    public const int MultiColumnMaxLength = 1200, SingleColumnMaxLength = 3500;
    public static InteractionMessageProperties Build(IReadOnlyList<BoardColumn> columns, Func<ulong, string> authorName, string title, string scope, string? subtitle = null, bool refreshable = false);
}

public interface IUserNameResolver { Task<IReadOnlyDictionary<ulong, string>> ResolveAsync(IEnumerable<ulong> userIds); }   // unknown/failed ids map to BoardFormatter.UnknownUser; results cached 30 min

public interface ICommandMentionService
{
    ValueTask<string> GetMentionAsync(params string[] path);        // "</task view:123>" or "/task view" when unknown
    ValueTask<string> GetDescriptionAsync(params string[] path);    // registered description or ""
}
```
`InteractionIds.BoardRefresh` is created in Task 7; until then `BoardView` references the literal via a private const that Task 7 replaces (see Step 8).

- [ ] **Step 1: Reference the app from the test project** — add the ProjectReference above, then `dotnet build KanbanCord.Tests/KanbanCord.Tests.csproj` (expect PASS; confirms a Web-SDK project can be referenced).

- [ ] **Step 2: Failing tests**

`KanbanCord.Tests/PresentationTests/BoardFormatterTests.cs`
```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;

namespace KanbanCord.Tests.PresentationTests;

public class BoardFormatterTests
{
    private static BoardEntry Entry(int number, string title = "Task", ulong authorId = 1) =>
        new(number, new TaskItem { GuildId = 1, Title = title, Description = "d", AuthorId = authorId });

    [Fact]
    public void FormatColumn_ShouldRenderPlaceholder_WhenColumnIsEmpty()
    {
        // Act
        var result = BoardFormatter.FormatColumn([], _ => "x", 1000);

        // Assert
        Assert.Equal("```bash\n ```", result);
    }

    [Fact]
    public void FormatColumn_ShouldRenderNumberTitleAndAuthor()
    {
        // Act
        var result = BoardFormatter.FormatColumn([Entry(1, "Fix bug", 7), Entry(2, "Ship it", 8)], id => $"user{id}", 1000);

        // Assert
        Assert.Equal("```bash\n1 - \"Fix bug\" added by: user7\n2 - \"Ship it\" added by: user8```", result);
    }

    [Fact]
    public void FormatColumn_ShouldTruncateWithRemainingCount_WhenTooLong()
    {
        // Arrange
        var entries = Enumerable.Range(1, 200).Select(i => Entry(i, new string('t', 40))).ToList();

        // Act
        var result = BoardFormatter.FormatColumn(entries, _ => "someone", 500);

        // Assert
        Assert.True(result.Length <= 500, $"Length was {result.Length}");
        Assert.Matches(@"…and \d+ more```$", result);
        Assert.StartsWith("```bash\n1 - ", result);
    }

    [Fact]
    public void FormatColumn_ShouldNeverExceedMax_ForSingleColumnLimit()
    {
        // Arrange
        var entries = Enumerable.Range(1, 500).Select(i => Entry(i, new string('t', 40))).ToList();

        // Act
        var result = BoardFormatter.FormatColumn(entries, _ => new string('n', 32), BoardView.SingleColumnMaxLength);

        // Assert
        Assert.True(result.Length <= BoardView.SingleColumnMaxLength);
    }

    [Fact]
    public void FormatColumn_ShouldShowUnknownUser_WhenNameCannotBeResolved()
    {
        // Act
        var result = BoardFormatter.FormatColumn([Entry(1, "Task", 99)], _ => BoardFormatter.UnknownUser, 1000);

        // Assert
        Assert.Contains("added by: Unknown User", result);
    }
}
```

`KanbanCord.Tests/PresentationTests/BoardHasherTests.cs`
```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;

namespace KanbanCord.Tests.PresentationTests;

public class BoardHasherTests
{
    private static IReadOnlyList<BoardColumn> Board(string title, BoardStatus status = BoardStatus.Backlog, ulong? assignee = null) =>
        [new BoardColumn(status, [new BoardEntry(1, new TaskItem { GuildId = 1, Title = title, Description = "d", AuthorId = 1, AssigneeId = assignee })])];

    [Fact]
    public void Hash_ShouldBeStable_ForIdenticalBoards()
    {
        Assert.Equal(BoardHasher.Hash(Board("a")), BoardHasher.Hash(Board("a")));
    }

    [Fact]
    public void Hash_ShouldChange_WhenTitleOrColumnOrAssigneeChanges()
    {
        var baseline = BoardHasher.Hash(Board("a"));

        Assert.NotEqual(baseline, BoardHasher.Hash(Board("b")));
        Assert.NotEqual(baseline, BoardHasher.Hash(Board("a", BoardStatus.Completed)));
        Assert.NotEqual(baseline, BoardHasher.Hash(Board("a", assignee: 5)));
    }

    [Fact]
    public void Hash_ShouldBeEightHexCharacters()
    {
        Assert.Matches("^[0-9a-f]{8}$", BoardHasher.Hash(Board("a")));
    }
}
```

- [ ] **Step 3: Run to confirm failure** — `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~PresentationTests"` → build FAIL.

- [ ] **Step 4: Implement the pure pieces**

`Presentation/Colors.cs`
```csharp
using NetCord;

namespace KanbanCord.DiscordApplication.Presentation;

public static class Colors
{
    public static readonly Color Default = new(255, 117, 20);
}
```

`Presentation/MessageFactory.cs`
```csharp
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class MessageFactory
{
    public static InteractionMessageProperties Text(string text, bool ephemeral = false) =>
        Container([new TextDisplayProperties(text)], ephemeral);

    public static InteractionMessageProperties Container(IEnumerable<IComponentContainerComponentProperties> components, bool ephemeral = false) =>
        new()
        {
            Components = [new ComponentContainerProperties(components).WithAccentColor(Colors.Default)],
            Flags = MessageFlags.IsComponentsV2 | (ephemeral ? MessageFlags.Ephemeral : 0),
            AllowedMentions = AllowedMentionsProperties.None,
        };

    public static InteractionMessageProperties WithAllowedUsers(this InteractionMessageProperties message, params ulong[] userIds)
    {
        message.AllowedMentions = new AllowedMentionsProperties { AllowedUsers = userIds };

        return message;
    }
}
```

`Presentation/BoardFormatter.cs`
```csharp
using KanbanCord.Core.Services;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardFormatter
{
    public const string UnknownUser = "Unknown User";

    private const string Fence = "```bash\n";
    private const string Closing = "```";
    private const int OverflowLineReserve = 30; // room for "…and 1234 more"

    public static string FormatColumn(IReadOnlyList<BoardEntry> entries, Func<ulong, string> authorName, int maxLength)
    {
        if (entries.Count == 0)
            return $"{Fence} {Closing}";

        var lines = entries
            .Select(entry => $"{entry.Number} - \"{entry.Task.Title}\" added by: {authorName(entry.Task.AuthorId)}")
            .ToList();

        var budget = maxLength - Fence.Length - Closing.Length - OverflowLineReserve;

        var included = new List<string>();
        var used = 0;

        foreach (var line in lines)
        {
            var cost = line.Length + 1; // + newline

            if (used + cost > budget)
                break;

            included.Add(line);
            used += cost;
        }

        var hidden = lines.Count - included.Count;

        if (hidden > 0)
            included.Add($"…and {hidden} more");

        return $"{Fence}{string.Join('\n', included)}{Closing}";
    }
}
```

`Presentation/BoardHasher.cs`
```csharp
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
```

`Presentation/BoardScope.cs`
```csharp
using KanbanCord.Core.Models;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardScope
{
    public const string All = "all";

    private static readonly BoardStatus[] DefaultColumns = [BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed];

    public static IReadOnlyList<BoardStatus> Parse(string scope) =>
        Enum.TryParse<BoardStatus>(scope, out var status) && Enum.IsDefined(status) ? [status] : DefaultColumns;

    public static string ToScope(BoardStatus? status) => status?.ToString() ?? All;
}
```

`Presentation/BoardView.cs` (uses a private const for the refresh id until Task 7)
```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class BoardView
{
    public const int MultiColumnMaxLength = 1200;
    public const int SingleColumnMaxLength = 3500;

    private const string RefreshId = "board-refresh"; // replaced by InteractionIds.BoardRefresh in Task 7

    public static InteractionMessageProperties Build(
        IReadOnlyList<BoardColumn> columns,
        Func<ulong, string> authorName,
        string title,
        string scope,
        string? subtitle = null,
        bool refreshable = false)
    {
        List<IComponentContainerComponentProperties> components = [new TextDisplayProperties($"### {title}")];

        if (subtitle is not null)
            components.Add(new TextDisplayProperties(subtitle));

        var single = columns.Count == 1;

        foreach (var column in columns)
        {
            var block = BoardFormatter.FormatColumn(column.Entries, authorName, single ? SingleColumnMaxLength : MultiColumnMaxLength);

            components.Add(new TextDisplayProperties($"**{column.Status.ToFormattedString()}**\n{block}"));
        }

        if (refreshable)
        {
            components.Add(new ActionRowProperties([
                new ButtonProperties($"{RefreshId}:{scope}:{BoardHasher.Hash(columns)}", "Refresh", EmojiProperties.Standard("🔄"), ButtonStyle.Secondary),
            ]));
        }

        return MessageFactory.Container(components);
    }
}
```

- [ ] **Step 5: Implement the Discord-facing services**

`Services/IUserNameResolver.cs`
```csharp
namespace KanbanCord.DiscordApplication.Services;

public interface IUserNameResolver
{
    Task<IReadOnlyDictionary<ulong, string>> ResolveAsync(IEnumerable<ulong> userIds);
}
```

`Services/UserNameResolver.cs`
```csharp
using KanbanCord.DiscordApplication.Presentation;
using Microsoft.Extensions.Caching.Memory;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Services;

public class UserNameResolver : IUserNameResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly RestClient _restClient;
    private readonly IMemoryCache _cache;

    public UserNameResolver(RestClient restClient, IMemoryCache cache)
    {
        _restClient = restClient;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<ulong, string>> ResolveAsync(IEnumerable<ulong> userIds)
    {
        var ids = userIds.Distinct().ToArray();

        var names = await Task.WhenAll(ids.Select(ResolveOneAsync));

        return ids.Zip(names).ToDictionary(pair => pair.First, pair => pair.Second);
    }

    private async Task<string> ResolveOneAsync(ulong userId)
    {
        var key = $"username:{userId}";

        if (_cache.TryGetValue(key, out string? cached) && cached is not null)
            return cached;

        try
        {
            var user = await _restClient.GetUserAsync(userId);

            _cache.Set(key, user.Username, CacheDuration);

            return user.Username;
        }
        catch (RestException)
        {
            // Deleted accounts or users Discord can no longer resolve; not cached so a transient failure heals
            return BoardFormatter.UnknownUser;
        }
    }
}
```

`Services/ICommandMentionService.cs`
```csharp
namespace KanbanCord.DiscordApplication.Services;

public interface ICommandMentionService
{
    ValueTask<string> GetMentionAsync(params string[] path);

    ValueTask<string> GetDescriptionAsync(params string[] path);
}
```

`Services/CommandMentionService.cs` (modelled on BumpBuddy's, extended with descriptions and a bounded cache)
```csharp
using Microsoft.Extensions.Caching.Memory;
using NetCord.Gateway;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Services;

public class CommandMentionService : ICommandMentionService
{
    private const string CacheKey = "application-commands";

    private readonly RestClient _restClient;
    private readonly ShardedGatewayClient _gatewayClient;
    private readonly IMemoryCache _cache;

    public CommandMentionService(RestClient restClient, ShardedGatewayClient gatewayClient, IMemoryCache cache)
    {
        _restClient = restClient;
        _gatewayClient = gatewayClient;
        _cache = cache;
    }

    public async ValueTask<string> GetMentionAsync(params string[] path)
    {
        var command = (await GetCommandsAsync()).FirstOrDefault(x => x.Name.Equals(path[0], StringComparison.OrdinalIgnoreCase));

        return command is null
            ? $"/{string.Join(' ', path)}"
            : $"</{string.Join(' ', path)}:{command.Id}>";
    }

    public async ValueTask<string> GetDescriptionAsync(params string[] path)
    {
        var command = (await GetCommandsAsync()).FirstOrDefault(x => x.Name.Equals(path[0], StringComparison.OrdinalIgnoreCase));

        if (command is null)
            return string.Empty;

        if (path.Length == 1)
            return command.Description;

        return command.Options?.FirstOrDefault(x => x.Name.Equals(path[1], StringComparison.OrdinalIgnoreCase))?.Description ?? string.Empty;
    }

    private async ValueTask<IReadOnlyList<ApplicationCommand>> GetCommandsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<ApplicationCommand>? commands) && commands is { Count: > 0 })
            return commands;

        commands = await _restClient.GetGlobalApplicationCommandsAsync(_gatewayClient.Id);

        // Only cache a populated list so a call made before commands are registered does not stick
        if (commands.Count > 0)
            _cache.Set(CacheKey, commands, TimeSpan.FromHours(1));

        return commands;
    }
}
```

- [ ] **Step 6: Register** in `AddServices()`:
```csharp
        services
            .AddScoped<IUserNameResolver, UserNameResolver>()
            .AddScoped<ICommandMentionService, CommandMentionService>()
            ;
```
(+ `using KanbanCord.DiscordApplication.Services;`) and remove the "Task 6 registers…" comment.

- [ ] **Step 7: Run tests and build**

Run: `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~PresentationTests"` → PASS; `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings.
If an API shape differs in beta.28 (`AllowedMentionsProperties.AllowedUsers`, `ButtonProperties(string, string, EmojiProperties, ButtonStyle)`, `ShardedGatewayClient.Id`, `Command.Options`), fix against the installed package (`~/.nuget/packages/netcord/1.0.0-beta.28`) or BumpBuddy usage.

- [ ] **Step 8: Commit**

```bash
git add KanbanCord.Tests KanbanCord.DiscordApplication
git commit -m "Add Presentation foundation, board formatter, user and command mention services" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Error handling, preconditions, interaction ids

**Files:**
- Create: `KanbanCord.DiscordApplication/Components/InteractionIds.cs`
- Create: `KanbanCord.DiscordApplication/Presentation/ErrorMessageFactory.cs`
- Create: `KanbanCord.DiscordApplication/Commands/ApplicationCommandResultHandler.cs`
- Create: `KanbanCord.DiscordApplication/Components/ComponentInteractionResultHandler.cs`
- Create: `KanbanCord.DiscordApplication/Attributes/RequireInteractionOwnerAttribute.cs`
- Modify: `Presentation/BoardView.cs` (use `InteractionIds.BoardRefresh`), `ServiceCollectionExtensions.cs` (`AddDiscordConfiguration`)

**Interfaces:**
- Produces:
```csharp
public static class InteractionIds
{
    public const string BoardRefresh = "board-refresh";            // board-refresh:{scope}:{hash}
    public const string TaskAddModal = "task-add-modal";
    public const string TaskEditModal = "task-edit-modal";         // :{taskId}
    public const string TaskCommentButton = "task-comment";        // :{taskId}
    public const string TaskCommentModal = "task-comment-modal";   // :{taskId}
    public const string TaskViewPage = "task-view-page";           // :{taskId}:{page}
    public const string TaskDeleteConfirm = "task-delete-confirm"; // :{taskId}
    public const string TaskDeleteCancel = "task-delete-cancel";
    public const string ClearConfirm = "clear-confirm";
    public const string ClearCancel = "clear-cancel";
    public const string ResetConfirm = "reset-confirm";
    public const string ResetCancel = "reset-cancel";
    public const string Noop = "noop";                             // disabled page-label buttons
    public const string TitleField = "titleField";
    public const string DescriptionField = "descriptionField";
    public const string CommentField = "commentField";
}

public static class ErrorMessageFactory
{
    public static InteractionMessageProperties Create(IFailResult result, string? supportInvite, ILogger logger, string source);
}

public class RequireInteractionOwnerAttribute<TContext> : PreconditionAttribute<TContext> where TContext : ButtonInteractionContext  // copy of BumpBuddy's
```
- Error rules (in this order): `KanbanCordException` → its message, ephemeral. `MissingPermissionsResult` → "You are missing the following permissions: `Manage Messages`" (user) or "I am missing…" (bot). Other `IExceptionResult` → log error, generic ephemeral message (+ Support link button only when `supportInvite` is set). `PreconditionFailResult` → its message, ephemeral. Anything else → generic.

- [ ] **Step 1: Create `InteractionIds`** with exactly the constants above.

- [ ] **Step 2: Create the owner precondition** — copy `/home/jasper/Projects/BumpBuddy/DiscordApplications/BumpBuddy.DiscordApp.BumpBuddy/Attributes/RequireInteractionOwnerAttribute.cs` into `Attributes/`, namespace `KanbanCord.DiscordApplication.Attributes`. Message: "This button is only available to the user who used the command." (unchanged).

- [ ] **Step 3: Create `ErrorMessageFactory`**

```csharp
using System.Text.RegularExpressions;
using KanbanCord.Core.Exceptions;
using NetCord;
using NetCord.Rest;
using NetCord.Services;

namespace KanbanCord.DiscordApplication.Presentation;

public static partial class ErrorMessageFactory
{
    public static InteractionMessageProperties Create(IFailResult result, string? supportInvite, ILogger logger, string source)
    {
        if (result is IExceptionResult { Exception: KanbanCordException known })
            return MessageFactory.Text(known.Message, ephemeral: true);

        if (result is MissingPermissionsResult missing)
        {
            var permissions = Enum.GetValues<Permissions>()
                .Where(permission => permission != 0 && missing.MissingPermissions.HasFlag(permission))
                .Select(permission => $"`{SpacedWords().Replace(permission.ToString(), " $1").Trim()}`");

            var subject = missing.EntityType == MissingPermissionsResultEntityType.Bot ? "I am" : "You are";

            return MessageFactory.Text($"{subject} missing the following permissions: {string.Join(", ", permissions)}", ephemeral: true);
        }

        if (result is IExceptionResult exceptionResult)
            logger.LogError(exceptionResult.Exception, "Execution of {Source} failed with an exception", source);
        else
            logger.LogDebug("Execution of {Source} failed with '{Message}'", source, result.Message);

        if (result is PreconditionFailResult precondition)
            return MessageFactory.Text(precondition.Message, ephemeral: true);

        List<IComponentContainerComponentProperties> components =
        [
            new TextDisplayProperties("Something went wrong while handling that. Please try again, and if the problem persists, contact support."),
        ];

        if (!string.IsNullOrWhiteSpace(supportInvite))
            components.Add(new ActionRowProperties([new LinkButtonProperties(supportInvite, "Support")]));

        return MessageFactory.Container(components, ephemeral: true);
    }

    [GeneratedRegex("([A-Z])")]
    private static partial Regex SpacedWords();
}
```
(Add `using Microsoft.Extensions.Logging;` if ImplicitUsings does not provide `ILogger` here — it does for the Web SDK.)

- [ ] **Step 4: Create the two result handlers** (modelled on BumpBuddy's)

`Commands/ApplicationCommandResultHandler.cs`
```csharp
using KanbanCord.Core.Options;
using KanbanCord.DiscordApplication.Presentation;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class ApplicationCommandResultHandler<TContext> : IApplicationCommandResultHandler<TContext> where TContext : IApplicationCommandContext
{
    private readonly KanbanCordOptions _options;

    public ApplicationCommandResultHandler(IOptions<KanbanCordOptions> options)
    {
        _options = options.Value;
    }

    public async ValueTask HandleResultAsync(IExecutionResult result, TContext context, GatewayClient? client, ILogger logger, IServiceProvider services)
    {
        if (result is not IFailResult failResult)
            return;

        var message = ErrorMessageFactory.Create(failResult, _options.SupportInvite, logger, $"command '{context.Interaction.Data.Name}'");

        await context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
    }
}
```
`Components/ComponentInteractionResultHandler.cs` — same shape, `IComponentInteractionResultHandler<TContext> where TContext : IComponentInteractionContext`, source `$"interaction '{context.Interaction.Data.CustomId}'"`, namespaces `NetCord.Hosting.Services.ComponentInteractions`, `NetCord.Services.ComponentInteractions`.

Add `using NetCord;` where `InteractionCallback` needs it.

- [ ] **Step 5: Wire into `AddDiscordConfiguration`** — replace the method body's chain with:

```csharp
        services
            .AddDiscordShardedGateway(options =>
            {
                options.Intents = GatewayIntents.Guilds;

                options.Presence = new PresenceProperties(UserStatusType.Online)
                    .WithActivities([new UserActivityProperties("Watching out for work", UserActivityType.Watching)]);
            })
            .AddApplicationCommands((options, sp) =>
            {
                options.ResultHandler = new ApplicationCommandResultHandler<ApplicationCommandContext>(sp.GetRequiredService<IOptions<KanbanCordOptions>>());
            })
            .AddShardedGatewayHandlers(typeof(Program).Assembly, ServiceLifetime.Scoped)
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>((options, sp) =>
            {
                options.ResultHandler = new ComponentInteractionResultHandler<ButtonInteractionContext>(sp.GetRequiredService<IOptions<KanbanCordOptions>>());
            })
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>((options, sp) =>
            {
                options.ResultHandler = new ComponentInteractionResultHandler<ModalInteractionContext>(sp.GetRequiredService<IOptions<KanbanCordOptions>>());
            })
            ;
```
Add usings: `KanbanCord.DiscordApplication.Commands`, `KanbanCord.DiscordApplication.Components`, `NetCord.Hosting.Services.ComponentInteractions`, `NetCord.Services.ApplicationCommands`, `NetCord.Services.ComponentInteractions`.

- [ ] **Step 6: Point `BoardView` at `InteractionIds.BoardRefresh`** and delete its private `RefreshId`.

- [ ] **Step 7: Build** — `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings. Run `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~PresentationTests|FullyQualifiedName~ServiceTests"` → PASS.

- [ ] **Step 8: Commit**

```bash
git add KanbanCord.DiscordApplication
git commit -m "Add central error handling, interaction ids and owner precondition" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Gateway event handlers

**Files:**
- Create: `KanbanCord.DiscordApplication/Events/GuildCreatedEventHandler.cs`, `GuildDeletedEventHandler.cs`

**Interfaces:**
- Consumes: `ISettingsService.RemoveGuildDataAsync` (Task 5). Handlers are discovered by `AddShardedGatewayHandlers(typeof(Program).Assembly, ServiceLifetime.Scoped)` (Task 7).

- [ ] **Step 1: Write the handlers**

`Events/GuildCreatedEventHandler.cs`
```csharp
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace KanbanCord.DiscordApplication.Events;

public class GuildCreatedEventHandler : IGuildCreateShardedGatewayHandler
{
    private readonly ILogger<GuildCreatedEventHandler> _logger;

    public GuildCreatedEventHandler(ILogger<GuildCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(GatewayClient client, GuildCreateEventArgs arg)
    {
        _logger.LogInformation("Guild available: {guildId}", arg.GuildId);

        return ValueTask.CompletedTask;
    }
}
```
Note: `GuildCreate` fires for every guild on every (re)connect, so the log line says "available", not "joined" (the old DSharpPlus handler logged name + id; the guild name is on `arg.Guild?.Name` if the type exposes it — include it only if the property exists, otherwise leave the id).

`Events/GuildDeletedEventHandler.cs`
```csharp
using KanbanCord.Core.Services;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace KanbanCord.DiscordApplication.Events;

public class GuildDeletedEventHandler : IGuildDeleteShardedGatewayHandler
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<GuildDeletedEventHandler> _logger;

    public GuildDeletedEventHandler(ISettingsService settingsService, ILogger<GuildDeletedEventHandler> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async ValueTask HandleAsync(GatewayClient client, GuildDeleteEventArgs arg)
    {
        // An outage makes guilds temporarily unavailable; only a real removal may delete data
        if (arg.IsUnavailable)
        {
            _logger.LogInformation("Guild became unavailable: {guildId}", arg.GuildId);
            return;
        }

        await _settingsService.RemoveGuildDataAsync(arg.GuildId);

        _logger.LogInformation("Left guild: {guildId}", arg.GuildId);
    }
}
```

- [ ] **Step 2: Build** — `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings.

- [ ] **Step 3: Commit**

```bash
git add KanbanCord.DiscordApplication/Events
git commit -m "Add guild gateway handlers" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 9: Read-only commands — `/board`, `/archive`, `/stats`, `/repository`, `/help`, board refresh

**Files:**
- Create: `Commands/ColumnChoice.cs` (shared with Task 10), `Commands/BoardCommand.cs`, `Commands/ArchiveCommand.cs`, `Commands/StatsCommand.cs`, `Commands/RepositoryCommand.cs`, `Commands/HelpCommand.cs`
- Create: `Components/BoardHandlers.cs`

**Interfaces:**
- Consumes: `IBoardService.GetColumnsAsync`, `IUserNameResolver.ResolveAsync`, `BoardView.Build`, `BoardScope`, `BoardHasher`, `MessageFactory`, `ICommandMentionService`, `KanbanCordOptions`, `Source` constants.
- Produces: `enum ColumnChoice { Backlog, InProgress, Completed, Archived }` (+ `ToBoardStatus()`), reused by `/task move`.
- Helper used by several handlers (add to `Presentation/` here): 
```csharp
public static class InteractionExtensions
{
    public static ulong RequireGuildId(this Interaction interaction) => interaction.GuildId ?? throw new KanbanCordException("This command can only be used in a server.");
}
```
(`KanbanCord.DiscordApplication.Presentation.InteractionExtensions`; commands are guild-only via `Contexts = [InteractionContextType.Guild]`, this is defence in depth.)

- [ ] **Step 1: `ColumnChoice`**

```csharp
using KanbanCord.Core.Models;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public enum ColumnChoice
{
    [SlashCommandChoice(Name = "Backlog")] Backlog,
    [SlashCommandChoice(Name = "In Progress")] InProgress,
    [SlashCommandChoice(Name = "Completed")] Completed,
    [SlashCommandChoice(Name = "Archived")] Archived,
}

public static class ColumnChoiceExtensions
{
    public static BoardStatus ToBoardStatus(this ColumnChoice choice) => choice switch
    {
        ColumnChoice.Backlog => BoardStatus.Backlog,
        ColumnChoice.InProgress => BoardStatus.InProgress,
        ColumnChoice.Completed => BoardStatus.Completed,
        _ => BoardStatus.Archived,
    };
}
```

- [ ] **Step 2: `/board`**

```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class BoardCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;

    public BoardCommand(IBoardService boardService, IUserNameResolver userNameResolver)
    {
        _boardService = boardService;
        _userNameResolver = userNameResolver;
    }

    [SlashCommand("board", "Displays a given column on the board.", Contexts = [InteractionContextType.Guild])]
    public async Task<InteractionMessageProperties> BoardAsync(
        [SlashCommandParameter(Description = "The column to show")] ColumnChoice? column = null)
    {
        var scope = BoardScope.ToScope(column?.ToBoardStatus());

        var columns = await _boardService.GetColumnsAsync(Context.Interaction.RequireGuildId(), BoardScope.Parse(scope));

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        return BoardView.Build(
            columns,
            id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser),
            "KanbanCord Board",
            scope,
            refreshable: true);
    }
}
```
`/archive` (`ArchiveCommand`) is identical but: `[SlashCommand("archive", "Displays all the archived tasks.", …)]`, no parameter, statuses `[BoardStatus.Archived]`, title `"KanbanCord Archive"`, `refreshable: false`.

- [ ] **Step 3: Board refresh handler** — `Components/BoardHandlers.cs`

```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class BoardHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;

    public BoardHandlers(IBoardService boardService, IUserNameResolver userNameResolver)
    {
        _boardService = boardService;
        _userNameResolver = userNameResolver;
    }

    [ComponentInteraction(InteractionIds.BoardRefresh)]
    public async Task<InteractionCallbackProperties> RefreshAsync(string scope, string hash)
    {
        var columns = await _boardService.GetColumnsAsync(Context.Interaction.RequireGuildId(), BoardScope.Parse(scope));

        if (BoardHasher.Hash(columns) == hash)
            return InteractionCallback.Message(MessageFactory.Text("Board already up to date.", ephemeral: true));

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        var message = BoardView.Build(columns, id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser), "KanbanCord Board", scope, refreshable: true);

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
```
`BoardScope.Parse` returns the default three columns for unknown scopes, so a tampered `scope` degrades to the normal board.

- [ ] **Step 4: `/stats`**

```csharp
using System.Diagnostics;
using System.Reflection;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class StatsCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ShardedGatewayClient _gatewayClient;

    public StatsCommand(ShardedGatewayClient gatewayClient)
    {
        _gatewayClient = gatewayClient;
    }

    [SlashCommand("stats", "Displays some stats of the bot.", Contexts = [InteractionContextType.Guild])]
    public InteractionMessageProperties Stats()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;

        var guildCount = _gatewayClient.Sum(shard => shard.Cache.Guilds.Count);

        var heapMemory = $"{GC.GetTotalMemory(false) / 1024 / 1024:n0} MB";

        using var process = Process.GetCurrentProcess();
        var uptime = DateTimeOffset.UtcNow - process.StartTime;

        var latency = (int)Context.Client.Latency.TotalMilliseconds;

        return MessageFactory.Container(
        [
            new TextDisplayProperties("### KanbanCord Stats"),
            new TextDisplayProperties($"**Latency:** {(latency == 0 ? "waiting for heartbeat..." : $"{latency} ms")}"),
            new TextDisplayProperties($"**Servers:** {guildCount}"),
            new TextDisplayProperties($"**Memory Usage:** {heapMemory}"),
            new TextDisplayProperties($"**Uptime:** {uptime.Days} days, {uptime.Hours} hours"),
            new TextDisplayProperties($"**Bot Version:** v{version.Major}.{version.Minor}.{version.Build}"),
        ]);
    }
}
```
If `Context.Client.Latency` is not a `TimeSpan` in beta.28, adapt (BumpBuddy's `ShardLatencyUpdateEventHandler` shows the property in use).

- [ ] **Step 5: `/repository`** — container with a `### KanbanCord Repository` display, the two paragraphs from the old `RepositoryCommand` ("What is KanbanCord?" and "Open Source Project", with `Source.LicenseUrl`) as `TextDisplayProperties`, then
```csharp
new ActionRowProperties([
    new LinkButtonProperties(Source.RepositoryUrl, "Github Repository"),
    new LinkButtonProperties($"https://discord.com/oauth2/authorize?client_id={Context.Client.Id}", "Invite Bot"),
])
```
Copy the exact prose from `KanbanCord.Bot/Commands/RepositoryCommand.cs`. `[SlashCommand("repository", "Get the repository url for this bot and other information.", Contexts = [InteractionContextType.Guild])]`.

- [ ] **Step 6: `/help`**

```csharp
using KanbanCord.Core.Options;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class HelpCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    private static readonly string[][] Commands =
    [
        ["board"], ["archive"], ["clear"], ["reset"], ["repository"], ["stats"],
        ["task", "add"], ["task", "edit"], ["task", "delete"], ["task", "view"], ["task", "start"],
        ["task", "complete"], ["task", "archive"], ["task", "move"], ["task", "assign"], ["task", "me"],
        ["task", "user"], ["task", "priority"], ["task", "comment"],
    ];

    private readonly ICommandMentionService _commandMentionService;
    private readonly KanbanCordOptions _options;

    public HelpCommand(ICommandMentionService commandMentionService, IOptions<KanbanCordOptions> options)
    {
        _commandMentionService = commandMentionService;
        _options = options.Value;
    }

    [SlashCommand("help", "Displays all the commands available.", Contexts = [InteractionContextType.Guild])]
    public async Task<InteractionMessageProperties> HelpAsync()
    {
        List<IComponentContainerComponentProperties> components = [new TextDisplayProperties("### KanbanCord Commands")];

        foreach (var path in Commands)
        {
            var mention = await _commandMentionService.GetMentionAsync(path);
            var description = await _commandMentionService.GetDescriptionAsync(path);

            components.Add(new TextDisplayProperties($"{mention}\n{description}"));
        }

        List<IButtonProperties> buttons = [new LinkButtonProperties($"https://discord.com/oauth2/authorize?client_id={Context.Client.Id}", "Invite")];

        if (_options.SupportInvite is not null)
            buttons.Add(new LinkButtonProperties(_options.SupportInvite, "Support"));

        components.Add(new ActionRowProperties(buttons));

        return MessageFactory.Container(components);
    }
}
```
(19 text displays + header + row = 21 components, under the 40 limit. If `ActionRowProperties` takes `IEnumerable<IActionRowComponentProperties>` rather than `IButtonProperties`, change the list type accordingly.)

- [ ] **Step 7: Build** — `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings.

- [ ] **Step 8: Manual check (dev guild with a dev bot token; first run registers commands — can take a minute to appear)**

Run: `Discord__Token=<dev token> Database__ConnectionString=mongodb://localhost:27017 dotnet run --project KanbanCord.DiscordApplication`. In Discord: `/board`, `/board column:Archived`, `/archive`, `/stats`, `/repository`, `/help`; click **Refresh** on `/board` with no change ("Board already up to date." ephemeral), add a task via mongosh/Compass and click again (board updates in place). Expected: all render as V2 containers, no embeds; `/help` command mentions are clickable.

- [ ] **Step 9: Commit**

```bash
git add KanbanCord.DiscordApplication
git commit -m "Add board, archive, stats, repository and help commands with board refresh" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 10: Autocomplete, choices, `/task` group skeleton and the read commands (`view`, `user`, `me`)

**Files:**
- Create: `Providers/TaskAutocompleteProvider.cs` (base + 4 concrete providers in one file each is not required; one file with the base and four small classes is fine)
- Create: `Commands/PriorityChoice.cs`
- Create: `Commands/Task/TaskCommandGroup.cs`, `TaskViewCommand.cs`, `TaskUserCommand.cs`, `TaskMeCommand.cs`
- Create: `Presentation/TaskView.cs`, `Presentation/ModalExtensions.cs` (used in Task 11)
- Create: `Components/TaskViewHandlers.cs`

**Interfaces:**
- Consumes: `IBoardService`, `ITaskService.GetAsync`, `IUserNameResolver`.
- Produces:
  - `AllTasksAutocompleteProvider`, `BacklogTasksAutocompleteProvider`, `InProgressTasksAutocompleteProvider`, `CompletedTasksAutocompleteProvider` (use as `AutocompleteProviderType = typeof(…)`); choice value is `task.Id.ToString()`, label `"[Column] n - Title"`, max 20, case-insensitive contains on the label.
  - `enum PriorityChoice { Low, Medium, High }` + `ToPriority()`.
  - `TaskCommandGroup` partial class: `[SlashCommand("task", "Task related commands", Contexts = [InteractionContextType.Guild])]`, fields `_taskService`, `_boardService`, `_userNameResolver`, `_commandMentionService`.
  - `TaskView.Build(TaskItem task, IReadOnlyDictionary<ulong,string> names, int page) : InteractionMessageProperties` — page 0 details; pages 1..N one comment each; row: `[◀][label disabled][▶][Add Comment]` when comments exist, else just `[Add Comment]`. Button ids: `task-view-page:{taskId}:{page}` and `task-comment:{taskId}`.
  - `TaskView.PageCount(TaskItem task) : int` = `1 + task.Comments.Count`.
  - `GetText(this IReadOnlyList<IModalComponent> components, string customId) : string` in `ModalExtensions`.

- [ ] **Step 1: Autocomplete providers**

```csharp
using KanbanCord.Core.Models;
using KanbanCord.Core.Services;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Providers;

public abstract class TaskAutocompleteProvider : IAutocompleteProvider<AutocompleteInteractionContext>
{
    private const int MaxChoices = 20;

    private readonly IBoardService _boardService;
    private readonly BoardStatus[] _statuses;

    protected TaskAutocompleteProvider(IBoardService boardService, params BoardStatus[] statuses)
    {
        _boardService = boardService;
        _statuses = statuses;
    }

    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        if (context.Interaction.GuildId is not { } guildId)
            return [];

        var columns = await _boardService.GetColumnsAsync(guildId, _statuses);

        return columns
            .SelectMany(column => column.Entries.Select(entry => new
            {
                Name = $"[{column.Status.ToFormattedString()}] {entry.Number} - {entry.Task.Title}",
                Value = entry.Task.Id.ToString(),
            }))
            .Where(choice => string.IsNullOrEmpty(option.Value) || choice.Name.Contains(option.Value, StringComparison.OrdinalIgnoreCase))
            .Take(MaxChoices)
            .Select(choice => new ApplicationCommandOptionChoiceProperties(choice.Name, choice.Value));
    }
}

public class AllTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed, BoardStatus.Archived);

public class BacklogTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.Backlog);

public class InProgressTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.InProgress);

public class CompletedTasksAutocompleteProvider(IBoardService boardService)
    : TaskAutocompleteProvider(boardService, BoardStatus.Completed);
```
(Choice names are capped by Discord at 100 characters; `[In Progress] 123 - ` + a 40-char title is well under.)

- [ ] **Step 2: `PriorityChoice`**

```csharp
using KanbanCord.Core.Models;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public enum PriorityChoice
{
    [SlashCommandChoice(Name = "Low")] Low,
    [SlashCommandChoice(Name = "Medium")] Medium,
    [SlashCommandChoice(Name = "High")] High,
}

public static class PriorityChoiceExtensions
{
    public static Priority ToPriority(this PriorityChoice choice) => choice switch
    {
        PriorityChoice.Low => Priority.Low,
        PriorityChoice.High => Priority.High,
        _ => Priority.Medium,
    };
}
```

- [ ] **Step 3: `ModalExtensions`**

```csharp
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
}
```

- [ ] **Step 4: `TaskView`**

```csharp
using KanbanCord.Core.Models;
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class TaskView
{
    public static int PageCount(TaskItem task) => 1 + task.Comments.Count;

    public static InteractionMessageProperties Build(TaskItem task, IReadOnlyDictionary<ulong, string> names, int page)
    {
        var pageCount = PageCount(task);
        var current = Math.Clamp(page, 0, pageCount - 1);

        string NameOf(ulong id) => names.GetValueOrDefault(id, BoardFormatter.UnknownUser);

        List<IComponentContainerComponentProperties> components = [];

        if (current == 0)
        {
            var priority = task.Priority switch
            {
                Priority.Low => "🟡 Low",
                Priority.High => "🔴 High",
                _ => "🟠 Medium",
            };

            components.Add(new TextDisplayProperties($"### {task.Title}"));
            components.Add(new TextDisplayProperties(task.Description));
            components.Add(new ComponentSeparatorProperties());
            components.Add(new TextDisplayProperties(
                $"**Author:** <@{task.AuthorId}> ({NameOf(task.AuthorId)})\n" +
                $"**Assigned To:** {(task.AssigneeId is { } assignee ? $"<@{assignee}>" : "None")}\n" +
                $"**Current Column:** {task.Status.ToFormattedString()}\n" +
                $"**Priority:** {priority}\n" +
                $"**Created At:** {new Timestamp(task.CreatedAt, TimestampStyle.LongDateTime)}\n" +
                $"**Last Updated At:** {new Timestamp(task.LastUpdatedAt, TimestampStyle.LongDateTime)}"));

            if (task.Comments.Count > 0)
                components.Add(new TextDisplayProperties("-# View comments on the following pages"));
        }
        else
        {
            var comment = task.Comments[current - 1];

            components.Add(new TextDisplayProperties($"### Comment by {NameOf(comment.AuthorId)}"));
            components.Add(new TextDisplayProperties(comment.Text));
            components.Add(new TextDisplayProperties($"-# Comment added {new Timestamp(comment.CreatedAt, TimestampStyle.LongDateTime)}"));
        }

        List<IActionRowComponentProperties> buttons = [];

        if (pageCount > 1)
        {
            var back = current == 0 ? pageCount - 1 : current - 1;
            var forward = current == pageCount - 1 ? 0 : current + 1;

            buttons.Add(new ButtonProperties($"{InteractionIds.TaskViewPage}:{task.Id}:{back}", EmojiProperties.Standard("◀️"), ButtonStyle.Primary));
            buttons.Add(new ButtonProperties(InteractionIds.Noop, $"{current + 1}/{pageCount}", ButtonStyle.Secondary).WithDisabled());
            buttons.Add(new ButtonProperties($"{InteractionIds.TaskViewPage}:{task.Id}:{forward}", EmojiProperties.Standard("▶️"), ButtonStyle.Primary));
        }

        buttons.Add(new ButtonProperties($"{InteractionIds.TaskCommentButton}:{task.Id}", "Add Comment", EmojiProperties.Standard("💬"), ButtonStyle.Secondary));

        components.Add(new ActionRowProperties(buttons));

        return MessageFactory.Container(components);
    }
}
```
Add `using KanbanCord.DiscordApplication.Components;` for `InteractionIds`. `new Timestamp(DateTime, style)`: if the constructor wants `DateTimeOffset`, use `new DateTimeOffset(DateTime.SpecifyKind(task.CreatedAt, DateTimeKind.Utc))`. `Noop` is a button no handler listens to (disabled, so it never fires).

- [ ] **Step 5: Task group skeleton**

`Commands/Task/TaskCommandGroup.cs`
```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Services;
using NetCord;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.Task;

[SlashCommand("task", "Task related commands", Contexts = [InteractionContextType.Guild])]
public partial class TaskCommandGroup : ApplicationCommandModule<ApplicationCommandContext>
{
    private readonly ITaskService _taskService;
    private readonly IBoardService _boardService;
    private readonly IUserNameResolver _userNameResolver;
    private readonly ICommandMentionService _commandMentionService;

    public TaskCommandGroup(
        ITaskService taskService,
        IBoardService boardService,
        IUserNameResolver userNameResolver,
        ICommandMentionService commandMentionService)
    {
        _taskService = taskService;
        _boardService = boardService;
        _userNameResolver = userNameResolver;
        _commandMentionService = commandMentionService;
    }
}
```
(No namespace clash: the namespace segment `Task` shadows `System.Threading.Tasks.Task` inside `KanbanCord.DiscordApplication.Commands.Task`. The old project had the same layout and used `ValueTask`; to avoid ambiguity, name the namespace `KanbanCord.DiscordApplication.Commands.TaskGroup` and the folder `Commands/Task/` — the folder name does not affect the namespace if you declare it explicitly. Use `namespace KanbanCord.DiscordApplication.Commands.TaskGroup;` in every file of this folder.)

- [ ] **Step 6: `view`, `user`, `me`**

`TaskViewCommand.cs`
```csharp
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Providers;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskGroup;

public partial class TaskCommandGroup
{
    // Open to everyone (the old command overrode the group's Manage Messages requirement)
    [SubSlashCommand("view", "View a tasks details such as description, author and comments.")]
    public async Task<InteractionMessageProperties> ViewAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);

        var names = await _userNameResolver.ResolveAsync(item.Comments.Select(c => c.AuthorId).Append(item.AuthorId));

        return TaskView.Build(item, names, 0);
    }
}
```

`TaskUserCommand.cs` + `TaskMeCommand.cs`
```csharp
using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskGroup;

public partial class TaskCommandGroup
{
    [SubSlashCommand("user", "Displays all the tasks assigned to a specified user.")]
    public Task<InteractionMessageProperties> UserAsync(User user) => BuildAssignedTasksAsync(user.Id);

    [SubSlashCommand("me", "Displays all the tasks assigned to you.")]
    public Task<InteractionMessageProperties> MeAsync() => BuildAssignedTasksAsync(Context.User.Id);

    private async Task<InteractionMessageProperties> BuildAssignedTasksAsync(ulong assigneeId)
    {
        var columns = await _boardService.GetColumnsAsync(
            Context.Interaction.RequireGuildId(),
            [BoardStatus.Backlog, BoardStatus.InProgress, BoardStatus.Completed],
            assigneeId);

        var names = await _userNameResolver.ResolveAsync(columns.SelectMany(c => c.Entries).Select(e => e.Task.AuthorId));

        return BoardView.Build(
            columns,
            id => names.GetValueOrDefault(id, BoardFormatter.UnknownUser),
            "KanbanCord Board",
            BoardScope.All,
            subtitle: $"Tasks assigned to <@{assigneeId}>");
    }
}
```
(Put both in one file `TaskUserCommand.cs`; `TaskMeCommand` is not a separate file in this layout — fine, parity is by command, not file.)

- [ ] **Step 7: Pagination handler** — `Components/TaskViewHandlers.cs`

```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class TaskViewHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskService _taskService;
    private readonly IUserNameResolver _userNameResolver;

    public TaskViewHandlers(ITaskService taskService, IUserNameResolver userNameResolver)
    {
        _taskService = taskService;
        _userNameResolver = userNameResolver;
    }

    // Anyone may page through a task, matching the old allowUsageByAnyone: true
    [ComponentInteraction(InteractionIds.TaskViewPage)]
    public async Task<InteractionCallbackProperties> PageAsync(string taskId, int page)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), taskId);

        var names = await _userNameResolver.ResolveAsync(item.Comments.Select(c => c.AuthorId).Append(item.AuthorId));

        var message = TaskView.Build(item, names, page);

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
```

- [ ] **Step 8: Build + manual check**

`dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings. Manual (needs tasks; insert two via Compass or wait for Task 11): `/task view` with autocomplete showing `[Backlog] 1 - …`; tamper check — paste another guild's task id as the `task` option → "The selected task was not found, please try again." ephemeral; `/task me`, `/task user @someone`.

- [ ] **Step 9: Commit**

```bash
git add KanbanCord.DiscordApplication
git commit -m "Add task autocomplete providers, task group skeleton and view/user/me commands" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 11: `/task` mutations — add, edit, comment (modals), start, complete, archive, move, priority, assign, delete (confirmation)

**Files:**
- Create: `Presentation/TaskModals.cs`
- Create: `Commands/Task/TaskAddCommand.cs`, `TaskEditCommand.cs`, `TaskCommentCommand.cs`, `TaskStatusCommands.cs` (start/complete/archive/move), `TaskPriorityCommand.cs`, `TaskAssignCommand.cs`, `TaskDeleteCommand.cs`
- Create: `Components/TaskModalHandlers.cs`, `Components/TaskButtonHandlers.cs`

**Interfaces:**
- Consumes: all `ITaskService` members (Tasks 2–3), `ICommandMentionService`, `TaskView`, `ModalExtensions.GetText`, `InteractionIds`, `RequireInteractionOwnerAttribute`.
- Produces:
```csharp
public static class TaskModals
{
    public static ModalProperties Add();                       // id InteractionIds.TaskAddModal
    public static ModalProperties Edit(TaskItem task);         // id $"{TaskEditModal}:{task.Id}", prefilled
    public static ModalProperties Comment(string taskId);      // id $"{TaskCommentModal}:{taskId}"
}
```
- Every mutating subcommand carries `[RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]` (parity: only `view`, `user`, `me` are open). Before writing the start/complete/archive texts, read the old `TaskStartCommand`, `TaskArchiveCommand` (already read) and `TaskCompleteCommand` (not yet read) and copy their message wording exactly.

- [ ] **Step 1: `TaskModals`**

```csharp
using KanbanCord.Core.Constants;
using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Components;
using NetCord;
using NetCord.Rest;

namespace KanbanCord.DiscordApplication.Presentation;

public static class TaskModals
{
    public static ModalProperties Add() =>
        new(InteractionIds.TaskAddModal, "Add a new Task", [TitleInput(null), DescriptionInput(null)]);

    public static ModalProperties Edit(TaskItem task) =>
        new($"{InteractionIds.TaskEditModal}:{task.Id}", "Edit a Task", [TitleInput(task.Title), DescriptionInput(task.Description)]);

    public static ModalProperties Comment(string taskId) =>
        new($"{InteractionIds.TaskCommentModal}:{taskId}", "Add a comment to a task",
        [
            new LabelProperties("Comment:",
                new TextInputProperties(InteractionIds.CommentField, TextInputStyle.Paragraph)
                    .WithPlaceholder("Put your comment here")
                    .WithMinLength(Limits.TaskCommentMinLength)
                    .WithMaxLength(Limits.TaskCommentMaxLength)),
        ]);

    private static LabelProperties TitleInput(string? value) =>
        new("Title:",
            new TextInputProperties(InteractionIds.TitleField, TextInputStyle.Short)
                .WithPlaceholder("Title of the task")
                .WithMaxLength(Limits.TaskTitleMaxLength)
                .WithValue(value));

    private static LabelProperties DescriptionInput(string? value) =>
        new("Description:",
            new TextInputProperties(InteractionIds.DescriptionField, TextInputStyle.Paragraph)
                .WithPlaceholder("Description of the task")
                .WithMaxLength(Limits.TaskDescriptionMaxLength)
                .WithValue(value));
}
```

- [ ] **Step 2: Modal-opening commands**

`TaskAddCommand.cs`
```csharp
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskGroup;

public partial class TaskCommandGroup
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("add", "Add a task to the backlog")]
    public Task<InteractionCallbackProperties> AddAsync() =>
        Task.FromResult<InteractionCallbackProperties>(InteractionCallback.Modal(TaskModals.Add()));
}
```
`TaskEditCommand.cs`: same attributes, `[SubSlashCommand("edit", "Edit a task title and or description.")]`, parameter `task` with `AllTasksAutocompleteProvider`; body:
```csharp
var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);
return InteractionCallback.Modal(TaskModals.Edit(item));
```
`TaskCommentCommand.cs`: `[SubSlashCommand("comment", "Add a comment to a task")]`, same parameter; body: `await _taskService.GetAsync(guildId, task);` (validates existence/guild up front, as the old command did) then `return InteractionCallback.Modal(TaskModals.Comment(task));`.
(Both are `async Task<InteractionCallbackProperties>`; `Context.Interaction.RequireGuildId()` needs `using KanbanCord.DiscordApplication.Presentation;`.)

- [ ] **Step 3: Modal handlers** — `Components/TaskModalHandlers.cs`

```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Services;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class TaskModalHandlers : ComponentInteractionModule<ModalInteractionContext>
{
    private readonly ITaskService _taskService;
    private readonly ICommandMentionService _commandMentionService;

    public TaskModalHandlers(ITaskService taskService, ICommandMentionService commandMentionService)
    {
        _taskService = taskService;
        _commandMentionService = commandMentionService;
    }

    [ComponentInteraction(InteractionIds.TaskAddModal)]
    public async Task<InteractionCallbackProperties> AddAsync()
    {
        var task = await _taskService.AddAsync(
            Context.Interaction.RequireGuildId(),
            Context.User.Id,
            Context.Components.GetText(InteractionIds.TitleField),
            Context.Components.GetText(InteractionIds.DescriptionField));

        var board = await _commandMentionService.GetMentionAsync("board");

        return InteractionCallback.Message(MessageFactory.Text($"The task \"{task.Title}\" has been added to the backlog. View it using {board}."));
    }

    [ComponentInteraction(InteractionIds.TaskEditModal)]
    public async Task<InteractionCallbackProperties> EditAsync(string taskId)
    {
        var task = await _taskService.EditAsync(
            Context.Interaction.RequireGuildId(),
            taskId,
            Context.Components.GetText(InteractionIds.TitleField),
            Context.Components.GetText(InteractionIds.DescriptionField));

        var board = await _commandMentionService.GetMentionAsync("board");

        return InteractionCallback.Message(MessageFactory.Text($"The task \"{task.Title}\" has been edited. View it using {board}."));
    }

    [ComponentInteraction(InteractionIds.TaskCommentModal)]
    public async Task<InteractionCallbackProperties> CommentAsync(string taskId)
    {
        var task = await _taskService.AddCommentAsync(
            Context.Interaction.RequireGuildId(),
            taskId,
            Context.User.Id,
            Context.Components.GetText(InteractionIds.CommentField));

        var view = await _commandMentionService.GetMentionAsync("task", "view");

        return InteractionCallback.Message(MessageFactory.Text($"A comment has been added to task \"{task.Title}\". View it using {view}."));
    }
}
```

- [ ] **Step 4: Status commands** — `Commands/Task/TaskStatusCommands.cs` (one file, four subcommands; all `[RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]`)

```csharp
using KanbanCord.Core.Models;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Providers;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskGroup;

public partial class TaskCommandGroup
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("start", "Start a task and have it moved from Backlog to the In-Progress column.")]
    public async Task<InteractionMessageProperties> StartAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(BacklogTasksAutocompleteProvider))] string task)
    {
        var change = await _taskService.StartAsync(Context.Interaction.RequireGuildId(), task);

        var board = await _commandMentionService.GetMentionAsync("board");

        return MessageFactory.Text($"The task \"{change.Task.Title}\" has been started and moved to the **In Progress** column. View it using {board}.");
    }

    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("complete", "<copy the description from the old TaskCompleteCommand>")]
    public async Task<InteractionMessageProperties> CompleteAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(InProgressTasksAutocompleteProvider))] string task)
    {
        var change = await _taskService.CompleteAsync(Context.Interaction.RequireGuildId(), task);

        var board = await _commandMentionService.GetMentionAsync("board");

        return MessageFactory.Text($"<copy the old TaskCompleteCommand message, using change.Task.Title and {board}>");
    }

    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("archive", "Archive a task and have it moved to the list of archived items.")]
    public async Task<InteractionMessageProperties> ArchiveAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(CompletedTasksAutocompleteProvider))] string task)
    {
        var change = await _taskService.ArchiveAsync(Context.Interaction.RequireGuildId(), task);

        var archive = await _commandMentionService.GetMentionAsync("archive");

        return MessageFactory.Text(
            $"The task \"{change.Task.Title}\" has been moved from **{change.From.ToFormattedString()}** to **{BoardStatus.Archived.ToFormattedString()}**. View it using {archive}.");
    }

    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("move", "Move a task from one column to another.")]
    public async Task<InteractionMessageProperties> MoveAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task,
        ColumnChoice to)
    {
        var change = await _taskService.MoveAsync(Context.Interaction.RequireGuildId(), task, to.ToBoardStatus());

        return MessageFactory.Text(
            $"The task \"{change.Task.Title}\" has been moved from **{change.From.ToFormattedString()}** to **{to.ToBoardStatus().ToFormattedString()}**.");
    }
}
```
**The two `<copy …>` markers are instructions to you, not code:** open `/home/jasper/Projects/KanbanCord/KanbanCord.Bot/Commands/Task/TaskCompleteCommand.cs`, copy its `[Description]` and message string verbatim (it mirrors `TaskStartCommand`: "…has been completed and moved to the **Completed** column. View it using {board}." is the expected shape — confirm against the file) and replace the markers; the build must not contain a literal `<copy` string. `using KanbanCord.Core.Models;` provides `ToFormattedString`.

- [ ] **Step 5: `TaskPriorityCommand.cs`**

```csharp
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("priority", "Set a priority for a task.")]
    public async Task<InteractionMessageProperties> PriorityAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task,
        PriorityChoice priority)
    {
        var change = await _taskService.SetPriorityAsync(Context.Interaction.RequireGuildId(), task, priority.ToPriority());

        return MessageFactory.Text(
            $"The priority for task \"{change.Task.Title}\" has been updated from **{change.From}** to **{priority.ToPriority()}**.");
    }
```
(wrapped in `public partial class TaskCommandGroup` with the same usings as `TaskStatusCommands.cs`).

- [ ] **Step 6: `TaskAssignCommand.cs`**

```csharp
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Providers;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskGroup;

public partial class TaskCommandGroup
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("assign", "Assign a task to a user.")]
    public async Task<InteractionMessageProperties> AssignAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task,
        [SlashCommandParameter(Description = "The user to assign this task to, leave empty to remove assignee")] User? assignee = null)
    {
        var change = await _taskService.AssignAsync(Context.Interaction.RequireGuildId(), task, assignee?.Id);

        if (assignee is null)
            return MessageFactory.Text($"The task \"{change.Task.Title}\" is no longer assigned to <@{change.PreviousAssigneeId}>.");

        var directMessageSent = await TrySendAssignmentDirectMessageAsync(assignee, change.Task.Title, change.Task.Description);

        var message = MessageFactory.Text($"The task \"{change.Task.Title}\" has been assigned to {assignee}.");

        // Ping in the channel only when the assignee could not be reached by DM (their DMs are closed)
        return directMessageSent ? message : message.WithAllowedUsers(assignee.Id);
    }

    private async Task<bool> TrySendAssignmentDirectMessageAsync(User assignee, string title, string description)
    {
        try
        {
            var channel = await assignee.GetDMChannelAsync();

            await channel.SendMessageAsync(new MessageProperties
            {
                Components =
                [
                    new ComponentContainerProperties(
                    [
                        new TextDisplayProperties($"### You have been assigned to a Task!\n-# {Context.Guild?.Name ?? "a server"}"),
                        new TextDisplayProperties($"**Task Name**\n{title}\n\n**Task Description**\n{description}"),
                    ]).WithAccentColor(Colors.Default),
                ],
                Flags = MessageFlags.IsComponentsV2,
            });

            return true;
        }
        catch (Exception)
        {
            // Closed DMs / blocked bot: caller falls back to a channel ping
            return false;
        }
    }
}
```
(`$"{assignee}"` renders a NetCord `User` as its mention; if it does not, use `<@{assignee.Id}>`.)

- [ ] **Step 7: Delete with confirmation** — `TaskDeleteCommand.cs`

```csharp
using KanbanCord.DiscordApplication.Components;
using KanbanCord.DiscordApplication.Presentation;
using KanbanCord.DiscordApplication.Providers;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands.TaskGroup;

public partial class TaskCommandGroup
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SubSlashCommand("delete", "Delete a task completely, this will skip the archive and never be accessible again.")]
    public async Task<InteractionMessageProperties> DeleteAsync(
        [SlashCommandParameter(Description = "Search for the task to select", AutocompleteProviderType = typeof(AllTasksAutocompleteProvider))] string task)
    {
        var item = await _taskService.GetAsync(Context.Interaction.RequireGuildId(), task);

        var names = await _userNameResolver.ResolveAsync([item.AuthorId]);

        return MessageFactory.Container(
        [
            new TextDisplayProperties("### Delete Task"),
            new TextDisplayProperties("Are you sure you want to delete this task?"),
            new TextDisplayProperties($"**Title:** {item.Title}\n**Description:** {item.Description}\n**Author:** <@{item.AuthorId}> ({names.GetValueOrDefault(item.AuthorId, BoardFormatter.UnknownUser)})"),
            new ActionRowProperties(
            [
                new ButtonProperties($"{InteractionIds.TaskDeleteConfirm}:{item.Id}", "Delete", ButtonStyle.Danger),
                new ButtonProperties(InteractionIds.TaskDeleteCancel, "Cancel", ButtonStyle.Secondary),
            ]),
        ]);
    }
}
```

- [ ] **Step 8: Button handlers** — `Components/TaskButtonHandlers.cs`

```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Attributes;
using KanbanCord.DiscordApplication.Presentation;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

public class TaskButtonHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskService _taskService;

    public TaskButtonHandlers(ITaskService taskService)
    {
        _taskService = taskService;
    }

    // Open to everyone, like the old "Add Comment" button on /task view
    [ComponentInteraction(InteractionIds.TaskCommentButton)]
    public async Task<InteractionCallbackProperties> CommentAsync(string taskId)
    {
        await _taskService.GetAsync(Context.Interaction.RequireGuildId(), taskId);

        return InteractionCallback.Modal(TaskModals.Comment(taskId));
    }

    [RequireInteractionOwner<ButtonInteractionContext>]
    [ComponentInteraction(InteractionIds.TaskDeleteConfirm)]
    public async Task<InteractionCallbackProperties> DeleteConfirmAsync(string taskId)
    {
        var task = await _taskService.DeleteAsync(Context.Interaction.RequireGuildId(), taskId);

        var message = MessageFactory.Text($"The task \"{task.Title}\" has been deleted.");

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }

    [RequireInteractionOwner<ButtonInteractionContext>]
    [ComponentInteraction(InteractionIds.TaskDeleteCancel)]
    public InteractionCallbackProperties DeleteCancel()
    {
        var message = MessageFactory.Text("Deletion cancelled.");

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
```
If a handler is synchronous and NetCord requires `Task`/`ValueTask` return types, make `DeleteCancel` return `Task<InteractionCallbackProperties>` via `Task.FromResult`.

- [ ] **Step 9: Build** — `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings; `grep -rn "<copy" KanbanCord.DiscordApplication` → no output.

- [ ] **Step 10: Manual check (dev guild)**

1. `/task add` → modal → submit → confirmation V2 message with clickable `/board` mention; `/board` lists it.
2. `/task edit` (prefilled modal) → submit; `/task comment` → submit with 9 chars (Discord blocks via min length) and 10 chars (accepted); **Add Comment** button on `/task view` works for a non-admin account.
3. `/task start|complete|archive|move|priority`; repeat `move` to the same column and `priority` to the same value → ephemeral error texts.
4. `/task assign` with DMs open → DM received, no channel ping; with the assignee's DMs closed (or a bot user) → channel ping fires; `/task assign` with no user → "no longer assigned"; again → "has no user assigned."
5. `/task delete` → another user clicks **Delete** → "This button is only available to the user who used the command."; the invoker clicks → deleted; clicking an old confirm again → "task not found" ephemeral.
6. A user without Manage Messages runs `/task add` → "You are missing the following permissions: `Manage Messages`".

- [ ] **Step 11: Commit**

```bash
git add KanbanCord.DiscordApplication
git commit -m "Add task mutation commands, modals and delete confirmation" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 12: `/clear` and `/reset` with stateless confirmations

**Files:**
- Create: `Commands/ClearCommand.cs`, `Commands/ResetCommand.cs`
- Create: `Components/BoardConfirmationHandlers.cs`

**Interfaces:**
- Consumes: `ITaskService.ClearBoardAsync`, `ResetBoardAsync`, `RequireInteractionOwnerAttribute`, `InteractionIds.ClearConfirm/ClearCancel/ResetConfirm/ResetCancel`.

- [ ] **Step 1: Commands**

`Commands/ClearCommand.cs`
```csharp
using KanbanCord.DiscordApplication.Components;
using KanbanCord.DiscordApplication.Presentation;
using NetCord;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;

namespace KanbanCord.DiscordApplication.Commands;

public class ClearCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    [RequireUserPermissions<ApplicationCommandContext>(Permissions.ManageMessages)]
    [SlashCommand("clear", "Clear the kanban board completely, this will archive all current tasks.", Contexts = [InteractionContextType.Guild])]
    public InteractionMessageProperties Clear() =>
        MessageFactory.Container(
        [
            new TextDisplayProperties("### Clear Board"),
            new TextDisplayProperties("Are you sure you want to clear the board?"),
            new ActionRowProperties(
            [
                new ButtonProperties(InteractionIds.ClearConfirm, "Clear", ButtonStyle.Danger),
                new ButtonProperties(InteractionIds.ClearCancel, "Cancel", ButtonStyle.Secondary),
            ]),
        ]);
}
```
`Commands/ResetCommand.cs`: same shape; `[SlashCommand("reset", "Resets the kanban board completely, this will delete all current and archived tasks.", …)]`, header `### Reset Board`, text "Are you sure you want to reset the board? This can not be undone!", buttons `ResetConfirm` ("Reset", Danger) and `ResetCancel`.

- [ ] **Step 2: Handlers** — `Components/BoardConfirmationHandlers.cs`

```csharp
using KanbanCord.Core.Services;
using KanbanCord.DiscordApplication.Attributes;
using KanbanCord.DiscordApplication.Presentation;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace KanbanCord.DiscordApplication.Components;

[RequireInteractionOwner<ButtonInteractionContext>]
public class BoardConfirmationHandlers : ComponentInteractionModule<ButtonInteractionContext>
{
    private readonly ITaskService _taskService;

    public BoardConfirmationHandlers(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [ComponentInteraction(InteractionIds.ClearConfirm)]
    public async Task<InteractionCallbackProperties> ClearAsync()
    {
        await _taskService.ClearBoardAsync(Context.Interaction.RequireGuildId());

        return Replace("The board has been cleared.");
    }

    [ComponentInteraction(InteractionIds.ResetConfirm)]
    public async Task<InteractionCallbackProperties> ResetAsync()
    {
        await _taskService.ResetBoardAsync(Context.Interaction.RequireGuildId());

        return Replace("The board has been reset.");
    }

    [ComponentInteraction(InteractionIds.ClearCancel)]
    public Task<InteractionCallbackProperties> CancelClearAsync() => Task.FromResult(Replace("Clearing cancelled."));

    [ComponentInteraction(InteractionIds.ResetCancel)]
    public Task<InteractionCallbackProperties> CancelResetAsync() => Task.FromResult(Replace("Reset cancelled."));

    private static InteractionCallbackProperties Replace(string text)
    {
        var message = MessageFactory.Text(text);

        return InteractionCallback.ModifyMessage(options => options.Components = message.Components);
    }
}
```
(The class-level precondition covers all four buttons: only the invoker can confirm or cancel.)

- [ ] **Step 3: Build** — `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln` → 0 warnings.

- [ ] **Step 4: Manual check (dev guild)** — `/clear` → another user clicks Clear → rejected; invoker clicks → every non-archived task moves to Archived (`/board` empty, `/archive` shows them); `/reset` → Cancel replaces the prompt; run again → Reset → `/archive` and `/board` empty; a second server's tasks (insert one manually with another `GuildId`) are untouched.

- [ ] **Step 5: Commit**

```bash
git add KanbanCord.DiscordApplication
git commit -m "Add clear and reset commands with stateless confirmations" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 13: Architecture test, parity sweep, remove the old bot, docs and policy checks

**Files:**
- Create: `KanbanCord.Tests/ArchitectureTests/NoRepositoryDependencyTests.cs`
- Delete: `KanbanCord.Bot/` (whole project), stray stubs (`KanbanCord.DscordApplication`, `KanbanCord.OldBot`) if still present
- Modify: `KanbanCord.sln`, `Directory.Packages.props`, `README.md`, `docker-compose.yml` (comment only), `.github/workflows/*.yml` (only if they reference `KanbanCord.Bot` or an old `dotnet-version`), `docs/superpowers/specs/2026-10-11-netcord-rewrite-design.md`

- [ ] **Step 1: Failing architecture test** (written first; it passes only if every earlier task obeyed the rule)

`KanbanCord.Tests/ArchitectureTests/NoRepositoryDependencyTests.cs`
```csharp
using System.Reflection;
using KanbanCord.Core.Repositories;

namespace KanbanCord.Tests.ArchitectureTests;

public class NoRepositoryDependencyTests
{
    [Fact]
    public void DiscordApplication_ShouldNotInjectRepositories()
    {
        // Arrange
        var assembly = typeof(KanbanCord.DiscordApplication.Extensions.ServiceCollectionExtensions).Assembly;
        Type[] forbidden = [typeof(ITaskItemRepository), typeof(ISettingsRepository)];

        // Act
        var offenders = assembly.GetTypes()
            .SelectMany(type => type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), (type, ctor) => (type, ctor))
            .Where(x => x.ctor.GetParameters().Any(p => forbidden.Contains(p.ParameterType)))
            .Select(x => x.type.FullName)
            .ToList();

        // Assert
        Assert.Empty(offenders);
    }

    [Fact]
    public void Core_ShouldNotReferenceNetCord()
    {
        // Arrange
        var references = typeof(KanbanCord.Core.Services.ITaskService).Assembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert
        Assert.DoesNotContain(references, name => name!.StartsWith("NetCord", StringComparison.Ordinal));
    }
}
```
Run: `dotnet test KanbanCord.Tests/KanbanCord.Tests.csproj --filter "FullyQualifiedName~ArchitectureTests"` → PASS expected. If it fails, fix the offending class by routing through a service (do not weaken the test).

- [ ] **Step 2: Parity sweep** — produce a table in the terminal by running:
```bash
grep -rhoE '\[(SubSlashCommand|SlashCommand)\("[a-z]+"' KanbanCord.DiscordApplication | sort
grep -rhoE '\[Command\("[a-z]+"' KanbanCord.Bot | sort
```
Expected: the new set contains `board archive clear reset help repository stats task` plus `add edit delete view start complete archive move assign me user priority comment`. Also confirm: 4 autocomplete providers (`ls KanbanCord.DiscordApplication/Providers`), 2 gateway handlers (`Events/`), the `ColumnChoice`/`PriorityChoice` enums. Any gap: implement it before continuing.

- [ ] **Step 3: Remove the old bot**

```bash
git rm -r KanbanCord.Bot
git rm -r --cached --ignore-unmatch KanbanCord.DscordApplication KanbanCord.OldBot
dotnet sln /home/jasper/Projects/KanbanCord/KanbanCord.sln remove KanbanCord.Bot/KanbanCord.Bot.csproj
```
(The stub directories may exist only on disk; delete them with `rm -r` only after confirming with `ls` that they contain nothing but the csproj stub.) Then in `Directory.Packages.props` remove `DSharpPlus`, `DSharpPlus.Commands`, `DSharpPlus.Interactivity`, `DSharpPlus.Natives.Zstd`, `J4asper.DSharpPlus.BetterPagination`. Check `grep -rn "KanbanCord.Bot\|DSharpPlus" --include=*.cs --include=*.csproj --include=*.props --include=*.yml --include=*.sln --include=Dockerfile .` returns nothing outside `docs/`.

- [ ] **Step 3b: CI** — read `.github/workflows/build-solution.yml` and `unit-tests.yml`; if `dotnet-version` is `9.x` (the WIP moved the solution to `net10.0`), change to `10.0.x`. Do not change anything else.

- [ ] **Step 4: Full verification**

Run: `dotnet build /home/jasper/Projects/KanbanCord/KanbanCord.sln -c Release --warnaserror` → 0 warnings, 0 errors.
Run: `dotnet test /home/jasper/Projects/KanbanCord/KanbanCord.sln -c Release` → all pass (repository tests included; if Mongo2Go cannot start in this environment, state that explicitly rather than claiming a pass).
Run: `docker build -t kanbancord:test /home/jasper/Projects/KanbanCord` → succeeds (Dockerfile already targets `KanbanCord.DiscordApplication`); then `docker run --rm -e Discord__Token=x -e Database__ConnectionString=mongodb://host.docker.internal:27017 kanbancord:test` starts and serves `/health` on :5000 inside the container (a failing Discord check is acceptable with a dummy token; a crash at start-up is not).

- [ ] **Step 5: Docs and config note**

README: find any mention of `Discord__SupportInvite` / `Discord:SupportInvite` (`grep -n SupportInvite README.md docker-compose.yml`) and replace with `KanbanCord__SupportInvite`; add one line under configuration: "`KanbanCord__SupportInvite` (optional) — support server invite shown by `/help` and in error messages. Renamed from `Discord__SupportInvite`." Leave `Discord__Token` as is. In the spec, change §5 "Permissions (parity)" to the verified behaviour (view/me/user open) and mark Status "implemented".

- [ ] **Step 6: Final manual parity pass (dev guild)** — run the checklists from Tasks 9, 11 and 12 once end to end against the final build, including: remove the bot from a second test guild and confirm its tasks/settings documents are deleted while a guild that merely went unavailable keeps data (simulate by checking logs for "became unavailable" never triggering deletion; do not force an outage).

- [ ] **Step 7: Policy checklist (organisation AI policy)** — before opening the PR: run Snyk Code (SAST), Snyk Open Source (licences) and the secrets check on the branch and attach results; open the PR with the **AI-assisted** label; request peer review; ensure the service tests are human-reviewed (they encode the behaviour contract); the PR description ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`. Add an `// AI-assisted (Claude)` comment on `BoardFormatter.FormatColumn` (the truncation budget logic is the one non-trivial algorithm).

- [ ] **Step 8: Commit**

```bash
git add -A KanbanCord.Tests KanbanCord.sln Directory.Packages.props README.md docker-compose.yml .github docs KanbanCord.DiscordApplication
git status --short   # review: only intended changes; the deletion of KanbanCord.Bot is staged
git commit -m "Remove DSharpPlus bot, add architecture tests and update docs" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage**

| Spec section | Task |
|---|---|
| §1 NetCord everywhere; services-only; V2 everywhere | 1–12; enforced by Task 13 architecture test and the V2 constraint in every view |
| §2 scaffold defects (Program.cs leftovers, duplicate registration, missing HttpClient, health check) | Task 1 |
| §3 services + errors + result handlers | Tasks 2–5 (services), 7 (handlers) |
| §4 interaction flows (add/comment/edit modals, reset/clear/delete confirmations, board refresh) | Tasks 9, 11, 12 |
| §5 parity checklist (commands, providers, handlers, permissions, mentions, presentation notes) | Tasks 9–12, verified by Task 13 Step 2 |
| §6 host/DI/health/packages | Tasks 1, 7, 13 |
| §7 testing (service tests, architecture test, manual checklist) | Tasks 2–6, 13 |
| Added request: unit tests for services and repositories, BumpBuddy-inspired | Factories (Task 2), service tests (Tasks 2–5), repository edge cases + Mongo-backed service test (Task 5a); existing repository tests (all 12 methods) kept |
| §8 out of scope | respected (no migration, no new commands; refresh button is in the spec) |
| §9 governance | Global Constraints + Task 13 Step 7 |
| §10 open items (customId syntax, ping in TextDisplay, view permissions, name resolution) | `customId` colon params: Tasks 9–12 use BumpBuddy's proven `name:param` form; ping: Task 11 Step 6/10 (manual); view permissions: resolved (open); name resolution: `UserNameResolver` with REST + cache |

**Placeholder scan:** the only intentional non-code markers are the two `<copy …>` instructions in Task 11 Step 4 for `TaskCompleteCommand` (the file was not read while planning); the step states exactly where to copy from and that the build must not contain them. No TBD/TODO elsewhere.

**Type consistency:** `StatusChange(Task, From)`, `PriorityChange(Task, From)`, `AssignmentChange(Task, PreviousAssigneeId)`, `BoardEntry(Number, Task)`, `BoardColumn(Status, Entries)` are defined in Tasks 3–4 and used with the same members in Tasks 9–11; `InteractionIds` constants are defined in Task 7 and referenced by name afterwards; `ITaskService` method names (`GetAsync`, `AddAsync`, `EditAsync`, `DeleteAsync`, `MoveAsync`, `StartAsync`, `CompleteAsync`, `ArchiveAsync`, `SetPriorityAsync`, `AssignAsync`, `AddCommentAsync`, `ClearBoardAsync`, `ResetBoardAsync`) match across tasks and tests; `BoardScope.Parse/ToScope`, `BoardView.Build(columns, authorName, title, scope, subtitle, refreshable)` match their callers.

**Known risk to watch during execution:** NetCord beta.28 API shapes I could not compile-check while planning (`AllowedMentionsProperties.AllowedUsers`, `TextInputProperties.WithMinLength/WithMaxLength`, slash commands returning `InteractionCallbackProperties` for modals, `ShardedGatewayClient` enumeration/`Id`, `GetCurrentApplicationAsync` parameters, `Timestamp` constructor). Each is called out at its step; the fix is always "match BumpBuddy's usage" and never a warning suppression.
