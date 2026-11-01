using System.Text;

namespace SFA_WebAPI.Services
{
    public sealed class TextFileBotQuestionLogService : IBotQuestionLogService
    {
        private readonly ILogger<TextFileBotQuestionLogService> _logger;
        private readonly string _logPath;
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public TextFileBotQuestionLogService(
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<TextFileBotQuestionLogService> logger)
        {
            _logger = logger;

            var configuredPath = configuration["BotQuestionLogPath"];
            _logPath = string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(environment.ContentRootPath, "data", "bot-question-log.txt")
                : Path.IsPathRooted(configuredPath)
                    ? configuredPath
                    : Path.Combine(environment.ContentRootPath, configuredPath);
        }

        public async Task LogQuestionAsync(
            string question,
            string answer,
            string? ipAddress,
            string? userAgent,
            CancellationToken cancellationToken = default)
        {
            var safeQuestion = Sanitize(question);
            var safeAnswer = Sanitize(answer);
            if (string.IsNullOrWhiteSpace(safeQuestion))
            {
                throw new ArgumentException("Question message must not be empty.", nameof(question));
            }

            var directory = Path.GetDirectoryName(_logPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException($"Invalid log file path configured: {_logPath}");
            }

            var line = string.Join('\t',
                DateTimeOffset.UtcNow.ToString("O"),
                Sanitize(ipAddress),
                Sanitize(userAgent),
                safeQuestion,
                safeAnswer) + Environment.NewLine;

            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                Directory.CreateDirectory(directory);
                await File.AppendAllTextAsync(_logPath, line, Encoding.UTF8, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write bot question log to {LogPath}", _logPath);
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private static string Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
