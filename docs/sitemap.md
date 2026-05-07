# FlowCore — Sitemap

Every URL exposed by the app, with the controller, action, and view that handle it. Two routing styles are in play: ASP.NET MVC's **default conventional route** (`{controller}/{action}/{id?}`) for index/list pages, and **attribute routes** with `:guid` constraints for the resource-shaped URLs (workspaces, projects, tasks, settings). All URLs are emitted lowercase via `RouteOptions.LowercaseUrls = true` in [`Program.cs`](../FlowCore/Program.cs); route matching is case-insensitive, so legacy `/Workspaces` style URLs still resolve.

---

## Home

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/` | `Index` | `Views/Home/Index.cshtml` | (default route) — landing page, "My work" view |
| GET | `/home/error` | `Error` | `Views/Shared/Error.cshtml` | (default route) — wired in non-dev via `UseExceptionHandler("/Home/Error")` |

## Workspaces

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/workspaces` | `Index` | `Views/Workspaces/Index.cshtml` | (default route) |
| GET | `/workspaces/{id:guid}` | `Details` | `Views/Workspaces/Details.cshtml` | route name `workspace-details` |

## Projects

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/projects` | `Index` | `Views/Projects/Index.cshtml` | (default route); accepts optional `?workspaceId={guid}` filter |
| GET | `/projects/create` | `Create` | `Views/Projects/Create.cshtml` | (default route); accepts optional `?workspaceId={guid}` |
| POST | `/projects/create` | `Create` | `Views/Projects/Create.cshtml` *(on validation error)* | (default route); success → redirect to `Details` |
| GET | `/projects/{id:guid}` | `Details` | `Views/Projects/Details.cshtml` | route name `project-details` |
| GET | `/projects/{id:guid}/boards/{boardId:guid}` | `Details` | `Views/Projects/Details.cshtml` | route name `project-board-details` — same action, hierarchy URL |
| POST | `/projects/delete/{id}` | `Delete` | — | (default route); redirects to `Index` |

## Boards

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/boards` | `Index` | `Views/Boards/Index.cshtml` | (default route) |
| GET | `/boards/details/{id}` | `Details` | — | (default route); redirects to `Projects.Details` with `boardId` — boards have no standalone view |

## Tasks

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/tasks` | `Index` | `Views/Tasks/Index.cshtml` | (default route) |
| GET | `/projects/{projectId:guid}/tasks/new` | `Create` | `Views/Tasks/Create.cshtml` | route name `task-create-form`; accepts optional `?boardId={guid}&parentTaskItemId={guid}` |
| POST | `/projects/{projectId:guid}/tasks` | `Create` | `Views/Tasks/Create.cshtml` *(on validation error)* | success → redirect to `Details` |
| GET | `/tasks/{id:guid}` | `Details` | `Views/Tasks/Details.cshtml` | route name `task-details` |
| POST | `/tasks/{id:guid}/comments` | `AddComment` | — | redirects to `Details` |
| POST | `/tasks/{id:guid}/delete` | `Delete` | — | redirects to `Projects.Details` (or `Index` as fallback) |

## Comments

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/comments` | `Index` | `Views/Comments/Index.cshtml` | (default route) |
| GET | `/comments/details/{id}` | `Details` | `Views/Comments/Details.cshtml` | (default route) |

## Tags

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/tags` | `Index` | `Views/Tags/Index.cshtml` | (default route) |
| GET | `/tags/details/{id}` | `Details` | `Views/Tags/Details.cshtml` | (default route) |

## Users

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/users` | `Index` | `Views/Users/Index.cshtml` | (default route) |
| GET | `/users/details/{id}` | `Details` | `Views/Users/Details.cshtml` | (default route) |

## Statuses

Read-only admin overview of every `TaskStatusDefinition` across all workspaces, grouped by workspace. To edit, use the per-workspace Settings page.

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/statuses` | `Index` | `Views/Statuses/Index.cshtml` | (default route) |

## Settings

All actions live under the workspace prefix `/workspaces/{workspaceId:guid}/settings`, declared via a controller-level `[Route]`. Settings are workspace-scoped — there is no global settings page.

| HTTP | URL | Action | View | Notes |
|---|---|---|---|---|
| GET | `/workspaces/{workspaceId:guid}/settings` | `Index` | `Views/Settings/Index.cshtml` | route name `workspace-settings` |
| POST | `/workspaces/{workspaceId:guid}/settings/statuses` | `Create` | — | adds a new `TaskStatusDefinition`; redirects to `Index` |
| POST | `/workspaces/{workspaceId:guid}/settings/statuses/{id:guid}` | `Update` | — | edits an existing status; redirects to `Index` |
| POST | `/workspaces/{workspaceId:guid}/settings/statuses/{id:guid}/reorder` | `Reorder` | — | swaps a status with its neighbour (`?direction=-1` or `1`); redirects to `Index` |
| POST | `/workspaces/{workspaceId:guid}/settings/statuses/{id:guid}/delete` | `Delete` | — | deletes a status; rejects deletion if any `TaskItem` references it; redirects to `Index` |

---

## Shared partials

| Partial | Used by | Purpose |
|---|---|---|
| `Views/Shared/_Layout.cshtml` | All pages (via `_ViewStart`) | Sidebar, header, breadcrumb slot |
| `Views/Shared/_Breadcrumbs.cshtml` | `_Layout` | Renders `ViewBag.Breadcrumbs` (built by [`BreadcrumbTrailBuilder`](../FlowCore/Services/BreadcrumbTrailBuilder.cs) via `LinkGenerator`) |
| `Views/Shared/_BoardKanban.cshtml` | `Views/Projects/Details.cshtml` | The column-grouped task board on the project detail page |
| `Views/Shared/_AssigneeAvatarStack.cshtml` | `Views/Home/Index.cshtml`, `Views/Shared/_BoardKanban.cshtml` | Stacked avatars for task assignees |

Framework-only partials (`_ViewImports`, `_ViewStart`, `_ValidationScriptsPartial`) are intentionally omitted — they're plumbing.

---

## Routing conventions

- **Lowercase URL emission** is enabled globally via `RouteOptions.LowercaseUrls` and `LowercaseQueryStrings` in [`Program.cs`](../FlowCore/Program.cs). Tag helpers (`asp-controller` / `asp-action`), redirects, and `LinkGenerator` all emit lowercase. Matching is case-insensitive — `/Workspaces` still resolves.
- **`:guid` constraints** on attribute routes reject malformed IDs at the routing layer; controllers never see them. `/tasks/not-a-guid` returns 404 before any code in `TasksController` runs.
- **POST verbs in URLs** (`POST /tasks/{id}/comments`, `POST /tasks/{id}/delete`) are deliberate. HTML forms only support `GET` and `POST`, so a true REST verb (`DELETE /tasks/{id}`) would require JS or hidden `_method` shims. The verb-in-path pattern is the right call for a server-rendered Razor app.
- **Named routes** (`workspace-details`, `project-details`, `project-board-details`, `task-details`, `task-create-form`, `workspace-settings`) are referenced by [`BreadcrumbTrailBuilder`](../FlowCore/Services/BreadcrumbTrailBuilder.cs) through `LinkGenerator.GetPathByAction`, so renaming a URL template never breaks the breadcrumbs.
- **Default route** (`{controller=Home}/{action=Index}/{id?}`) remains registered and handles all index/list pages plus the few admin-flavoured actions (`Projects.Delete`, `Boards.Details`) that don't merit custom routes.
