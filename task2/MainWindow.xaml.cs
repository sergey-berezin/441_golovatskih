
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace FaceComparer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ICommand Load { get; private set; }
        public ICommand Compare { get; private set; }
        public ICommand Cancel { get; private set; }
        public DataGrid2DData DataGrid2D { get; private set; }
        public bool ComparisonRunning { get; set; }

        public MainWindow()
        {
            imagePaths = new List<string>();
            tokenSource = new CancellationTokenSource();
            ComparisonRunning = false;
            Load = new RelayCommand(_ => { LoadImages(); }, _ => { return !ComparisonRunning; });
            Compare = new RelayCommand(_ => { CompareImagesAsync(); }, _ => { return !ComparisonRunning && imagePaths.Count > 1; });
            Cancel = new RelayCommand(_ => { CancelComparison(); }, _ => { return ComparisonRunning; });
            DataGrid2D = new DataGrid2DData();
            InitializeComponent();
            DataContext = this;
        }

        private void LoadImages()
        {
            Clear();
            var dialog = new OpenFileDialog
            {
                Filter = "Images (*.PNG;*.png)|*.PNG;*.png",
                Multiselect = true,
                Title = "Choose images to compare"
            };
            var response = dialog.ShowDialog();
            var fileNames = new List<string>();
            if (response == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var path in dialog.FileNames)
                {
                    imagePaths.Add(path);
                    fileNames.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                }
            }
            else
            {
                imagePaths.Clear();
                return;
            }
            if (fileNames.Count <= 1)
            {
                imagePaths.Clear();
                return;
            }
            DataGrid2D.ColumnHeaders = fileNames.ToArray();
            DataGrid2D.RowHeaders = fileNames.ToArray();
            var data = new string[fileNames.Count, fileNames.Count];
            for (int i = 0; i < fileNames.Count; ++i)
            {
                for (int j = 0; j < fileNames.Count; ++j)
                {
                    if (i == j)
                    {
                        data[i, j] = string.Empty;
                    }
                    else
                    {
                        data[i, j] = $"({0}; {0})";
                    }
                }
            }
            DataGrid2D.Data2D = data;
            ((RelayCommand)Compare).RaiseCanExecuteChanged();
        } 

        private async void CompareImagesAsync()
        {
            ComparisonRunning = true;
            RiseAllCanExecuteChanged();
            tokenSource = new CancellationTokenSource();
            var data = new string[imagePaths.Count, imagePaths.Count];
            var progressStep = 100.0 / ((imagePaths.Count * imagePaths.Count) - imagePaths.Count);

            CmpPB.Value = 0;
            CmpPB.Foreground = Brushes.LimeGreen;
            for (int i = 0; i < imagePaths.Count; ++i)
            {
                for (int j = 0; j < imagePaths.Count; ++j)
                {
                    if (i == j)
                    {
                        data[i, j] = string.Empty;
                        continue;
                    }
                    try
                    {
                        if (tokenSource.IsCancellationRequested)
                        {
                            tokenSource.Token.ThrowIfCancellationRequested();
                        }
                        //Можно было бы заранее перевести все изображения в байты
                        //Но тогда есть вероятность заполнить оперативную память
                        var image1 = await File.ReadAllBytesAsync(imagePaths[i]);
                        var image2 = await File.ReadAllBytesAsync(imagePaths[j]);
                        var cmp = await ArcFacePackage.ArcFacePackage.ProcessAsync(image1, image2, tokenSource.Token);
                        data[i, j] = $"({cmp[0]}; {cmp[1]})";
                        CmpPB.Value += progressStep;
                    }
                    catch (OperationCanceledException) { }
                }
            }
            if (!tokenSource.IsCancellationRequested)
            {
                CmpPB.Value = 100;
                DataGrid2D.Data2D = data;
            }
            else
            {
                CmpPB.Foreground = Brushes.PaleVioletRed;
            }
            ComparisonRunning = false;
            RiseAllCanExecuteChanged();
        }

        private void CancelComparison()
        {
            tokenSource.Cancel();
        }

        private void Clear()
        {
            CmpPB.Value = 0;
            DataGrid2D = new DataGrid2DData();
            MainDataGrid.DataContext = DataGrid2D;
            imagePaths.Clear();
            ((RelayCommand)Compare).RaiseCanExecuteChanged();
        }

        private void RiseAllCanExecuteChanged()
        {
            ((RelayCommand)Load).RaiseCanExecuteChanged();
            ((RelayCommand)Compare).RaiseCanExecuteChanged();
            ((RelayCommand)Cancel).RaiseCanExecuteChanged();
        }

        private CancellationTokenSource tokenSource;
        private List<string> imagePaths;
    }
}
