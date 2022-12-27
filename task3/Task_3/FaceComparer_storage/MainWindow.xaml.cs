using FaceComparer_storage.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Xml;

namespace FaceComparer_storage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ICommand Load { get; private set; }
        public ICommand Compare { get; private set; }
        public ICommand Cancel { get; private set; }
        public ICommand ManageDB { get; private set; }
        public DataGrid2DData DataGrid2D { get; private set; }
        public bool ComparisonRunning { get; set; }
        public ObservableCollection<ImageItem> CachedImages { get; } = new();

        public MainWindow()
        {
            imagePaths = new List<string>();
            tokenSource = new CancellationTokenSource();
            ComparisonRunning = false;
            Load = new RelayCommand(_ => { LoadImages(); }, _ => { return !ComparisonRunning; });
            Compare = new RelayCommand(_ => { CompareImagesAsync(); }, _ => { return !ComparisonRunning && imagePaths.Count > 1; });
            Cancel = new RelayCommand(_ => { CancelComparison(); }, _ => { return ComparisonRunning; });
            ManageDB = new RelayCommand(_ => { ManageDatabase(); }, _ => { return !ComparisonRunning; });
            DataGrid2D = new DataGrid2DData();
            using (var db = new ImagesContext())
            {
                foreach (var imageItem in db.Images)
                {
                    CachedImages.Add(imageItem);
                }
            }
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
            if (response == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var path in dialog.FileNames)
                {
                    imagePaths.Add(path);
                }
            }
            else
            {
                imagePaths.Clear();
                return;
            }
            if (imagePaths.Count <= 1)
            {
                imagePaths.Clear();
                return;
            }
            DataGrid2D.ColumnHeaders = imagePaths.ToArray();
            DataGrid2D.RowHeaders = imagePaths.ToArray();
            var data = new string[imagePaths.Count, imagePaths.Count];
            for (int i = 0; i < imagePaths.Count; ++i)
            {
                for (int j = 0; j < imagePaths.Count; ++j)
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

            ApplyTemplateOnHeaders();

            DataGrid2D.OnColumnHeadersChanged();
            DataGrid2D.OnRowHeadersChanged();

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
            CmpPB.Foreground = System.Windows.Media.Brushes.LimeGreen;
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
                        var image1 = await File.ReadAllBytesAsync(imagePaths[i], tokenSource.Token);
                        var image2 = await File.ReadAllBytesAsync(imagePaths[j], tokenSource.Token);

                        float[]? embeddings1 = null;
                        if (FindImageInDb(image1, out var imageItem1))
                        {
                            embeddings1 = new float[imageItem1.Embeddings.Length / 4];
                            Buffer.BlockCopy(imageItem1.Embeddings, 0, embeddings1, 0, imageItem1.Embeddings.Length);
                        }
                        else
                        {
                            embeddings1 = await ArcFacePackage.ArcFacePackage.GetEmbeddingsAsync(image1, tokenSource.Token);
                            AddEmbeddingsToDb(image1, embeddings1, imagePaths[i]);
                        }

                        float[]? embeddings2 = null;
                        if (FindImageInDb(image2, out var imageItem2))
                        {
                            embeddings2 = new float[imageItem2.Embeddings.Length / 4];
                            Buffer.BlockCopy(imageItem2.Embeddings, 0, embeddings2, 0, imageItem2.Embeddings.Length);
                        }
                        else
                        {
                            embeddings2 = await ArcFacePackage.ArcFacePackage.GetEmbeddingsAsync(image2, tokenSource.Token);
                            AddEmbeddingsToDb(image2, embeddings2, imagePaths[j]);
                        }

                        var cmp = ArcFacePackage.ArcFacePackage.ProcessEmbeddings(embeddings1, embeddings2);
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
                ApplyTemplateOnHeaders();
            }
            else
            {
                CmpPB.Foreground = System.Windows.Media.Brushes.PaleVioletRed;
            }
            ComparisonRunning = false;
            RiseAllCanExecuteChanged();
        }

        private void AddEmbeddingsToDb(byte[] image, float[] embeddings, string fullName)
        {
                var hash = GetHash(image);
                using (var db = new ImagesContext())
                {
                    var byteArray = new byte[embeddings.Length * 4];
                    Buffer.BlockCopy(embeddings, 0, byteArray, 0, byteArray.Length);

                    var imageDetails = new ImageDetails { Data = image, FullFileName = fullName };
                    var newImageItem = new ImageItem
                    {
                        Hash = hash,
                        Embeddings = byteArray,
                        Details = imageDetails
                    };
                    db.Add(newImageItem);
                    db.SaveChanges();
                }
        }

        private bool FindImageInDb(byte[] image, out ImageItem imageItem)
        {
            var hash = GetHash(image);
            using (var db = new ImagesContext())
            {
                var q = db.Images
                    .Where(x => x.Hash == hash)
                    .Include(x => x.Details)
                    .Where(x => Equals(x.Details.Data, image));
                if (q != null && q.Any())
                {
                    imageItem = q.First();
                }
                else
                {
                    imageItem = new ImageItem();
                    return false;
                }
            }
            return true;
        }

        private string GetHash(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(data).Select(x => x.ToString("X2")));
            }
        }


        public DataTemplate CreateTemplate(string path)
        {
            string markup = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">";
            markup += "<Grid Width=\"40\">";
            markup += "<Image Source =\"" + path + "\" Width=\"32\" Height=\"32\"/>";
            markup += "</Grid>";
            markup += "</DataTemplate>";

            StringReader stringReader = new StringReader(markup);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            return (DataTemplate)XamlReader.Load(xmlReader);
        }

        private DataGridRow GetRow(int index)
        {
            DataGridRow row = (DataGridRow)MainDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                MainDataGrid.UpdateLayout();
                MainDataGrid.ScrollIntoView(MainDataGrid.Items[index]);
                row = (DataGridRow)MainDataGrid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        private void CancelComparison()
        {
            tokenSource.Cancel();
        }

        private void ManageDatabase()
        {
            var manageWindow = new DBManageWindow();
            manageWindow.ShowDialog();
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

        private BitmapImage ToBitmapImage(string path)
        {
            var bmpImage = new Bitmap(path);
            BitmapImage bmImg = new();

            using (MemoryStream memStream = new())
            {
                bmpImage.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
                memStream.Position = 0;

                bmImg.BeginInit();
                bmImg.CacheOption = BitmapCacheOption.OnLoad;
                bmImg.UriSource = null;
                bmImg.StreamSource = memStream;
                bmImg.EndInit();
            }

            return bmImg;
        }

        public void ApplyTemplateOnHeaders()
        {
            for (int intCounter = 0; intCounter < MainDataGrid.Columns.Count; intCounter++)
            {
                MainDataGrid.Columns[intCounter].HeaderTemplate = CreateTemplate((string)(MainDataGrid.Columns[intCounter].Header));
            }
            MainDataGrid.RowHeaderWidth = 50;
            for (int i = 0; i < MainDataGrid.Items.Count; i++)
            {
                DataGridRow row = GetRow(i);
                row.HeaderTemplate = CreateTemplate((string)(MainDataGrid.Columns[i].Header));
            }
        }

        private CancellationTokenSource tokenSource;
        private List<string> imagePaths;
    }
}
