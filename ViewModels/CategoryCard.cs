namespace TouchScreenPOS.ViewModels;

public sealed class CategoryCard
{
    public int? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public int SortOrder { get; init; }
}
