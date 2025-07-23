namespace CornerShop.Models
{
    /// <summary>
    /// Result of a compensation action in saga orchestration
    /// </summary>
    public class CompensationResult
    {
        public string StepId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt);
    }
}
