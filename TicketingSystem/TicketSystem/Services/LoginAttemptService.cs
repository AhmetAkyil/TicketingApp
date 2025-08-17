using System.Collections.Concurrent;

namespace TicketSystem.Services
{
    public class LoginAttemptService
    {
        private readonly ConcurrentDictionary<string, (int Count, DateTime LastAttempt)> _attempts = new();

        private const int MaxAttempts = 5;
        private readonly TimeSpan BlockDuration = TimeSpan.FromSeconds(30);

        public bool IsBlocked(string username)
        {
            Console.WriteLine($"[BLOCK CHECK] {username} - Attempts: {_attempts.GetValueOrDefault(username).Count}");

            if (_attempts.TryGetValue(username, out var data))
            {
                if (data.Count >= MaxAttempts && DateTime.Now - data.LastAttempt < BlockDuration)
                    return true;

                // 10 dakika geçmişse sıfırla
                if (DateTime.Now - data.LastAttempt >= BlockDuration)
                    _attempts[username] = (0, DateTime.Now);
            }

            return false;
        }

        public void RecordAttempt(string username, bool success)
        {
            Console.WriteLine($"[ATTEMPT] {username} - Success: {success}");

            if (success)
            {
                _attempts[username] = (0, DateTime.Now);
                return;
            }

            if (_attempts.TryGetValue(username, out var data))
            {
                _attempts[username] = (data.Count + 1, DateTime.Now);
            }
            else
            {
                _attempts[username] = (1, DateTime.Now);
            }
        }
    }
}
