using FlowCore.Data;
using FlowCore.Repositories;
using FlowCore.Repositories.InMemory;
using FlowCore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<DemoDataGraph>(_ => DemoDataBuilder.CreateSampleGraph());
builder.Services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ITagRepository, InMemoryTagRepository>();
builder.Services.AddSingleton<IBoardRepository, InMemoryBoardRepository>();
builder.Services.AddSingleton<IBoardColumnRepository, InMemoryBoardColumnRepository>();
builder.Services.AddSingleton<ITaskStatusDefinitionRepository, InMemoryTaskStatusDefinitionRepository>();
builder.Services.AddSingleton<ICommentRepository, InMemoryCommentRepository>();
builder.Services.AddSingleton<IBreadcrumbTrailBuilder, BreadcrumbTrailBuilder>();

var app = builder.Build();
Console.WriteLine("Starting application... is development: " + app.Environment.IsDevelopment());
if (app.Environment.IsDevelopment())
{
    var demo = app.Services.GetRequiredService<DemoDataGraph>();

    var projectCount = demo.Workspaces.Sum(w => w.Projects.Count);
    var taskCount = DemoDataLinqExamples.AllTasks(demo.Workspaces).Count();

    Console.WriteLine(
        $"[DemoData] Workspaces={demo.Workspaces.Count}, Projects={projectCount}, " +
        $"TaskItems={taskCount}, Users={demo.Users.Count}, Tags={demo.Tags.Count}");

    DemoDataLinqExamples.WriteDevelopmentQuerySample(demo);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
