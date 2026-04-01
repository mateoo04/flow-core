using FlowCore.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
Console.WriteLine("Starting application... is development: " + app.Environment.IsDevelopment());
if (app.Environment.IsDevelopment())
{
    var demo = DemoDataBuilder.CreateSampleGraph();

    // Summary counts (same SelectMany chain you will reuse for reports).
    var projectCount = demo.Workspaces.Sum(w => w.Projects.Count);
    var taskCount = DemoDataLinqExamples.AllTasks(demo.Workspaces).Count();

    Console.WriteLine(
        $"[DemoData] Workspaces={demo.Workspaces.Count}, Projects={projectCount}, " +
        $"TaskItems={taskCount}, Users={demo.Users.Count}, Tags={demo.Tags.Count}");

    // Smislene LINQ upite nad istim grafom — kasnije IQueryable/EF Core umjesto IEnumerable.
    DemoDataLinqExamples.WriteDevelopmentQuerySample(demo);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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