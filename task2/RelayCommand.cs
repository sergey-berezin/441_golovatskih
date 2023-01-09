using System;
using System.Diagnostics;
using System.Windows.Input;

namespace FaceComparer
{
    public class RelayCommand : ICommand
    {
        #region Fields 
        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;
        #endregion // Fields 
        #region Constructors 
        public RelayCommand(Action<object> execute) : this(execute, null) { }
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute; _canExecute = canExecute;
        }
        #endregion // Constructors 
        #region ICommand Members 
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        //https://stackoverflow.com/questions/48279452/the-relaycommand-for-enable-disable-a-button-dont-work-like-expected
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
        public void Execute(object parameter) { _execute(parameter); }
        #endregion // ICommand Members 
    }
}
