# KanbanCord NetCord Rewrite — Design

Date: 2026-10-11 · Branch: `feature/netcord-rewrite` · Status: implemented (manual Discord checks pending)

## 1. Goal

Rewrite the KanbanCord Discord bot from DSharpPlus to [NetCord](https://netcord.dev/), using the BumpBuddy
solution (`/home/jasper/Projects/BumpBuddy/DiscordApplications/BumpBuddy.DiscordApp.BumpBuddy`) as the structural
reference, **without** a dedicated API: one process, direct MongoDB access.

Hard requirements (from the request):

1. NetCord replaces DSharpPlus everywhere.
2. Commands, component handlers, gateway handlers and autocomplete providers call **services**, never repositories.
3. All bot messages use **Components V2** (no `content`, no embeds).

## 2. Current state

| Project | Role |
|---|---|
| `KanbanCord.Bot` | Old DSharpPlus bot. Behavioural reference only. Not modified. Deleted at parity. |
| `KanbanCord.DiscordApplication` | New NetCord bot. Scaffolded (csproj, DI, health checks, background services, `Program.cs`); no commands, handlers or services yet. |
| `KanbanCord.Core` | Models, repositories, options. Reused. Gains `Services/`. |
| `KanbanCord.Tests` | Repository tests only (xUnit, Moq, Mongo2Go). Gains service tests. |
| `KanbanCord.DscordApplication`, `KanbanCord.OldBot` | Stray csproj stubs. Deleted. |

Known defects in the scaffold, fixed as part of this work:

- `Program.cs` uses `AddServiceDefaults`, `MapDefaultEndpoints`, OpenAPI, Scalar and controllers — BumpBuddy
  leftovers with no counterpart in this solution.
- `AddBackgroundServices()` registers `UptimeMonitorBackgroundService` twice; `DatabaseSetupBackgroundService`
  is never registered.
- `AddHttpClient()` is never registered, but the uptime monitor injects `IHttpClientFactory`.
- `DiscordConnectivityHealthCheck` still references the DSharpPlus `DiscordClient`.

## 3. Architecture

```
KanbanCord.Core                       (no NetCord reference)
  Services/        ITaskService, ISettingsService, IBoardService + implementations
  Models/          existing; plus result/view types where needed
  Exceptions/      KanbanCordException (user-facing failure)
  Repositories/    unchanged; called only by services
KanbanCord.DiscordApplication
  Commands/        ApplicationCommandModule classes: parse input -> call service -> render
  Components/      button + modal handlers (stateless, customId-encoded)
  Events/          gateway handlers (guild join/leave)
  Presentation/    V2 message builders (board, task, confirmation, error)
  Providers/       autocomplete (IAutocompleteProvider)
  Services/        ICommandMentionService (Discord-aware, lives here, not in Core)
KanbanCord.Tests                      service tests
```

**Dependency rule.** Nothing in `KanbanCord.DiscordApplication` may reference `ITaskItemRepository` or
`ISettingsRepository`. Enforced by a test that scans the assembly for constructor parameters of those types.

**Approach.** Services return plain models/results (approach A). They never build Discord messages, so Core stays
free of NetCord and services are testable without Discord. `Presentation/` converts models to
`InteractionMessageProperties`.

### Services

| Service | Responsibility (replaces direct repo calls in…) |
|---|---|
| `ITaskService` | Get, add, edit, delete, move, start, complete, archive, set priority, assign/unassign, add comment, plus `ClearBoardAsync` (archive every non-archived task) and `ResetBoardAsync` (delete every task of the guild). Takes the task id as the string that crosses the Discord boundary; a malformed id, a missing task and a task of another guild all throw `KanbanCordException("The selected task was not found, please try again.")`. Validates title/description/comment bounds and sets `LastUpdatedAt` from an injected `TimeProvider`. |
| `IBoardService` | `GetColumnsAsync(guildId, statuses, assigneeId?)`: one column per requested status in order, entries numbered per column over all tasks of that status (so numbers stay stable when filtering by assignee). Also feeds autocomplete (labels `[Column] n - Title`). Returns data only; author names are resolved by `Presentation` through `IUserNameResolver`. |
| `ISettingsService` | `RemoveGuildDataAsync(guildId)`: resets the guild's tasks (via `ITaskService`) and removes its settings document. Nothing else touches settings today. |

Task identifiers cross the Discord boundary as the `ObjectId` string (autocomplete value, customId parameter);
services accept `ObjectId`; parsing and invalid-id handling happens once in a shared parameter/helper, producing the
same "task not found" error.

### Errors

Services throw `KanbanCordException` for user-facing failures. A central `ApplicationCommandResultHandler<TContext>`
and `ComponentInteractionResultHandler<TContext>` (modelled on BumpBuddy's) render it as an ephemeral V2 message.
Other exceptions are logged and rendered as a generic ephemeral error with the support-invite link button
(`KanbanCordOptions.SupportInvite`, when configured). Missing-permission failures render the permission list.
Commands therefore contain no per-command "not found" branches.

## 4. Interaction flows (replacing `WaitForModal` / `WaitForButton`)

NetCord has no awaiting helpers. Every multi-step flow is two stateless handlers; context travels in the `customId`
using NetCord's colon-separated parameters (verify the exact parameter syntax against the pinned NetCord version
while implementing).

| Flow | Step 1 | Step 2 |
|---|---|---|
| `/task add` | Respond with `InteractionCallback.Modal`, id `task-add` | Modal handler `task-add`: read title/description, `ITaskService.AddAsync`, confirmation message |
| Comment button | Button `task-comment:{taskId}` -> modal | Modal handler `task-comment:{taskId}`: `AddCommentAsync` |
| `/reset`, `/clear` | V2 confirmation with buttons `reset-confirm:{userId}` / `reset-cancel:{userId}` | Handler checks clicker == `userId` (precondition modelled on `RequireInteractionOwner`), calls the service, updates the message in place |
| Board refresh | Button `board-refresh:{status}` | Re-read via `IBoardService`, update message; keep the "Board already up to date" ephemeral reply |

Behaviour change: the old 5-minute button timeout (which disabled the button) is dropped. A stale button after a
restart or long delay is handled by the handler (e.g. "This confirmation has expired" if the board changed or the
user does not match). Documented here because it is user-visible.

Limits from `Limits.cs` (title 40, description 600, comment 10–600) apply to modal text inputs unchanged.

## 5. Commands, providers, events — parity checklist

Every item below must exist in the new project before `KanbanCord.Bot` is deleted.

**Top-level:** `/board [column]`, `/archive`, `/clear`, `/reset`, `/help`, `/repository`, `/stats`.

**`/task` group:** `add`, `edit`, `delete`, `view`, `start`, `complete`, `move`, `priority`, `assign`, `comment`,
`archive`, `user`, `me`.

**Providers:** all-tasks, backlog, in-progress, completed autocomplete -> `IAutocompleteProvider`s calling
`ITaskService.ListByStatusAsync` (one service call, no fetch-all-then-filter per provider; max 20 results,
case-insensitive contains, same `[Column] n - Title` label format). Column and priority choices become enum-backed
choices.

**Gateway handlers:** `GuildCreate` (log), `GuildDelete` (skip unavailable; `ISettingsService.RemoveGuildDataAsync`;
log). `GuildDownloadCompleted` handler is dropped (no NetCord equivalent need).

**Permissions (parity).** Old behaviour: the `/task` group requires Manage Messages, except `me` and `user`;
`/clear` and `/reset` require Manage Messages; everything else is open. NetCord's group-level
`DefaultGuildPermissions` cannot exempt individual subcommands, so permissions are enforced with a runtime
user-permission precondition per subcommand (and `DefaultGuildPermissions` only where every subcommand shares it).
The old `/task view`, `/task me` and `/task user` explicitly override the group requirement and are open to everyone; the "Add Comment" button on `/task view` is also open.

**Command mentions.** `ICommandMentionService` caches global command ids once (on ready / first use) and produces
`</task view:id>` mentions; replaces `CommandHelper` and per-response `GetGlobalApplicationCommandsAsync` calls.
`/help` builds its list from the same cache.

**Presentation notes (V2).**

- Accent colour: reuse the old default embed colour (`WithDefaultColor`) as the container accent.
- Board columns render as `TextDisplay` blocks in a `ComponentContainer`; confirmations/errors are small containers.
- V2 messages cannot carry `content`: the `/task assign` ping goes inside a `TextDisplay` with explicit
  `AllowedMentions` for the assignee. Verify the ping fires during implementation; keep the DM-to-assignee attempt
  and its fallback.
- Ephemeral flag is combined with `IsComponentsV2` for errors and "already up to date".
- Author usernames come from the gateway user cache with a REST fallback.

## 6. Host and DI

- Keep `WebApplication` (Dockerfile `HEALTHCHECK` calls `http://localhost:5000/health`), trimmed to
  `MapHealthChecks("/health")` plus `app.AddModules(...)`. Remove service defaults, OpenAPI, Scalar, controllers and
  their package references.
- Register: options (database, uptime monitor, KanbanCord), repositories, services (scoped), background services
  (database setup, uptime monitor — each once), `AddHttpClient()`, sharded gateway with `GatewayIntents.Guilds` and the
  existing presence ("Watching out for work"), application commands with the custom result handler, component
  interactions, gateway handlers.
- `DiscordConnectivityHealthCheck` is rewritten for NetCord (shards connected; REST call as fallback).
- Package versions via `Directory.Packages.props`; remove DSharpPlus and `J4asper.DSharpPlus.BetterPagination`
  once no project references them. `/help` pagination becomes a V2 message (no pagination library).

## 7. Testing

- Test data comes from `KanbanCord.Tests/Factories`. Repository tests also cover persistence edge cases the services rely on (id generation, full-document round trip, insertion order, no upsert on update, guild isolation), and `TaskServiceMongoTests` runs the real services over Mongo2Go.
- `KanbanCord.Tests/ServiceTests/`: xUnit with an in-memory repository fake (and Moq where interaction verification matters) over the repositories; Arrange/Act/Assert; cover not-found and
  invalid-transition errors, `LastUpdatedAt` updates, board grouping/numbering/filtering, guild data removal.
- Existing repository tests are kept and must stay green.
- Architecture test: no DiscordApplication type depends on a repository interface.
- Presentation and interaction handlers are verified manually against a development guild (V2 trees are not
  asserted in unit tests). Manual checklist = the parity checklist in section 5.

## 8. Out of scope

- No data migration: Mongo models and stored formats are unchanged.
- No new commands or features; command names, `/task` structure, limits and options are unchanged.
- No API, messaging (Rebus), Aspire or telemetry infrastructure from BumpBuddy.

## 9. Governance (organisation AI policy)

AI-generated code is peer-reviewed; unit tests for service logic are human-verified; the code is scanned with Snyk
(secrets, SAST, licence compliance) before merge; the PR carries the **AI-assisted** label; code sections where AI
solved a non-trivial problem are commented with the tool used.

## 10. Open items to confirm during implementation

- Exact NetCord `customId` parameter syntax and modal-component API in the pinned beta version.
- Whether `AllowedMentions` pings from inside a V2 `TextDisplay`.
- Gateway cache vs REST fallback for resolving author usernames of users not in cache.
