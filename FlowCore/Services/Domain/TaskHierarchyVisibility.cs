using System.Linq.Expressions;
using FlowCore.Models;

namespace FlowCore.Services.Domain;

public static class TaskHierarchyVisibility
{
    public static bool IsBoardRootCard(TaskItem task) => task.ParentTaskItemId is null;

    public static Expression<Func<TaskItem, bool>> MyTasksRootCard(Guid userId)
    {
        return task => task.ParentTaskItemId == null
            && (task.TaskAssignments.Any(a => a.UserId == userId)
                || task.Subtasks.Any(s => s.TaskAssignments.Any(a => a.UserId == userId)));
    }
}
