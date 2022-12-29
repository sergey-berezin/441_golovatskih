using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FaceComparer_storage
{
    public class DataGrid2DData : INotifyPropertyChanged
    {
        public DataGrid2DData()
        {
            RowHeaders = new[]{ "" };
            ColumnHeaders = new[]{ "" };
            Data2D = new[,]
                {
                    { "" }
                };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string[] RowHeaders
        {
            get
            {
                return _rowHeaders;
            }
            set
            {
                _rowHeaders = value;
                OnPropertyChanged(nameof(RowHeaders));
            }
        }

        public string[] ColumnHeaders
        {
            get
            {
                return _columnHeaders;
            }
            set
            {
                _columnHeaders = value;
                OnPropertyChanged(nameof(ColumnHeaders));
            }
        }

        public string[,] Data2D
        {
            get
            {
                return _data2D;
            }
            set
            {
                _data2D = value;
                OnPropertyChanged(nameof(Data2D));
            }
        }

        public void OnColumnHeadersChanged()
        {
            OnPropertyChanged(nameof(ColumnHeaders));
        }

        public void OnRowHeadersChanged()
        {
            OnPropertyChanged(nameof(RowHeaders));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string[] _rowHeaders;
        private string[] _columnHeaders;
        private string[,] _data2D;
    }
}
