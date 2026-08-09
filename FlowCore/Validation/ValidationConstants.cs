namespace FlowCore.Validation;

public static class ValidationConstants
{
    public const int ProjectNameMaxLength = 200;
    public const int BoardNameMaxLength = 60;
    public const int TagNameMaxLength = 50;
    public const int WorkspaceNameMaxLength = 80;
    public const int DescriptionMaxLength = 2000;
    public const int TaskTitleMaxLength = 200;
    public const int CommentBodyMaxLength = 4000;
    public const int UserNameMaxLength = 100;
    public const int UserEmailMaxLength = 200;

    public const string HexColor = @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$";
    public const string HexColorError = "Color must be a hex like #f00 or #ff0000.";
}
