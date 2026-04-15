using FlowCore.Data;
using FlowCore.Models;

namespace FlowCore.Repositories.InMemory;

internal static class TaskGraphRemoval
{
    /// <summary>Removes a task and all descendants from the graph (board, status, parent, assignments, tags, comments).</summary>
    public static void RemoveTaskRecursive(InMemoryDataStore store, TaskItem task)
    {
        foreach (var child in task.Subtasks.ToList())
            RemoveTaskRecursive(store, child);

        foreach (var c in task.Comments.ToList())
        {
            task.Comments.Remove(c);
            c.TaskItem = null;
        }

        foreach (var a in task.TaskAssignments.ToList())
        {
            a.User?.TaskAssignments.Remove(a);
            task.TaskAssignments.Remove(a);
        }

        foreach (var tt in task.TaskTags.ToList())
        {
            tt.Tag?.TaskTags.Remove(tt);
            task.TaskTags.Remove(tt);
        }

        task.Board?.Tasks.Remove(task);
        task.TaskStatusDefinition?.TaskItems.Remove(task);
        task.ParentTaskItem?.Subtasks.Remove(task);
    }
}
