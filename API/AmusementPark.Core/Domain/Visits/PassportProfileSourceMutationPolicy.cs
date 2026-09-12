namespace AmusementPark.Core.Domain.Visits;

public static class PassportProfileSourceMutationPolicy
{
    public static bool CanChangeCompletedVisitProjection(VisitStatus status)
    {
        return status == VisitStatus.Completed;
    }

    public static bool CanChangeCompletedVisitProjection(
        VisitStatus? previousStatus,
        VisitStatus nextStatus)
    {
        return previousStatus == VisitStatus.Completed
            || nextStatus == VisitStatus.Completed;
    }
}
