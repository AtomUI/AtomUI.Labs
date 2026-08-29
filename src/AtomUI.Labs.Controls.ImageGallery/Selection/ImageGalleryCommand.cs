using System.Windows.Input;

namespace AtomUI.Labs.Controls.ImageGallery.Selection;

internal sealed class ImageGalleryCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public ImageGalleryCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute ?? (() => true);
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute();

    public void Execute(object? parameter)
    {
        if (_canExecute())
        {
            _execute();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
