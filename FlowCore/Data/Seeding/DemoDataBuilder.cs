using FlowCore.Models;
using Microsoft.AspNetCore.Identity;

namespace FlowCore.Data;

public sealed record SampleGraph(
    IReadOnlyList<User> Users,
    IReadOnlyList<Tag> Tags,
    IReadOnlyList<Workspace> Workspaces,
    IReadOnlyList<WorkspaceMember> WorkspaceMembers);

public static class DemoDataBuilder
{
    private readonly record struct Team(User Alex, User Sam, User Casey, User Jordan, User Morgan);

    public static SampleGraph CreateSampleGraph(IPasswordHasher<User> hasher, string sharedPassword)
    {
        var now = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        Guid Ng() => Guid.NewGuid();

        var ownerAlex = new User
        {
            Id = DemoSeedIds.UserAlex,
            FullName = "Alex Owner",
            Email = "alex@flowcore.demo",
            NormalizedEmail = "ALEX@FLOWCORE.DEMO",
            UserName = "alex@flowcore.demo",
            NormalizedUserName = "ALEX@FLOWCORE.DEMO",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            JoinedAt = now.AddMonths(-6),
            IsActive = true
        };
        ownerAlex.PasswordHash = hasher.HashPassword(ownerAlex, sharedPassword);

        var memberSam = new User
        {
            Id = DemoSeedIds.UserSam,
            FullName = "Sam Member",
            Email = "sam@flowcore.demo",
            NormalizedEmail = "SAM@FLOWCORE.DEMO",
            UserName = "sam@flowcore.demo",
            NormalizedUserName = "SAM@FLOWCORE.DEMO",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            JoinedAt = now.AddMonths(-3),
            IsActive = true
        };
        memberSam.PasswordHash = hasher.HashPassword(memberSam, sharedPassword);

        var casey = new User
        {
            Id = DemoSeedIds.UserCasey,
            FullName = "Casey Rivera",
            Email = "casey@flowcore.demo",
            NormalizedEmail = "CASEY@FLOWCORE.DEMO",
            UserName = "casey@flowcore.demo",
            NormalizedUserName = "CASEY@FLOWCORE.DEMO",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            JoinedAt = now.AddMonths(-2),
            IsActive = true
        };
        casey.PasswordHash = hasher.HashPassword(casey, sharedPassword);

        var jordan = new User
        {
            Id = DemoSeedIds.UserJordan,
            FullName = "Jordan Lee",
            Email = "jordan@flowcore.demo",
            NormalizedEmail = "JORDAN@FLOWCORE.DEMO",
            UserName = "jordan@flowcore.demo",
            NormalizedUserName = "JORDAN@FLOWCORE.DEMO",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            JoinedAt = now.AddMonths(-2),
            IsActive = true
        };
        jordan.PasswordHash = hasher.HashPassword(jordan, sharedPassword);

        var morgan = new User
        {
            Id = DemoSeedIds.UserMorgan,
            FullName = "Morgan Kim",
            Email = "morgan@flowcore.demo",
            NormalizedEmail = "MORGAN@FLOWCORE.DEMO",
            UserName = "morgan@flowcore.demo",
            NormalizedUserName = "MORGAN@FLOWCORE.DEMO",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            JoinedAt = now.AddMonths(-1),
            IsActive = true
        };
        morgan.PasswordHash = hasher.HashPassword(morgan, sharedPassword);

        var demoUser = new User
        {
            Id = DemoSeedIds.UserDemo,
            FullName = "Demo User",
            Email = DemoSeedIds.UserDemoEmail,
            NormalizedEmail = DemoSeedIds.UserDemoEmail.ToUpperInvariant(),
            UserName = DemoSeedIds.UserDemoEmail,
            NormalizedUserName = DemoSeedIds.UserDemoEmail.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            JoinedAt = now,
            IsActive = true
        };
        demoUser.PasswordHash = hasher.HashPassword(demoUser, sharedPassword);

        var users = new List<User> { ownerAlex, memberSam, casey, jordan, morgan, demoUser };
        var team = new Team(ownerAlex, memberSam, casey, jordan, morgan);

        var tagUi = new Tag { Id = DemoSeedIds.TagUi, Name = "ui", ColorHex = "#6366F1" };
        var tagBug = new Tag { Id = DemoSeedIds.TagBug, Name = "bug", ColorHex = "#EF4444" };
        var tags = new List<Tag> { tagUi, tagBug };

        var organization = new Workspace
        {
            Id = DemoSeedIds.WorkspaceNorth,
            Name = "Acme Corporation",
            Description = "Your company's workspace: projects group work by product, platform, or internal function.",
            CreatedAt = now.AddDays(-90),
        };

        var statuses = ProjectBlueprint.CreateWorkspaceStatuses(organization.Id, now, Ng);
        foreach (var s in statuses.All)
        {
            s.Workspace = organization;
            organization.TaskStatusDefinitions.Add(s);
        }

        var marketingSite = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            statuses,
            "Acme.com: marketing & sign-up",
            "Public site, content, SEO, and self-serve trial checkout.",
            ProjectStatus.Active,
            ProjectPriority.High);
        SeedMarketingSiteTasks(marketingSite, now, team, tagUi, tagBug, Ng);

        var retailApp = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            statuses,
            "Acme Shop: mobile",
            "Customer iOS/Android app: browse, cart, and order tracking.",
            ProjectStatus.Active,
            ProjectPriority.High);
        SeedRetailAppTasks(retailApp, now, team, tagUi, tagBug, Ng);

        var designSys = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            statuses,
            "Compass: design system",
            "Figma kit, React primitives, and tokens shared across product surfaces.",
            ProjectStatus.Planning,
            ProjectPriority.Low);
        SeedDesignSystemTasks(designSys, now, team, tagUi, Ng);

        var partnerIntegrations = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            statuses,
            "Partner Hub: revenue integrations",
            "Wholesale portals, EDI hooks, and ERP-facing APIs for top partners.",
            ProjectStatus.Planning,
            ProjectPriority.Medium);
        SeedPartnerIntegrationTasks(partnerIntegrations, now, team, tagBug, Ng);

        var peopleTech = ProjectBlueprint.CreateProject(
            organization,
            Ng,
            now,
            statuses,
            "People tech: new hire experience",
            "Device prep, identity groups, and lightweight automations so week-one isn't helpdesk roulette.",
            ProjectStatus.Active,
            ProjectPriority.Low);
        SeedPeopleTechTasks(peopleTech, now, team, Ng);

        organization.Projects.Add(marketingSite.Project);
        organization.Projects.Add(retailApp.Project);
        organization.Projects.Add(designSys.Project);
        organization.Projects.Add(partnerIntegrations.Project);
        organization.Projects.Add(peopleTech.Project);

        ApplyDemoGraphRandomization(organization, users, demoUser, statuses, now, Ng);

        var workspaces = new List<Workspace> { organization };

        // Build membership list
        var memberships = new List<WorkspaceMember>();

        void AddMember(Guid workspaceId, Guid userId, WorkspaceRole role)
        {
            if (memberships.Any(m => m.WorkspaceId == workspaceId && m.UserId == userId))
                return;
            memberships.Add(new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId = userId,
                Role = role,
                JoinedAt = now
            });
        }

        // Owners: whoever was the previous OwnerUserId becomes the Owner WorkspaceMember.
        AddMember(DemoSeedIds.WorkspaceNorth, DemoSeedIds.UserAlex, WorkspaceRole.Owner);
        AddMember(DemoSeedIds.WorkspaceNorth, DemoSeedIds.UserDemo, WorkspaceRole.Member);

        // Members: anyone with a TaskAssignment to a task in workspace W is at least a Member of W.
        // Walk the already-built graph: workspace -> projects -> boards -> tasks -> assignments.
        foreach (var ws in workspaces)
        {
            foreach (var project in ws.Projects)
                foreach (var board in project.Boards)
                    foreach (var task in board.Tasks)
                        foreach (var assignment in task.TaskAssignments)
                            AddMember(ws.Id, assignment.UserId, WorkspaceRole.Member);
        }

        return new SampleGraph(users, tags, workspaces, memberships);
    }

    private static TaskItem NewTask(
        Func<Guid> ng,
        Board board,
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
            BoardId = board.Id,
            Title = title,
            Description = description,
            TaskStatusDefinitionId = status.Id,
            Priority = priority,
            StoryPoints = storyPoints,
            ParentTaskItemId = parent?.Id,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DueDate = dueDate,
            Board = board,
            TaskStatusDefinition = status,
            ParentTaskItem = parent
        };
        board.Tasks.Add(t);
        status.TaskItems.Add(t);
        if (parent is not null)
        {
            parent.Subtasks.Add(t);
        }

        return t;
    }

    private static void Assign(TaskItem task, User user, DateTime at)
    {
        var a = new TaskAssignment
        {
            TaskItemId = task.Id,
            UserId = user.Id,
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
            Assign(task, assignees[i], baseAt.AddHours(-i));
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
            ctx.Board,
            ctx.Todo,
            "Primary nav & URL scheme (pre-build)",
            "Lock IA before eng cuts templates: pricing, solutions, and docs need stable paths.",
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
            ctx.Board,
            ctx.InProgress,
            "Approved nav wireframes (desktop + mobile)",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-10),
            now,
            null,
            epicIa);
        Assign(subIa1, sam, now.AddDays(-10));

        var subIa2 = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "301/302 redirect map from legacy blog URLs",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-10),
            now,
            now.AddDays(12),
            epicIa);
        AssignMany(subIa2, now.AddDays(-10), jordan, morgan, casey);

        var epicCheckout = NewTask(
            ng,
            ctx.Board,
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
            ctx.Board,
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
            ctx.Board,
            ctx.Todo,
            "Toast + retry copy for soft declines",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-4),
            now,
            now.AddDays(8),
            epicCheckout);
        AssignMany(subErr, now.AddDays(-4), casey, morgan, jordan);

        var epicTrust = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Trial module: trust badges & fine print",
            "Above-the-fold block on signup; legal wants EU-specific footnotes.",
            TaskPriority.Medium,
            5,
            now.AddDays(-6),
            now,
            now.AddDays(11),
            parent: null);
        Assign(epicTrust, sam, now.AddDays(-6));

        var badges = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "Source SVG badges from brand toolkit (SOC2, GDPR)",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-5),
            now,
            now.AddDays(15),
            epicTrust);
        AssignMany(badges, now.AddDays(-5), jordan, morgan);

        var legalReview = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Legal review notes folded into signup accordion",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-3),
            now,
            now.AddDays(9),
            epicTrust);
        AssignMany(legalReview, now.AddDays(-3), sam, alex, morgan);

        var seo = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "Programmatic SEO: PDP + collection title templates",
            "Coordinate with catalog ops on character limits.",
            TaskPriority.Medium,
            3,
            now.AddDays(-4),
            now,
            now.AddDays(17),
            parent: null);
        AssignMany(seo, now.AddDays(-4), sam, casey, morgan);

        var heroPl = NewTask(
            ng,
            ctx.Board,
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
            ctx.Board,
            ctx.Todo,
            "GA4: sign-up funnel event map v2",
            "Align names with mobile for exec dashboard.",
            TaskPriority.Medium,
            2,
            now.AddDays(-2),
            now,
            now.AddDays(13),
            parent: null);
        Assign(analytics, alex, now.AddDays(-2));

        var safari = NewTask(
            ng,
            ctx.Board,
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
            ctx.Board,
            ctx.Done,
            "Privacy center IA, shipped in docs subdomain",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-22),
            now.AddDays(-4),
            null,
            parent: null);

        NewTask(
            ng,
            ctx.Board,
            ctx.Done,
            "Homepage hero A/B (Q1): readout & shutdown",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-18),
            now.AddDays(-6),
            null,
            parent: null);

        AddComment(ng, epicIa, sam,
            "Redirects wait on wireframe sign-off; don't ask CMS for slugs yet.",
            now.AddDays(-8));
        AddComment(ng, epicCheckout, alex,
            "Webhook signing secret rotated in vault this morning; staging redeployed.",
            now.AddDays(-3));
        AddComment(ng, heroPl, sam,
            "Using 2× exports from Figma node `Hero / Spring`. Ping if raster shifts.",
            now.AddDays(-5));
        AddComment(ng, safari, sam,
            "Still reproduces on iOS 18.4 simulator; not visible in Chromium.",
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
            ctx.Board,
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
            ctx.Board,
            ctx.Todo,
            "Motion-safe splash + notification pre-prompt",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-8),
            now,
            null,
            epicOnboard);
        Assign(subSplash, alex, now.AddDays(-8));

        var subBio = NewTask(
            ng,
            ctx.Board,
            ctx.InProgress,
            "Biometric opt-in & fallback to PIN",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-6),
            now,
            null,
            epicOnboard);
        AssignMany(subBio, now.AddDays(-6), casey, jordan, morgan);

        var epicOffline = NewTask(
            ng,
            ctx.Board,
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
            ctx.Board,
            ctx.Backlog,
            "Merge rules when prices change mid-session",
            string.Empty,
            TaskPriority.High,
            5,
            now.AddDays(-5),
            now,
            now.AddDays(11),
            epicOffline);
        AssignMany(subSync, now.AddDays(-5), alex, morgan, jordan);

        var subQueue = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Reliable outbox for favorites + cart deltas",
            string.Empty,
            TaskPriority.Medium,
            5,
            now.AddDays(-4),
            now,
            now.AddDays(14),
            epicOffline);
        AssignMany(subQueue, now.AddDays(-4), alex, casey, morgan);
        var push = NewTask(
            ng,
            ctx.Board,
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
            ctx.Board,
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

        var orderMap = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Order tracking map: match fulfilment carrier palette",
            string.Empty,
            TaskPriority.Medium,
            3,
            now.AddDays(-3),
            now,
            now.AddDays(9),
            parent: null);
        AssignMany(orderMap, now.AddDays(-3), morgan, jordan, alex);

        var saveForLater = NewTask(
            ng,
            ctx.Board,
            ctx.InProgress,
            "Save-for-later sync across phone + tablet",
            string.Empty,
            TaskPriority.Medium,
            5,
            now.AddDays(-4),
            now,
            now.AddDays(8),
            parent: null);
        Assign(saveForLater, alex, now.AddDays(-4));

        var appReview = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "App Review notes + demo account for 3.1 submission",
            string.Empty,
            TaskPriority.High,
            2,
            now.AddDays(-1),
            now,
            now.AddDays(5),
            parent: null);
        AssignMany(appReview, now.AddDays(-1), sam, casey, jordan);

        NewTask(
            ng,
            ctx.Board,
            ctx.Done,
            "App Store creatives refresh (spring drop)",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-19),
            now.AddDays(-3),
            null,
            parent: null);

        var darkMode = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "Dark mode regression matrix (iPad + phone)",
            string.Empty,
            TaskPriority.Low,
            3,
            now.AddDays(-1),
            now,
            now.AddDays(16),
            parent: null);
        AssignMany(darkMode, now.AddDays(-1), casey, sam, alex);

        NewTask(
            ng,
            ctx.Board,
            ctx.Done,
            "February beta cohort: feedback export & thank-you mail",
            string.Empty,
            TaskPriority.Medium,
            1,
            now.AddDays(-14),
            now.AddDays(-5),
            null,
            parent: null);

        NewTask(
            ng,
            ctx.Board,
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
            "Drop the second marketing slide if locale = DE; legal asked yesterday.",
            now.AddDays(-5));
        AddComment(ng, epicOffline, sam,
            "Desktop team wants same merge rules eventually; keep interfaces internal for now.",
            now.AddDays(-4));
        AddComment(ng, push, alex,
            "Firebase dynamic link TTL is 7d; doc that in runbook.",
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
            ctx.Board,
            ctx.InProgress,
            "Button primitives: size ramps & focus ring",
            "Align with WCAG 2.2 focus-visible spec.",
            TaskPriority.Medium,
            5,
            now.AddDays(-8),
            now,
            now.AddDays(14),
            parent: null);
        AssignMany(buttons, now.AddDays(-8), alex, jordan, morgan);
        LinkTag(buttons, tagUi, now.AddDays(-7));

        var touchTargets = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "SM / MD / LG touch targets from spacing scale 4/6/8",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-6),
            now,
            now.AddDays(11),
            buttons);
        AssignMany(touchTargets, now.AddDays(-6), sam, casey, jordan);

        var destructive = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "Destructive variant: hover vs focus story in Storybook",
            string.Empty,
            TaskPriority.Low,
            1,
            now.AddDays(-4),
            now,
            now.AddDays(9),
            buttons);
        AssignMany(destructive, now.AddDays(-4), morgan, sam, casey);

        var audit = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Quarterly drift check: Figma UI kit vs Storybook props",
            string.Empty,
            TaskPriority.Low,
            2,
            now.AddDays(-3),
            now,
            now.AddDays(21),
            parent: null);
        Assign(audit, sam, now.AddDays(-3));

        var dataTable = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "DataTable density tokens (comfortable / compact)",
            "Blocked on commerce grid work.",
            TaskPriority.Low,
            3,
            now.AddDays(-2),
            now,
            now.AddDays(24),
            parent: null);
        AssignMany(dataTable, now.AddDays(-2), jordan, alex, morgan);

        AddComment(ng, buttons, sam,
            "Ping design before merging; they're renaming `accent-subtle` this week.",
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
            ctx.Board,
            ctx.InProgress,
            "Shopify wholesale orders: idempotent webhook handler",
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
            ctx.Board,
            ctx.Backlog,
            "NetSuite SKU sync: discovery brief for RevOps",
            "Need field map from ERP owner before API spike.",
            TaskPriority.Medium,
            3,
            now.AddDays(-2),
            now,
            now.AddDays(30),
            parent: null);

        AssignMany(netsuite, now.AddDays(-2), casey, jordan, sam);

        var sso = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Distributor portal SSO handoff (SAML)",
            "Vendor: Okta; target pilot account in May.",
            TaskPriority.Medium,
            5,
            now.AddDays(-4),
            now,
            now.AddDays(18),
            parent: null);
        AssignMany(sso, now.AddDays(-4), sam, morgan, alex, casey);

        AddComment(ng, webhooks, sam,
            "Logged 412 duplicates last Friday; table `wh_order_events` is catching them now.",
            now.AddDays(-2));
        AddComment(ng, netsuite, alex,
            "Won't schedule eng until RevOps confirms nightly vs near-real-time.",
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
            ctx.Board,
            ctx.InProgress,
            "Spring laptop refresh: pilot cohort (sales)",
            "Encrypted fleet; ship window March 18-28.",
            TaskPriority.Medium,
            5,
            now.AddDays(-9),
            now,
            now.AddDays(12),
            parent: null);
        AssignMany(laptops, now.AddDays(-9), alex, sam, casey);

        var jamf = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Jamf policy tag: `refresh-2026-spring` on 42 devices",
            string.Empty,
            TaskPriority.Medium,
            2,
            now.AddDays(-6),
            now,
            now.AddDays(8),
            laptops);
        AssignMany(jamf, now.AddDays(-6), casey, jordan, morgan);

        var fedex = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "FedEx return labels + spreadsheet for Facilities",
            string.Empty,
            TaskPriority.Low,
            1,
            now.AddDays(-5),
            now,
            now.AddDays(6),
            laptops);
        AssignMany(fedex, now.AddDays(-5), sam, alex);

        var okta = NewTask(
            ng,
            ctx.Board,
            ctx.Todo,
            "Okta: auto-add new hires to “All Acme” + Slack on day-one",
            "HRIS webhook already sends start date.",
            TaskPriority.Medium,
            3,
            now.AddDays(-4),
            now,
            now.AddDays(20),
            parent: null);
        AssignMany(okta, now.AddDays(-4), alex, sam, casey, morgan);

        var swag = NewTask(
            ng,
            ctx.Board,
            ctx.Backlog,
            "Swag + desk checklist automation (Notion → email)",
            "Nice-to-have after laptop flow is stable.",
            TaskPriority.Low,
            2,
            now.AddDays(-2),
            now,
            now.AddDays(40),
            parent: null);
        AssignMany(swag, now.AddDays(-2), morgan, jordan, sam);

        AddComment(ng, laptops, alex,
            "Two folks in London warehouse extended ship by 3 days (rows 18-19 on the sheet).",
            now.AddDays(-3));
    }

    private static void ApplyDemoGraphRandomization(
        Workspace organization,
        List<User> users,
        User demoUser,
        WorkspaceStatuses statuses,
        DateTime now,
        Func<Guid> ng)
    {
        const int seed = 202605131;
        var rng = new Random(seed);

        EnsureExtraSubtasksForCoverage(organization, statuses, now, ng, rng);

        var allTasks = AllTasksFlat(organization).ToList();
        ClearAllTaskAssignments(allTasks, users);

        Shuffle(allTasks, rng);

        var pool = users.ToArray();
        var n = allTasks.Count;
        var c5 = Math.Min(2, n);
        var rem = Math.Max(0, n - c5);
        var c1 = rem == 0 ? 0 : (int)Math.Round(rem * 0.40);
        var c2 = rem == 0 ? 0 : (int)Math.Round(rem * 0.20);
        var c3 = rem == 0 ? 0 : (int)Math.Round(rem * 0.20);
        var c4 = rem - c1 - c2 - c3;
        if (c4 < 0)
        {
            c3 += c4;
            c4 = 0;
        }

        var i = 0;
        for (var u = 0; u < c5; u++, i++)
            AssignRandomTier(allTasks[i], 5, pool, now, rng);
        for (var u = 0; u < c4; u++, i++)
            AssignRandomTier(allTasks[i], 4, pool, now, rng);
        for (var u = 0; u < c3; u++, i++)
            AssignRandomTier(allTasks[i], 3, pool, now, rng);
        for (var u = 0; u < c2; u++, i++)
            AssignRandomTier(allTasks[i], 2, pool, now, rng);
        for (var u = 0; u < c1; u++, i++)
            Assign(allTasks[i], demoUser, now.AddMilliseconds(-u));

        while (i < n)
        {
            Assign(allTasks[i], demoUser, now.AddMilliseconds(-i));
            i++;
        }

        EnsureLegacyDemoPicksHaveDemo(organization, demoUser, now);
        ClampDemoUserBacklogToThree(organization, demoUser, statuses, now, rng);
        RebalanceDueDates(allTasks, now, rng);
    }

    private static IEnumerable<TaskItem> AllTasksFlat(Workspace ws) =>
        ws.Projects.SelectMany(p => p.Boards).SelectMany(b => b.Tasks);

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var k = list.Count - 1; k > 0; k--)
        {
            var j = rng.Next(k + 1);
            (list[k], list[j]) = (list[j], list[k]);
        }
    }

    private static void ClearAllTaskAssignments(IReadOnlyList<TaskItem> tasks, List<User> users)
    {
        foreach (var t in tasks)
        {
            foreach (var a in t.TaskAssignments.ToList())
                a.User?.TaskAssignments.Remove(a);
            t.TaskAssignments.Clear();
        }

        foreach (var u in users)
            u.TaskAssignments.Clear();
    }

    private static void AssignRandomTier(
        TaskItem task,
        int assigneeCount,
        User[] pool,
        DateTime now,
        Random rng)
    {
        var picks = pool.OrderBy(_ => rng.Next()).Take(assigneeCount).ToArray();
        AssignMany(task, now, picks);
    }

    private static void EnsureLegacyDemoPicksHaveDemo(Workspace organization, User demoUser, DateTime now)
    {
        var tick = 0;
        foreach (var project in organization.Projects)
        {
            var picks = project.Boards
                .SelectMany(b => b.Tasks)
                .OrderBy(t => t.Id)
                .GroupBy(t => t.TaskStatusDefinitionId)
                .SelectMany(g => g.Take(1))
                .Take(3)
                .ToList();

            foreach (var t in picks)
            {
                if (t.TaskAssignments.All(a => a.UserId != demoUser.Id))
                    Assign(t, demoUser, now.AddTicks(--tick));
            }
        }
    }

    private static void ClampDemoUserBacklogToThree(
        Workspace organization,
        User demoUser,
        WorkspaceStatuses statuses,
        DateTime now,
        Random rng)
    {
        var backlogId = statuses.Backlog.Id;

        bool DemoOn(TaskItem t) => t.TaskAssignments.Any(a => a.UserId == demoUser.Id);

        var demoTasks = AllTasksFlat(organization).Where(DemoOn).ToList();
        var inBacklog = demoTasks.Where(t => t.TaskStatusDefinitionId == backlogId).ToList();
        Shuffle(inBacklog, rng);

        while (inBacklog.Count > 3)
        {
            var t = inBacklog[^1];
            inBacklog.RemoveAt(inBacklog.Count - 1);
            var dest = rng.Next(3) switch
            {
                0 => statuses.Todo,
                1 => statuses.InProgress,
                _ => statuses.Done
            };
            MoveTaskToStatus(t, dest, now);
        }

        while (inBacklog.Count < 3)
        {
            var donor = demoTasks
                .Where(t => t.TaskStatusDefinitionId != backlogId && DemoOn(t))
                .OrderBy(_ => rng.Next())
                .FirstOrDefault(t => t.TaskStatusDefinition?.IsDoneState != true);
            if (donor is null)
                break;
            MoveTaskToStatus(donor, statuses.Backlog, now);
            inBacklog.Add(donor);
            demoTasks = AllTasksFlat(organization).Where(DemoOn).ToList();
            inBacklog = demoTasks.Where(t => t.TaskStatusDefinitionId == backlogId).ToList();
        }
    }

    private static void MoveTaskToStatus(TaskItem task, TaskStatusDefinition next, DateTime nowUtc)
    {
        if (task.TaskStatusDefinitionId == next.Id)
            return;
        task.TaskStatusDefinition?.TaskItems.Remove(task);
        task.TaskStatusDefinition = next;
        task.TaskStatusDefinitionId = next.Id;
        next.TaskItems.Add(task);
        task.UpdatedAt = nowUtc;
    }

    private static void EnsureExtraSubtasksForCoverage(
        Workspace organization,
        WorkspaceStatuses statuses,
        DateTime now,
        Func<Guid> ng,
        Random rng)
    {
        var roots = AllTasksFlat(organization).Where(t => t.ParentTaskItemId is null).ToList();
        var target = (int)Math.Ceiling(roots.Count / 2.0);
        while (roots.Count(r => r.Subtasks.Count > 0) < target)
        {
            var r = roots.Where(x => x.Subtasks.Count == 0).OrderBy(_ => rng.Next()).FirstOrDefault();
            if (r?.Board is null)
                break;

            var col = rng.Next(4) switch
            {
                0 => statuses.Backlog,
                1 => statuses.Todo,
                2 => statuses.InProgress,
                _ => statuses.Done
            };

            var title = r.Title.Length <= 40 ? r.Title : r.Title[..40];
            NewTask(
                ng,
                r.Board,
                col,
                "Drill-down: " + title,
                string.Empty,
                TaskPriority.Low,
                1,
                now.AddDays(-rng.Next(1, 10)),
                now,
                now.AddDays(rng.Next(6, 45)),
                r);
        }
    }

    private static void RebalanceDueDates(IReadOnlyList<TaskItem> allTasks, DateTime now, Random rng)
    {
        var list = allTasks.ToList();
        var target = (int)Math.Round(list.Count / 3.0);
        var have = list.Count(t => t.DueDate.HasValue);

        Shuffle(list, rng);

        if (have < target)
        {
            foreach (var t in list.Where(t => !t.DueDate.HasValue))
            {
                if (have >= target)
                    break;
                t.DueDate = now.Date.AddDays(rng.Next(2, 55));
                have++;
            }
        }
        else if (have > target)
        {
            foreach (var t in list.Where(t => t.DueDate.HasValue))
            {
                if (have <= target)
                    break;
                t.DueDate = null;
                have--;
            }
        }
    }

}
