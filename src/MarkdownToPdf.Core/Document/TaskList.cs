namespace MarkdownToPdf.Core.Document;

public sealed class TaskList : IDocumentElement
{
    public IReadOnlyList<TaskItem> Items { get; }

    public TaskList(IReadOnlyList<TaskItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            throw new ArgumentException("Task list must contain at least one item.", nameof(items));
        }

        Items = [.. items];
    }
}
