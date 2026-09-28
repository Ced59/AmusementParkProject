namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalWorkflowStageCount
{
    public HistoricalWorkflowStageCount(HistoricalEditorialWorkflowState stage, int resourceCount)
    {
        if (!Enum.IsDefined(stage) || resourceCount < 0)
        {
            throw new ArgumentException("The historical workflow stage count is invalid.");
        }

        this.Stage = stage;
        this.ResourceCount = resourceCount;
    }

    public HistoricalEditorialWorkflowState Stage { get; }

    public int ResourceCount { get; }
}
