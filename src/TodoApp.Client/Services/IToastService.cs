namespace TodoApp.Client.Services;

public interface IToastService
{
    event Action? OnChange;
    IReadOnlyList<ToastMessage> Toasts { get; }
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowInfo(string message);
    void Remove(Guid id);
}
