using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Xbox.Tools.Shared
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PDBPathSelector : Window
    {
        public string NTSymbolPathString
        {
            get 
            {
                if (mNTSymbolPath == null)
                    return "<Not set>";
                else
                    return mNTSymbolPath;
            }
        }

        public ObservableCollection<string> Paths
        {
            get
            {
                return mPaths;
            }
            set
            {
                mPaths = value;
            }

        }

        public bool UseNTSymbolPath
        {
            get
            {
                return mUseNTSymbolPath;
            }
            set
            {
                mUseNTSymbolPath = value;
            }
        }

        public bool NTSymbolPathSet
        {
            get
            {
                return mNTSymbolPath != null;
            }
        }

        public List<string> GetResultantPathList()
        {
            return new List<string>(mPaths);
        }

        public PDBPathSelector(List<string> currentPaths, bool useNTSymbolPath)
        {
            // Don't start with dups please
            System.Diagnostics.Debug.Assert(currentPaths.Count == currentPaths.Distinct().ToList().Count);

            mNTSymbolPath = Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH");
            mPaths = new ObservableCollection<string>(currentPaths);

            mUseNTSymbolPath = useNTSymbolPath;
            InitializeComponent();

            DataContext = this;
        }

        private string mNTSymbolPath;
        private bool mUseNTSymbolPath;

        private ObservableCollection<string> mPaths;

        private void AddFolder(object sender, RoutedEventArgs e)
        {
            string newPath = NewPathEntry.Text;
            if (String.IsNullOrEmpty(newPath))
                return;
            if (!Directory.Exists(newPath))
            {
                MessageBox.Show("Directory " + newPath + " does not exist. Not added.");
                return;
            }
            if (!mPaths.Contains(newPath))
                mPaths.Add(newPath);
            else
                MessageBox.Show("Directory " + newPath + " already in path list.");
        }

        private void FolderSelect(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                NewPathEntry.Text = dialog.SelectedPath;
            }
        }

        private void NTSymbolPathChecked(object sender, RoutedEventArgs e)
        {
            mUseNTSymbolPath = !mUseNTSymbolPath;
        }

        private void OKClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void DeletePath(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;
            string pathToRemove = button.DataContext as string;

            if (pathToRemove == null)
                return;

            mPaths.Remove(pathToRemove);
        }
    }
}
