using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MyRepeater
{
    /// <summary>
    /// NameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NameWindow : Window
    {
        string name;
        public NameWindow()
        {
            InitializeComponent();
        }


        private void Ensure_Click(object sender, RoutedEventArgs e)
        {
            name = NameOfRecord.Text;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("文件名不能为空！");
            }
            else
            {
                this.Close(); // 关闭窗口
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public static string Work()
        {
            string tmp = string.Empty;

            NameWindow win = new();

            win.ShowDialog();

            tmp = win.name;

            return tmp;
        }

        private void NameOfRecord_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Ensure_Click(sender, e); }
        }
    }
}
