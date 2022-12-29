using FaceComparer_storage.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace FaceComparer_storage
{
    /// <summary>
    /// Логика взаимодействия для DBManageWindow.xaml
    /// </summary>
    public partial class DBManageWindow : Window
    {
        private int _selectedImageIndex;
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<ImageDetails> CachedImages { get; } = new();
        public ICommand RemoveImageCommand { get; private set; }

        public DBManageWindow()
        {
            using (var db = new ImagesContext())
            {
                foreach (var imageDetails in db.ImagesDetails)
                {
                    CachedImages.Add(imageDetails);
                }
            }

            RemoveImageCommand = new RelayCommand(_ => { RemoveImage(); });

            InitializeComponent();

            DataContext = this;
        }

        public System.Drawing.Image ByteArrayToImage(byte[] bytesArr)
        {
            using MemoryStream memstr = new(bytesArr);
            return System.Drawing.Image.FromStream(memstr);
        }

        private void RemoveImage()
        {
            try
            {
                var imageInfo = CachedImages[SelectedImageIndex];
                using (var db = new ImagesContext())
                {
                    var selectedImage = db.Images.Where(x => x.Id == imageInfo.Id).Include(x => x.Details).First();
                    if (selectedImage == null)
                    {
                        return;
                    }
                    db.ImagesDetails.Remove(selectedImage.Details);
                    db.Images.Remove(selectedImage);
                    db.SaveChanges();
                    CachedImages.RemoveAt(SelectedImageIndex);
                }
            }
            catch (Exception) {}
        }

        public int SelectedImageIndex
        {
            get { return _selectedImageIndex; }
            set { _selectedImageIndex = value; OnPropertyChanged(); }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
