using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Linq;

namespace Desktop_client_api_kod.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // ✅ Tüm pencereye Drag & Drop aktif et
            SetupGlobalDragAndDrop();
        }

        /// <summary>
        /// Tüm pencere için Drag & Drop özelliğini aktif eder
        /// Dosya her yerden bırakılabilir
        /// </summary>
        private void SetupGlobalDragAndDrop()
        {
            // Window'a AllowDrop özelliğini ekle
            DragDrop.SetAllowDrop(this, true);
            
            // Drop event'ini dinle
            AddHandler(DragDrop.DropEvent, OnFilesDropped);
            
            Console.WriteLine("✅ Global Drag & Drop aktif edildi");
        }

        /// <summary>
        /// Dosya pencereye bırakıldığında çağrılır
        /// </summary>
        private void OnFilesDropped(object? sender, DragEventArgs e)
        {
            e.Handled = true;
            
            var files = e.Data.GetFileNames()?.ToList();
            
            if (files == null || !files.Any())
            {
                return;
            }
            
            Console.WriteLine($"\n📁 {files.Count} dosya MainWindow'a bırakıldı:");
            foreach (var file in files)
            {
                Console.WriteLine($"   - {file}");
            }
            
            // JobHistoryView'i bul ve upload işlemini başlat
            if (Content is JobHistoryView jobHistoryView)
            {
                jobHistoryView.HandleFilesDropped(files);
            }
            else
            {
                Console.WriteLine("⚠️ JobHistoryView bulunamadı, upload yapılamadı");
            }
        }
    }
}