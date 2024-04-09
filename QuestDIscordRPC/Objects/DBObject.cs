namespace QuestDiscordRPC.Objects;

public class DBObject
{
    public readonly int Id;
    public readonly string PackageName;
    public readonly string AppName;
    public readonly string? ImageURL;

    public DBObject() { }

    public DBObject(int id, string packageName, string appName, string? imageURL)
    {
        Id = id;
        PackageName = packageName;
        AppName = appName;
        ImageURL = imageURL;
    }
}