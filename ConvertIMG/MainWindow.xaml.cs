using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace ConvertIMG
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

            //get folder path
            
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            string newLine = Environment.NewLine;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {

                textBox.Text += "You selected: " + dialog.FileName + newLine;
                ConverterIMG var = new ConverterIMG();
              
                DirectoryInfo selectedFolder = new DirectoryInfo(dialog.FileName);
                //show target directory
                statusLabel.Content = dialog.FileName;
                //after i pick the folder to work in, the app window free_es, says unresponding in the title
                //to fix it i decided to try launch heavy directory-walking-recursive loop with image processing
                //on a different thread, hopefully it won't free_ up the UI
                //Thread myNewThread = new Thread(() => WorkThreadFunction(selectedFolder));
               // myNewThread.Start();

                
                  var.WalkDirectoryTree(selectedFolder);
               // output list of low resultion files
                  textBox.Text += var.printSmallimageNames();


            }


           
        
        }
        //try only 1 file
        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".png";
            dlg.Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif|WEBP Files (*.webp)|*.webp";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                string newLine = Environment.NewLine;
                textBox.Text += "Selected: "+filename + newLine;

                ConverterIMG conv = new ConverterIMG();
                                
                    conv.VaryQualityLevel(filename);
               // conv.data2(this);
                textBox.Text += conv.printSmallimageNames();
            }

            
        }
        //
        public void WorkThreadFunction(DirectoryInfo path)
        {
            try
            {
                this.Dispatcher.Invoke(() =>
                {
                    ConverterIMG var = new ConverterIMG();
                    //  output list of low resultion files
                    textBox.Text += var.printSmallimageNames();
                    var.WalkDirectoryTree(path);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                   
            }
        }

      

    }

  
}

