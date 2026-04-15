using FlowCore.Data;
using FlowCore.Repositories;
using FlowCore.Repositories.InMemory;
using FlowCore.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<DemoDataGraph>(_ => DemoDataBuilder.CreateSampleGraph());
builder.Services.AddSingleton(sp => new InMemoryDataStore(sp.GetRequiredService<DemoDataGraph>()));
builder.Services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
builder.Services.AddSingleton<IProjectRepository, InMemoryProjectRepository>();
builder.Services.AddSingleton<ITaskRepository, InMemoryTaskRepository>();
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ITagRepository, InMemoryTagRepository>();
builder.Services.AddSingleton<IBoardRepository, InMemoryBoardRepository>();
builder.Services.AddSingleton<ICommentRepository, InMemoryCommentRepository>();
builder.Services.AddSingleton<IBreadcrumbTrailBuilder, BreadcrumbTrailBuilder>();
builder.Services.AddSingleton<UiText>();

var app = builder.Build();
Console.WriteLine("Starting application... is development: " + app.Environment.IsDevelopment());
if (app.Environment.IsDevelopment())
{
    var store = app.Services.GetRequiredService<InMemoryDataStore>();

    var projectCount = store.Workspaces.Sum(w => w.Projects.Count);
    var taskCount = DemoDataLinqExamples.AllTasks(store.Workspaces).Count();

    Console.WriteLine(
        $"[DemoData] Organizations={store.Workspaces.Count}, Projects={projectCount}, " +
        $"TaskItems={taskCount}, Users={store.Users.Count}, Tags={store.Tags.Count}");

    DemoDataLinqExamples.WriteDevelopmentQuerySample(store.Workspaces, store.Users);
}

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
