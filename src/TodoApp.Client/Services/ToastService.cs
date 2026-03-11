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
}

public class ToastService
{
    public event Action? OnChange;

    private readonly List<ToastMessage> _toasts = new();

    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

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
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast != null)
        {
            _toasts.Remove(toast);
            OnChange?.Invoke();
        }
    }

    private void Show(string message, ToastLevel level)
    {
        _toasts.Add(new ToastMessage { Message = message, Level = level });
        OnChange?.Invoke();
    }
}
