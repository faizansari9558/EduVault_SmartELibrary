using System.Collections.Concurrent;

namespace SmartELibrary.Services;

public interface IStudentSessionTracker
{
    bool CanStartSession(int studentId, string sessionId);
    bool TryStartSession(int studentId, string sessionId);
    void EndSession(int studentId, string? sessionId);
}

public class StudentSessionTracker : IStudentSessionTracker
{
    private readonly ConcurrentDictionary<int, string> activeSessions = new();

    public bool CanStartSession(int studentId, string sessionId)
    {
        return !activeSessions.TryGetValue(studentId, out var activeSessionId) || activeSessionId == sessionId;
    }

    public bool TryStartSession(int studentId, string sessionId)
    {
        while (true)
        {
            if (activeSessions.TryGetValue(studentId, out var activeSessionId))
            {
                return activeSessionId == sessionId;
            }

            if (activeSessions.TryAdd(studentId, sessionId))
            {
                return true;
            }
        }
    }

    public void EndSession(int studentId, string? sessionId)
    {
        if (!activeSessions.TryGetValue(studentId, out var activeSessionId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sessionId) || activeSessionId == sessionId)
        {
            activeSessions.TryRemove(studentId, out _);
        }
    }
}
