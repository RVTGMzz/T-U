namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Small cosmetic-only recency memory for Party Banter. It never persists into the save and never
/// changes gameplay state. Scores are penalties: lower means a candidate has been heard less recently.
/// </summary>
internal sealed class BanterMemoryTracker
{
    public const int MaxRecentExchangeIds = 12;
    public const int MaxRecentSpeakers = 8;
    public const int RecentExchangePenalty = 100;
    public const int RecentSpeakerPenalty = 18;

    private readonly Queue<string> _recentExchangeIds = new();
    private readonly Queue<string> _recentSpeakers = new();
    private readonly Dictionary<string, int> _sessionUseCounts = new(StringComparer.OrdinalIgnoreCase);

    public int RecentExchangeCount => _recentExchangeIds.Count;
    public int RecentSpeakerCount => _recentSpeakers.Count;

    public void Reset()
    {
        _recentExchangeIds.Clear();
        _recentSpeakers.Clear();
        _sessionUseCounts.Clear();
    }

    public int Score(string exchangeId, params string?[] speakers)
    {
        int penalty = 0;
        if (_recentExchangeIds.Contains(exchangeId, StringComparer.OrdinalIgnoreCase))
            penalty += RecentExchangePenalty;

        string[] recent = _recentSpeakers.ToArray();
        foreach (string? speaker in speakers)
        {
            if (string.IsNullOrWhiteSpace(speaker))
                continue;

            for (int i = recent.Length - 1, distance = 0; i >= 0; i--, distance++)
            {
                if (!recent[i].Equals(speaker, StringComparison.OrdinalIgnoreCase))
                    continue;
                penalty += Math.Max(2, RecentSpeakerPenalty - distance * 3);
                break;
            }
        }

        if (_sessionUseCounts.TryGetValue(exchangeId, out int uses) && uses > 0)
            penalty += Math.Min(20, uses * 2);
        return penalty;
    }

    public void Record(string exchangeId, params string?[] speakers)
    {
        if (!string.IsNullOrWhiteSpace(exchangeId))
        {
            _recentExchangeIds.Enqueue(exchangeId);
            while (_recentExchangeIds.Count > MaxRecentExchangeIds)
                _recentExchangeIds.Dequeue();
            _sessionUseCounts[exchangeId] = _sessionUseCounts.TryGetValue(exchangeId, out int uses) ? uses + 1 : 1;
        }

        foreach (string? speaker in speakers)
        {
            if (string.IsNullOrWhiteSpace(speaker))
                continue;
            _recentSpeakers.Enqueue(speaker);
            while (_recentSpeakers.Count > MaxRecentSpeakers)
                _recentSpeakers.Dequeue();
        }
    }

    public string Describe()
    {
        string ids = _recentExchangeIds.Count == 0 ? "-" : string.Join(" > ", _recentExchangeIds);
        string speakers = _recentSpeakers.Count == 0 ? "-" : string.Join(" > ", _recentSpeakers);
        return $"recentIds={_recentExchangeIds.Count}/{MaxRecentExchangeIds} [{ids}]; recentSpeakers={_recentSpeakers.Count}/{MaxRecentSpeakers} [{speakers}]";
    }
}
