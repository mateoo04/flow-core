namespace FlowCore.Models.ViewModels;

public sealed record CommentListRow(
    Guid Id,
    Guid TaskItemId,
    string AuthorName,
    string BodyPreview,
    DateTime CreatedAt);
