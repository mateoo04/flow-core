using FlowCore.Models;
using FlowCore.Models.ViewModels;

namespace FlowCore.Services;

public static class TaskAssigneeStackBuilder
{
    private const int MaxVisible = 3;

    public static TaskAssigneeStackVm FromTask(TaskItem task)
    {
        var assignees = task.TaskAssignments
            .Where(a => a.Role == TaskRole.Assignee)
            .Select(a => a.User)
            .Where(u => u is not null)
            .Cast<User>()
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.FullName)
            .ToList();

        var shown = assignees.Take(MaxVisible)
            .Select(u => new AssigneeAvatarVm
            {
                Initials = UserDisplayHelper.GetInitials(u.FullName),
                BackgroundHex = UserDisplayHelper.BackgroundColorForUser(u.Id)
            })
            .ToList();

        return new TaskAssigneeStackVm
        {
            Shown = shown,
            ExtraCount = Math.Max(0, assignees.Count - MaxVisible)
        };
    }
}
