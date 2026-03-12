namespace TodoApp.Client.Services;

public enum ToastLevel
{
    Success,
    Error,
    Info
}

public class ToastMessage
{
    public string Message { get; set; } = string.Empty;
    public ToastLevel Level { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ToastService : IToastService
{
    public event Action? OnChange;

    private readonly object _lock = new();
    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts
    {
        get
        {
            lock (_lock)
            {
                return _toasts.ToList().AsReadOnly();
            }
        }
    }

    public void ShowSuccess(string message)
    {
        Show(message, ToastLevel.Success);
    }

    public void ShowError(string message)
    {
        Show(message, ToastLevel.Error);
    }

    public void ShowInfo(string message)
    {
        Show(message, ToastLevel.Info);
    }

    public void Remove(Guid id)
    {
        lock (_lock)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == id);
            if (toast != null)
            {
                _toasts.Remove(toast);
            }
        }
        OnChange?.Invoke();
    }

    public void RemoveExpired(TimeSpan lifetime)
    {
        bool removed;
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - lifetime;
            removed = _toasts.RemoveAll(t => t.CreatedAt <= cutoff) > 0;
        }
        if (removed)
        {
            OnChange?.Invoke();
        }
    }

    private void Show(string message, ToastLevel level)
    {
        lock (_lock)
        {
            _toasts.Add(new ToastMessage { Message = message, Level = level });
        }
        OnChange?.Invoke();
    }
}
