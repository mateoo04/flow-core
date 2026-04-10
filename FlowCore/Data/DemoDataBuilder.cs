using FlowCore.Models;

namespace FlowCore.Data;

public sealed class DemoDataGraph
{
    public IReadOnlyList<Workspace> Workspaces { get; init; } = Array.Empty<Workspace>();
    public IReadOnlyList<User> Users { get; init; } = Array.Empty<User>();
    public IReadOnlyList<Tag> Tags { get; init; } = Array.Empty<Tag>();
}

public static class DemoDataBuilder
{
    private readonly record struct Team(User Alex, User Sam, User Casey, User Jordan, User Morgan);

    public static DemoDataGraph CreateSampleGraph()
    {
        var now = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid Ng() => Guid.NewGuid();

        var ownerAlex = new User
        {
            Id = DemoSeedIds.UserAlex,
            FullName = "Alex Owner",
            Email = "alex@flowcore.demo",
            JoinedAt = now.AddMonths(-6),
            IsActive = true
        };

        var memberSam = new User
        {
            Id = DemoSeedIds.UserSam,
            FullName = "Sam Member",
            Email = "sam@flowcore.demo",
            JoinedAt = now.AddMonths(-3),
            IsActive = true
        };

        var casey = new User
        {
            Id = DemoSeedIds.UserCasey,
            FullName = "Casey Rivera",
            Email = "casey@flowcore.demo",
            JoinedAt = now.AddMonths(-2),
            IsActive = true
        };

        var jordan = new User
        {
            Id = DemoSeedIds.UserJordan,
            FullName = "Jordan Lee",
            Email = "jordan@flowcore.demo",
            JoinedAt = now.AddMonths(-2),
            IsActive = true
        };

        var morgan = new User
        {
            Id = DemoSeedIds.UserMorgan,
            FullName = "Morgan Kim",
            Email = "morgan@flowcore.demo",
            JoinedAt = now.AddMonths(-1),
            IsActive = true
        };

        var users = new List<User> { ownerAlex, memberSam, casey, jordan, morgan };
        var team = new Team(ownerAlex, memberSam, casey, jordan, morgan);

        var tagUi = new Tag { Id = DemoSeedIds.TagUi, Name = "ui", ColorHex = "#6366F1" };
        var tagBug = new Tag { Id = DemoSeedIds.TagBug, Name = "bug", ColorHex = "#EF4444" };
        var tags = new List<Tag> { tagUi, tagBug };

        var organization = new Workspace
        {
            Id = DemoSeedIds.WorkspaceNorth,
            Name = "Acme Corporation",
            Description = "Your company’s workspace — projects group work by product, platform, or internal function.",
            CreatedAt = now.AddDays(-90),
            ArchivedAt = null,
            Visibility = WorkspaceVisibility.Team,
            OwnerUserId = ownerAlex.Id
        };

        var marketingSite = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            "Acme.com — marketing & sign-up",
            "Public site, content, SEO, and self-serve trial checkout.",
            ProjectStatus.Active,
            ProjectPriority.High);
        SeedMarketingSiteTasks(marketingSite, now, team, tagUi, tagBug, Ng);

        var retailApp = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            "Acme Shop — mobile",
            "Customer iOS/Android app: browse, cart, and order tracking.",
            ProjectStatus.Active,
            ProjectPriority.High);
        SeedRetailAppTasks(retailApp, now, team, tagUi, tagBug, Ng);

        var designSys = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            "Compass — design system",
            "Figma kit, React primitives, and tokens shared across product surfaces.",
            ProjectStatus.Planning,
            ProjectPriority.Low);
        SeedDesignSystemTasks(designSys, now, team, tagUi, Ng);

        var partnerIntegrations = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            "Partner Hub — revenue integrations",
            "Wholesale portals, EDI hooks, and ERP-facing APIs for top partners.",
            ProjectStatus.Planning,
            ProjectPriority.Medium);
        SeedPartnerIntegrationTasks(partnerIntegrations, now, team, tagBug, Ng);

        var peopleTech = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            "People tech — new hire experience",
            "Device prep, identity groups, and lightweight automations so week-one isn’t helpdesk roulette.",
            ProjectStatus.Active,
            ProjectPriority.Low);
        SeedPeopleTechTasks(peopleTech, now, team, Ng);

        organization.Projects.Add(marketingSite.Project);
        organization.Projects.Add(retailApp.Project);
        organization.Projects.Add(designSys.Project);
        organization.Projects.Add(partnerIntegrations.Project);
        organization.Projects.Add(peopleTech.Project);

        var workspaces = new List<Workspace> { organization };

        return new DemoDataGraph
        {
            Workspaces = workspaces,
            Users = users,
            Tags = tags
        };
    }

    private static TaskItem NewTask(
        Func<Guid> ng,
        BoardColumn column,
        TaskStatusDefinition status,
        string title,
        string description,
        TaskPriority priority,
        int storyPoints,
        DateTime createdAt,
        DateTime updatedAt,
        DateTime? dueDate,
        TaskItem? parent)
    {
        var t = new TaskItem
        {
            Id = ng(),
            BoardColumnId = column.Id,
            Title = title,
            Description = description,
            TaskStatusDefinitionId = status.Id,
            Priority = priority,
            StoryPoints = storyPoints,
            ParentTaskItemId = parent?.Id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DueDate = dueDate,
            BoardColumn = column,
            TaskStatusDefinition = status,
            ParentTaskItem = parent
        };
        column.Tasks.Add(t);
        status.TaskItems.Add(t);
        if (parent is not null)
        {
            parent.Subtasks.Add(t);
        }

        return t;
    }

    private static void Assign(TaskItem task, User user, TaskRole role, DateTime at)
    {
        var a = new TaskAssignment
        {
            TaskItemId = task.Id,
            UserId = user.Id,
            Role = role,
            AssignedAt = at,
            TaskItem = task,
            User = user
        };
        task.TaskAssignments.Add(a);
        user.TaskAssignments.Add(a);
    }

    private static void AssignMany(TaskItem task, DateTime baseAt, params User[] assignees)
    {
        for (var i = 0; i < assignees.Length; i++)
            Assign(task, assignees[i], TaskRole.Assignee, baseAt.AddHours(-i));
    }

    private static void LinkTag(TaskItem task, Tag tag, DateTime at)
    {
        var link = new TaskTag
        {
            TaskItemId = task.Id,
            TagId = tag.Id,
            LinkedAt = at,
            TaskItem = task,
            Tag = tag
        };
        task.TaskTags.Add(link);
        tag.TaskTags.Add(link);
    }

    private static void AddComment(Func<Guid> ng, TaskItem task, User author, string body, DateTime at)
    {
        var c = new Comment
        {
            Id = ng(),
            TaskItemId = task.Id,
            AuthorUserId = author.Id,
            Body = body,
            CreatedAt = at,
            EditedAt = null,
            TaskItem = task
        };
        task.Comments.Add(c);
    }

    private static void SeedMarketingSiteTasks(
        ProjectBoardContext ctx,
        DateTime now,
        Team team,
        Tag tagUi,
        Tag tagBug,
        Func<Guid> ng)
    {
        var (alex, sam, casey, jordan, morgan) = team;

        var epicIa = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Primary nav & URL scheme (pre-build)",
            "Lock IA before eng cuts templates—pricing, solutions, and docs need stable paths.",
            TaskPriority.High,
            8,
            now.AddDays(-14),
            now,
            now.AddDays(16),
            parent: null);
        AssignMany(epicIa, now.AddDays(-14), alex, sam, casey);
        LinkTag(epicIa, tagUi, now.AddDays(-10));

        var subIa1 = NewTask(
            ng,
            ctx.ColTodo,
            ctx.InProgress,
            "Approved nav wireframes (desktop + mobile)",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-10),
            now,
            null,
            epicIa);
        Assign(subIa1, sam, TaskRole.Assignee, now.AddDays(-10));

        var subIa2 = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "301/302 redirect map from legacy blog URLs",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-10),
            now,
            null,
            epicIa);

        var epicCheckout = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "14-day trial checkout (Stripe)",
            "Card-on-file optional; region-aware tax display.",
            TaskPriority.High,
            13,
            now.AddDays(-8),
            now,
            now.AddDays(9),
            parent: null);
        AssignMany(epicCheckout, now.AddDays(-8), alex, sam, casey, jordan, morgan);

        var subPay = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "PaymentIntent lifecycle + signed webhooks",
            string.Empty,
            TaskPriority.High,
            5,
            now.AddDays(-5),
            now,
            null,
            epicCheckout);
        AssignMany(subPay, now.AddDays(-5), alex, sam);

        var subErr = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Toast + retry copy for soft declines",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-4),
            now,
            null,
            epicCheckout);

        var epicTrust = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Trial module — trust badges & fine print",
            "Above-the-fold block on signup; legal wants EU-specific footnotes.",
            TaskPriority.Medium,
            5,
            now.AddDays(-6),
            now,
            now.AddDays(11),
            parent: null);
        Assign(epicTrust, sam, TaskRole.Assignee, now.AddDays(-6));

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "Source SVG badges from brand toolkit (SOC2, GDPR)",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-5),
            now,
            null,
            epicTrust);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Legal review notes folded into signup accordion",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-3),
            now,
            null,
            epicTrust);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "Programmatic SEO: PDP + collection title templates",
            "Coordinate with catalog ops on character limits.",
            TaskPriority.Medium,
            3,
            now.AddDays(-4),
            now,
            null,
            parent: null);

        var heroPl = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Homepage hero + bestseller rail (responsive)",
            "Match Figma 1440 / 768 / 390 breakpoints.",
            TaskPriority.High,
            5,
            now.AddDays(-7),
            now,
            now.AddDays(6),
            parent: null);
        AssignMany(heroPl, now.AddDays(-7), alex, sam, casey, jordan);
        LinkTag(heroPl, tagUi, now.AddDays(-6));

        var analytics = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "GA4: sign-up funnel event map v2",
            "Align names with mobile for exec dashboard.",
            TaskPriority.Medium,
            2,
            now.AddDays(-2),
            now,
            now.AddDays(13),
            parent: null);
        Assign(analytics, alex, TaskRole.Assignee, now.AddDays(-2));

        var safari = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Safari 17: flex gap regression on category chips",
            "Polyfill only if perf budget allows.",
            TaskPriority.Medium,
            2,
            now.AddDays(-4),
            now,
            now.AddDays(3),
            parent: null);
        LinkTag(safari, tagBug, now.AddDays(-3));
        AssignMany(safari, now.AddDays(-4), sam, casey, jordan);

        NewTask(
            ng,
            ctx.ColDone,
            ctx.Done,
            "Privacy center IA — shipped in docs subdomain",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-22),
            now.AddDays(-4),
            null,
            parent: null);

        NewTask(
            ng,
            ctx.ColDone,
            ctx.Done,
            "Homepage hero A/B (Q1) — readout & shutdown",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-18),
            now.AddDays(-6),
            null,
            parent: null);

        AddComment(ng, epicIa, sam,
            "Redirects wait on wireframe sign-off — don’t ask CMS for slugs yet.",
            now.AddDays(-8));
        AddComment(ng, epicCheckout, alex,
            "Webhook signing secret rotated in vault this morning; staging redeployed.",
            now.AddDays(-3));
        AddComment(ng, heroPl, sam,
            "Using 2× exports from Figma node `Hero / Spring` — ping if raster shifts.",
            now.AddDays(-5));
        AddComment(ng, safari, sam,
            "Still reproduces on iOS 18.4 simulator — not visible in Chromium.",
            now.AddDays(-2));
    }

    private static void SeedRetailAppTasks(
        ProjectBoardContext ctx,
        DateTime now,
        Team team,
        Tag tagUi,
        Tag tagBug,
        Func<Guid> ng)
    {
        var (alex, sam, casey, jordan, morgan) = team;

        var epicOnboard = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "First-launch experience (v3)",
            "Fewer screens; Face ID optional; restore purchases.",
            TaskPriority.High,
            8,
            now.AddDays(-11),
            now,
            now.AddDays(12),
            parent: null);
        AssignMany(epicOnboard, now.AddDays(-11), alex, sam, casey, jordan, morgan);
        LinkTag(epicOnboard, tagUi, now.AddDays(-9));

        var subSplash = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Motion-safe splash + notification pre-prompt",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-8),
            now,
            null,
            epicOnboard);
        Assign(subSplash, alex, TaskRole.Assignee, now.AddDays(-8));

        var subBio = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Biometric opt-in & fallback to PIN",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-6),
            now,
            null,
            epicOnboard);

        var epicOffline = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Offline product browse (read-mostly)",
            "Show last-synced catalog when offline banner shows.",
            TaskPriority.High,
            13,
            now.AddDays(-7),
            now,
            now.AddDays(20),
            parent: null);
        AssignMany(epicOffline, now.AddDays(-7), sam, casey, jordan, morgan);

        var subSync = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "Merge rules when prices change mid-session",
            string.Empty,
            TaskPriority.High,
            5,
            now.AddDays(-5),
            now,
            null,
            epicOffline);

        var subQueue = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Reliable outbox for favorites + cart deltas",
            string.Empty,
            TaskPriority.Medium,
            5,
            now.AddDays(-4),
            now,
            null,
            epicOffline);

        var push = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Push deep links: order status → in-app screen",
            "Handle cold start and expired JWT.",
            TaskPriority.Medium,
            3,
            now.AddDays(-5),
            now,
            now.AddDays(6),
            parent: null);
        AssignMany(push, now.AddDays(-5), alex, sam, morgan);

        var crash = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Crash: UIImagePickerController on 256MB devices",
            "#1 in Firebase for build 3.0.4.",
            TaskPriority.High,
            2,
            now.AddDays(-2),
            now,
            now.AddDays(4),
            parent: null);
        LinkTag(crash, tagBug, now.AddDays(-2));
        AssignMany(crash, now.AddDays(-2), casey, jordan);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Order tracking map: match fulfilment carrier palette",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-3),
            now,
            now.AddDays(9),
            parent: null);

        var saveForLater = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Save-for-later sync across phone + tablet",
            string.Empty,
            TaskPriority.Medium,
            5,
            now.AddDays(-4),
            now,
            now.AddDays(8),
            parent: null);
        Assign(saveForLater, alex, TaskRole.Assignee, now.AddDays(-4));

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "App Review notes + demo account for 3.1 submission",
            string.Empty,
            TaskPriority.High,
            2,
            now.AddDays(-1),
            now,
            now.AddDays(5),
            parent: null);

        NewTask(
            ng,
            ctx.ColDone,
            ctx.Done,
            "App Store creatives refresh (spring drop)",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-19),
            now.AddDays(-3),
            null,
            parent: null);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "Dark mode regression matrix (iPad + phone)",
            string.Empty,
            TaskPriority.Low,
            3,
            now.AddDays(-1),
            now,
            null,
            parent: null);

        NewTask(
            ng,
            ctx.ColDone,
            ctx.Done,
            "February beta cohort — feedback export & thank-you mail",
            string.Empty,
            TaskPriority.Medium,
            1,
            now.AddDays(-14),
            now.AddDays(-5),
            null,
            parent: null);

        NewTask(
            ng,
            ctx.ColDone,
            ctx.Done,
            "Sunset legacy wishlist endpoint (410 Gone)",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-25),
            now.AddDays(-7),
            null,
            parent: null);

        AddComment(ng, epicOnboard, alex,
            "Drop the second marketing slide if locale = DE — legal asked yesterday.",
            now.AddDays(-5));
        AddComment(ng, epicOffline, sam,
            "Desktop team wants same merge rules eventually; keep interfaces internal for now.",
            now.AddDays(-4));
        AddComment(ng, push, alex,
            "Firebase dynamic link TTL is 7d — doc that in runbook.",
            now.AddDays(-2));
        AddComment(ng, crash, alex,
            "Repro on iPhone SE 2022 with 20+ tabs backgrounded.",
            now.AddDays(-1));
    }

    private static void SeedDesignSystemTasks(
        ProjectBoardContext ctx,
        DateTime now,
        Team team,
        Tag tagUi,
        Func<Guid> ng)
    {
        var (alex, sam, casey, jordan, morgan) = team;

        var buttons = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Button primitives — size ramps & focus ring",
            "Align with WCAG 2.2 focus-visible spec.",
            TaskPriority.Medium,
            5,
            now.AddDays(-8),
            now,
            now.AddDays(14),
            parent: null);
        AssignMany(buttons, now.AddDays(-8), alex, jordan, morgan);
        LinkTag(buttons, tagUi, now.AddDays(-7));

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "SM / MD / LG touch targets from spacing scale 4/6/8",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-6),
            now,
            null,
            buttons);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "Destructive variant: hover vs focus story in Storybook",
            string.Empty,
            TaskPriority.Low,
            1,
            now.AddDays(-4),
            now,
            null,
            buttons);

        var audit = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Quarterly drift check: Figma UI kit vs Storybook props",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-3),
            now,
            now.AddDays(21),
            parent: null);
        Assign(audit, sam, TaskRole.Assignee, now.AddDays(-3));

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "DataTable density tokens (comfortable / compact)",
            "Blocked on commerce grid work.",
            TaskPriority.Low,
            3,
            now.AddDays(-2),
            now,
            null,
            parent: null);

        AddComment(ng, buttons, sam,
            "Ping design before merging — they’re renaming `accent-subtle` this week.",
            now.AddDays(-5));
    }

    private static void SeedPartnerIntegrationTasks(
        ProjectBoardContext ctx,
        DateTime now,
        Team team,
        Tag tagBug,
        Func<Guid> ng)
    {
        var (alex, sam, casey, jordan, morgan) = team;

        var webhooks = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Shopify wholesale orders — idempotent webhook handler",
            "Double events during flash sales; use payload id + HMAC.",
            TaskPriority.High,
            8,
            now.AddDays(-6),
            now,
            now.AddDays(9),
            parent: null);
        AssignMany(webhooks, now.AddDays(-6), alex, sam, casey, jordan, morgan);
        LinkTag(webhooks, tagBug, now.AddDays(-5));

        var netsuite = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "NetSuite SKU sync — discovery brief for RevOps",
            "Need field map from ERP owner before API spike.",
            TaskPriority.Medium,
            3,
            now.AddDays(-2),
            now,
            null,
            parent: null);

        var sso = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Distributor portal SSO handoff (SAML)",
            "Vendor: Okta; target pilot account in May.",
            TaskPriority.Medium,
            5,
            now.AddDays(-4),
            now,
            now.AddDays(18),
            parent: null);
        AssignMany(sso, now.AddDays(-4), sam, morgan);

        AddComment(ng, webhooks, sam,
            "Logged 412 duplicates last Friday — table `wh_order_events` is catching them now.",
            now.AddDays(-2));
        AddComment(ng, netsuite, alex,
            "Won’t schedule eng until RevOps confirms nightly vs near-real-time.",
            now.AddDays(-1));
    }

    private static void SeedPeopleTechTasks(
        ProjectBoardContext ctx,
        DateTime now,
        Team team,
        Func<Guid> ng)
    {
        var (alex, sam, casey, jordan, morgan) = team;

        var laptops = NewTask(
            ng,
            ctx.ColDoing,
            ctx.InProgress,
            "Spring laptop refresh — pilot cohort (sales)",
            "Encrypted fleet; ship window March 18–28.",
            TaskPriority.Medium,
            5,
            now.AddDays(-9),
            now,
            now.AddDays(12),
            parent: null);
        AssignMany(laptops, now.AddDays(-9), alex, sam, casey);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Jamf policy tag: `refresh-2026-spring` on 42 devices",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-6),
            now,
            null,
            laptops);

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "FedEx return labels + spreadsheet for Facilities",
            string.Empty,
            TaskPriority.Low,
            1,
            now.AddDays(-5),
            now,
            null,
            laptops);

        var okta = NewTask(
            ng,
            ctx.ColTodo,
            ctx.Todo,
            "Okta: auto-add new hires to “All Acme” + Slack on day-one",
            "HRIS webhook already sends start date.",
            TaskPriority.Medium,
            3,
            now.AddDays(-4),
            now,
            now.AddDays(20),
            parent: null);
        Assign(okta, alex, TaskRole.Assignee, now.AddDays(-4));

        NewTask(
            ng,
            ctx.ColTodo,
            ctx.Backlog,
            "Swag + desk checklist automation (Notion → email)",
            "Nice-to-have after laptop flow is stable.",
            TaskPriority.Low,
            2,
            now.AddDays(-2),
            now,
            null,
            parent: null);

        AddComment(ng, laptops, alex,
            "Two folks in London warehouse extended ship by 3 days — rows 18–19 on the sheet.",
            now.AddDays(-3));
    }

}
