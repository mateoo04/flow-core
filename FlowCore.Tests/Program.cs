using FlowCore.Models;
using FlowCore.Services.Domain;

var currentUserId = Guid.NewGuid();
var otherUserId = Guid.NewGuid();

AssertTrue(
    "Board cards include root tasks",
    TaskHierarchyVisibility.IsBoardRootCard(new TaskItem()));

AssertFalse(
    "Board cards exclude subtasks",
    TaskHierarchyVisibility.IsBoardRootCard(new TaskItem { ParentTaskItemId = Guid.NewGuid() }));

var myTasksPredicate = TaskHierarchyVisibility.MyTasksRootCard(currentUserId).Compile();

AssertTrue(
    "My tasks includes root tasks assigned to current user",
    myTasksPredicate(new TaskItem
    {
        TaskAssignments =
        {
            new TaskAssignment { UserId = currentUserId }
        }
    }));

AssertFalse(
    "My tasks excludes raw subtasks assigned to current user",
    myTasksPredicate(new TaskItem
    {
        ParentTaskItemId = Guid.NewGuid(),
        TaskAssignments =
        {
            new TaskAssignment { UserId = currentUserId }
        }
    }));

var parentId = Guid.NewGuid();
AssertTrue(
    "My tasks includes parent tasks when a subtask is assigned to current user",
    myTasksPredicate(new TaskItem
    {
        Id = parentId,
        Subtasks =
        {
            new TaskItem
            {
                ParentTaskItemId = parentId,
                TaskAssignments =
                {
                    new TaskAssignment { UserId = currentUserId }
                }
            }
        }
    }));

AssertFalse(
    "My tasks excludes parent tasks when only another user's subtask is assigned",
    myTasksPredicate(new TaskItem
    {
        Id = parentId,
        Subtasks =
        {
            new TaskItem
            {
                ParentTaskItemId = parentId,
                TaskAssignments =
                {
                    new TaskAssignment { UserId = otherUserId }
                }
            }
        }
    }));

Console.WriteLine("Task hierarchy visibility tests passed.");

static void AssertTrue(string name, bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Expected true: {name}");
    }
}

static void AssertFalse(string name, bool condition)
{
    if (condition)
    {
        throw new InvalidOperationException($"Expected false: {name}");
    }
}
