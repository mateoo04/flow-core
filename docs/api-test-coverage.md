# API test coverage

The regular integration tests live in `FlowCore.Tests/Api` and use the ASP.NET
Core test host with isolated in-memory data. Every CRUD API resource has a test
for its collection read, item read, create, update, and delete endpoint.

| Resource | Test class | Endpoints covered |
| --- | --- | --- |
| Workspaces | `WorkspacesApiTests` | `GET /api/workspaces`, `GET /api/workspaces/{id}`, `POST`, `PUT`, `DELETE` |
| Projects | `ProjectsApiTests` | `GET /api/projects`, `GET /api/projects/{id}`, `POST`, `PUT`, `DELETE` |
| Boards | `BoardsApiTests` | `GET /api/boards`, `GET /api/boards/{id}`, `POST`, `PUT`, `DELETE` |
| Statuses | `StatusesApiTests` | `GET /api/statuses`, `GET /api/statuses/{id}`, `POST`, `PUT`, `DELETE` |
| Tags | `TagsApiTests` | `GET /api/tags`, `GET /api/tags/{id}`, `POST`, `PUT`, `DELETE` |
| Tasks | `TasksApiTests` | `GET /api/tasks`, `GET /api/tasks/{id}`, `POST`, `PUT`, `DELETE` |
| Comments | `CommentsApiTests` | `GET /api/comments`, `GET /api/comments/{id}`, `POST`, `PUT`, `DELETE` |

That is 35 explicitly tested CRUD API endpoints. The same test classes also cover
important negative behaviour such as unauthenticated `401`, non-member `403`,
invalid request `400`, missing resource `404`, collection filters, `201 Created`
location headers, and selected domain validation rules.
