namespace SFA_WebAPI.Services
{
    public interface IBotQuestionLogService
    {
        Task LogQuestionAsync(
            string question,
            string answer,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default);
    }
}
