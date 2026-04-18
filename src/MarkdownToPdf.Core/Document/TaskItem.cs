namespace MarkdownToPdf.Core.Document;

public sealed class TaskItem
{
    public IReadOnlyList<InlineRun> Inlines { get; }
    public bool IsChecked { get; }

    public TaskItem(IReadOnlyList<InlineRun> inlines, bool isChecked)
    {
        ArgumentNullException.ThrowIfNull(inlines);
        if (inlines.Count == 0)
        {
            throw new ArgumentException("Task item must contain at least one inline run.", nameof(inlines));
        }

        Inlines = [.. inlines];
        IsChecked = isChecked;
    }
}
