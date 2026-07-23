using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CineCore.ViewModels.Base
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            // Tombol ga bisa diklik kalau lagi proses executing
            return !_isExecuting && (_canExecute == null || _canExecute());
        }

        // ── INI KUNCI ANTI HANG/DEADLOCK: Pakai 'async void' dan 'await' ──
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute();
            }
            catch (Exception ex)
            {
                // Kalau ada error di dalam command, WPF ga akan mati diam-diam
                System.Windows.MessageBox.Show($"Command Error: {ex.Message}", "System Error");
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
} 