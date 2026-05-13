using FlowCore.Models;
using FlowCore.Services.Domain;
using Xunit;

namespace FlowCore.Tests;

public class TaskHierarchyVisibilityTests
{
    [Fact]
    public void IsBoardRootCard_ReturnsTrue_ForRootTask()
    {
        var task = new TaskItem();

        var isRootCard = TaskHierarchyVisibility.IsBoardRootCard(task);

        Assert.True(isRootCard);
    }

    [Fact]
    public void IsBoardRootCard_ReturnsFalse_ForSubtask()
    {
        var task = new TaskItem { ParentTaskItemId = Guid.NewGuid() };

        var isRootCard = TaskHierarchyVisibility.IsBoardRootCard(task);

        Assert.False(isRootCard);
    }

    [Fact]
    public void MyTasksRootCard_ReturnsTrue_ForAssignedRootTask()
    {
        var currentUserId = Guid.NewGuid();
        var predicate = TaskHierarchyVisibility.MyTasksRootCard(currentUserId).Compile();
        var task = new TaskItem
        {
            TaskAssignments =
            {
                new TaskAssignment { UserId = currentUserId }
            }
        };

        var isVisible = predicate(task);

        Assert.True(isVisible);
    }

    [Fact]
    public void MyTasksRootCard_ReturnsFalse_ForAssignedSubtask()
    {
        var currentUserId = Guid.NewGuid();
        var predicate = TaskHierarchyVisibility.MyTasksRootCard(currentUserId).Compile();
        var task = new TaskItem
        {
            ParentTaskItemId = Guid.NewGuid(),
            TaskAssignments =
            {
                new TaskAssignment { UserId = currentUserId }
            }
        };

        var isVisible = predicate(task);

        Assert.False(isVisible);
    }

    [Fact]
    public void MyTasksRootCard_ReturnsTrue_ForParentWithAssignedSubtask()
    {
        var currentUserId = Guid.NewGuid();
        var predicate = TaskHierarchyVisibility.MyTasksRootCard(currentUserId).Compile();
        var parentId = Guid.NewGuid();
        var task = new TaskItem
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
        };

        var isVisible = predicate(task);

        Assert.True(isVisible);
    }

    [Fact]
    public void MyTasksRootCard_ReturnsFalse_ForParentWithOtherUsersAssignedSubtask()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var predicate = TaskHierarchyVisibility.MyTasksRootCard(currentUserId).Compile();
        var parentId = Guid.NewGuid();
        var task = new TaskItem
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
        };

        var isVisible = predicate(task);

        Assert.False(isVisible);
    }
}
