# [FlowCore](https://flow-core.up.railway.app)

FlowCore is a backend-focused full-stack project management app for organizing work across **workspaces, projects, boards, and tasks**. It combines authentication, workspace-level authorization, EF Core data modeling, and a Tailwind MVC UI in one complete app.

**Tech stack:** **ASP.NET Core MVC (.NET 10)**, **EF Core 10**, **PostgreSQL 18**, **Tailwind 4**.

## Technical focus

- Built with a clean backend architecture: **Controller -> Service -> Repository -> DbContext**.
- Uses **ASP.NET Core Identity** with cookie auth, login rate limiting, and workspace member/owner policies.
- Uses production-minded patterns: **`Result<T>` flow**, explicit cascade rules, and async-first data access.
- Includes a Tailwind UI and realistic seeded data so reviewers can evaluate full user flows quickly.

## Screenshots

**My tasks dashboard** - aggregated task board across multiple projects.
![My tasks dashboard](screenshots/my_tasks_dashboard.png)

**Project board** - kanban-style project view with task status columns.
![Project board](screenshots/project_board.png)

**Task details** - task metadata, assignees, subtasks, and comments.
![Task details](screenshots/task_details.png)

**Edit task form** - task editing flow with priority, due date, and assignee fields.
![Edit task form](screenshots/edit_task.png)

## Run locally

### Prerequisites

- **.NET 10 SDK**
- **Node.js + npm** (needed for Tailwind CSS build)
- **Docker** (or a local PostgreSQL instance)

### 1) Start PostgreSQL (Docker)

```bash
docker run -d --name flowcore-pg -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=FlowCore postgres:18
```

### 2) Install frontend dependencies

```bash
npm install
```

### 3) Apply migrations

```bash
dotnet ef database update --project FlowCore --startup-project FlowCore --context FlowCoreDbContext
```

### 4) Run the app

```bash
dotnet run --project FlowCore
```

Open the local URL printed by Kestrel.

## Tests

The regular xUnit suite in `FlowCore.Tests/Api` covers every API endpoint. See
[`docs/api-test-coverage.md`](docs/api-test-coverage.md) for the endpoint matrix.

The Playwright suite is a separate browser-based UI scenario with eleven visible
user steps: demo sign-in, workspace and project navigation, task creation,
assignment via autocomplete, comment creation, editing, and deletion. It uses
browser clicks and form interactions only; it does not call API endpoints directly.

With PostgreSQL running, execute:

```bash
npm install
npm run test:api
npm run test:e2e
```

By default, Playwright starts FlowCore at `http://127.0.0.1:5055`. To run against
an already running instance, set `PLAYWRIGHT_BASE_URL` to its URL before executing
the command. The suite uses the development-only **Explore demo workspace** login,
so do not point it at a production environment.

## Demo accounts (seeded)

On first run in `Development`, the app seeds demo users/projects/tasks.

- Emails: `alex@flowcore.demo`, `sam@flowcore.demo`, `casey@flowcore.demo`, `jordan@flowcore.demo`, `morgan@flowcore.demo`
- Password: `Seed__SharedPassword` if configured, otherwise local fallback `Admin6060!`

## Code highlights

If someone wants to inspect code quality quickly:

- [`FlowCore/Data/FlowCoreDbContext.cs`](FlowCore/Data/FlowCoreDbContext.cs)
- [`FlowCore/Common/Result.cs`](FlowCore/Common/Result.cs)
- [`FlowCore/Services/Domain/TaskService.cs`](FlowCore/Services/Domain/TaskService.cs)
- [`FlowCore/Repositories/EntityFramework/EfTaskRepository.cs`](FlowCore/Repositories/EntityFramework/EfTaskRepository.cs)
