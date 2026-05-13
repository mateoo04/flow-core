# FlowCore

Project-management app modelled on a workspace → project → board → task hierarchy. Built for an ASP.NET Core MVC course; structured to look and behave like a real production codebase rather than the usual lab template.

**.NET 10 · EF Core 10 · PostgreSQL 18 · Tailwind 4 · async/await throughout**

## Quick start

```bash
docker run -d --name flowcore-pg -p 5432:5432 \
  -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=FlowCore postgres:18

dotnet ef database update \
  --project FlowCore --startup-project FlowCore --context FlowCoreDbContext

dotnet run --project FlowCore
```

The first run in `Development` auto-seeds ~60 demo tasks across 5 projects (see [`DatabaseSeeder.cs`](FlowCore/Data/DatabaseSeeder.cs); guarded by an `Any()` check so subsequent runs preserve your data). Open the URL Kestrel prints.

## Production notes (Railway)

- The app now fails build/publish if Tailwind output cannot be generated (`npm run build:css`), so make sure Node/npm is available in your Railway build image.
- Forwarded proxy headers (`X-Forwarded-For`, `X-Forwarded-Proto`) are enabled in startup for TLS-terminating platforms.
- Demo login is **disabled outside Development by default**. Enable it explicitly with `Features__EnableDemoLogin=true` only for public demo environments.

## Development credentials

The dev seed creates five demo users. All share the password `Admin6060!` — **lab-only, never use in production.**

| Email | Role in seed |
|---|---|
| `alex@flowcore.demo` | Owner of `WorkspaceNorth` |
| `sam@flowcore.demo` | Member |
| `casey@flowcore.demo` | Member |
| `jordan@flowcore.demo` | Member |
| `morgan@flowcore.demo` | Member |

You can also sign up for an account at `/account/register`.

## Architecture

```
View → Controller → Service (validation, Result<T>) → Repository → FlowCoreDbContext → PostgreSQL
```

- **Repositories** read with `AsNoTracking`; heavy `GetById` queries use `AsSplitQuery`. Cascade-delete handled by Postgres, not by recursive code.
- **Domain services** ([`Services/Domain/`](FlowCore/Services/Domain/)) own validation and return [`Result<T>`](FlowCore/Common/Result.cs) so controllers map success / `Validation` / `NotFound` / `Conflict` without exception-driven control flow.
- **Attribute routing** with `:guid` constraints for resource URLs; lowercase URL emission via `RouteOptions.LowercaseUrls`. Named routes referenced through `LinkGenerator.GetPathByName` for refactor-safe URL generation in [`BreadcrumbTrailBuilder`](FlowCore/Services/BreadcrumbTrailBuilder.cs).
- **Cascade behaviour** is configured explicitly in [`OnModelCreating`](FlowCore/Data/FlowCoreDbContext.cs); `Comment.Author` and `TaskItem.TaskStatusDefinition` use `Restrict` (rationale in the model doc).

## Documentation

- [docs/semantic-model.md](docs/semantic-model.md) — entities + Mermaid ER diagram + cascade table
- [docs/sitemap.md](docs/sitemap.md) — every URL → controller / action / view
- [.claude/skills/](.claude/skills/) — agent skills for the EF and list-page workflows (auto-discovered by Claude Code)

## Highlights for code review

If you're evaluating engineering signal, start with these:
[`FlowCoreDbContext.cs`](FlowCore/Data/FlowCoreDbContext.cs) (Fluent API),
[`Result.cs`](FlowCore/Common/Result.cs),
[`TaskService.cs`](FlowCore/Services/Domain/TaskService.cs),
[`EfTaskRepository.cs`](FlowCore/Repositories/EntityFramework/EfTaskRepository.cs),
[`BreadcrumbTrailBuilder.cs`](FlowCore/Services/BreadcrumbTrailBuilder.cs).
