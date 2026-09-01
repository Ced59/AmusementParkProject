namespace AmusementPark.Core.Domain.Ratings;

public sealed record CompetitionRankAssignment(
    int Position,
    int Rank);

/// <summary>
/// Attribue des rangs de compétition à une séquence de scores déjà ordonnée.
/// Les ex æquo partagent le rang de leur premier score et le rang suivant
/// reprend la position réelle dans la séquence (1, 1, 3, ...).
/// </summary>
public static class CompetitionRankCalculator
{
    public static IReadOnlyList<CompetitionRankAssignment> AssignOrderedRanks(
        RankingScopeDefinition scope,
        IEnumerable<double> orderedScores)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(orderedScores);

        List<CompetitionRankAssignment> assignments = new List<CompetitionRankAssignment>();
        int position = 0;
        int rank = 0;
        double? rankAnchorScore = null;
        foreach (double score in orderedScores)
        {
            if (double.IsNaN(score) || double.IsInfinity(score))
            {
                throw new ArgumentOutOfRangeException(nameof(orderedScores));
            }

            position++;
            if (!rankAnchorScore.HasValue || !scope.AreScoresTied(rankAnchorScore.Value, score))
            {
                rank = position;
                rankAnchorScore = score;
            }

            assignments.Add(new CompetitionRankAssignment(position, rank));
        }

        return assignments.AsReadOnly();
    }
}
