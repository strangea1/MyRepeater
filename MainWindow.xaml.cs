using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static MyRepeater.RecordFunc;

namespace MyRepeater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    
    public class Record
    {
        public string RecordName {  get; set; }=string.Empty;
        public Record(string Name) { RecordName = Name; }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }
        public ObservableCollection<Record> RecordList = new();
        string SelectedItem = string.Empty;
        private string directoryPath;
        RecordFunc RecFunc = new RecordFunc();
        bool Repeating = false;
        private CancellationTokenSource _cancellationTokenSource;


        private void Init()
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 组合成新的文件夹路径
            directoryPath = System.IO.Path.Combine(currentDirectory, "data");

            // 如果data文件夹不存在，则创建它
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            Statusbar.Content = "Welcome to MyRepeater!";
            RecFunc.TriggerStop.Stop += StopTrigger;
            GetJsonFileNames(directoryPath);
            Record.ItemsSource = RecordList;
            // 获取当前程序所在的目录
        }

        private void StopTrigger()
        {
            if (!Repeating) { EndRecord_Click(null,null); }
            else { EndRepeat_Click(null, null); }
        }

        private void StartRecord_Click(object sender, RoutedEventArgs e)
        {
            Statusbar.Content= "录制中……（按下F12或点击结束录制按钮以结束录制）";
            RecFunc.isStopTriggered = false;
            RecFunc.StartRecord();
        }

        private void EndRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!Repeating && !RecFunc.isRepeating)
            {
                Dispatcher.Invoke(() =>
                {
                    Statusbar.Content = "Welcome to MyRepeater!";
                    RecFunc.EndRecord();
                    string name = NameWindow.Work();
                    if (name != null)
                    {
                        RecFunc.SaveEventToFile(directoryPath + '\\' + name + ".json");
                        RecFunc.Reset();
                        GetJsonFileNames(directoryPath);
                        Record.ItemsSource = RecordList;
                    }
                });
            }
            
        }

        private async void StartRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                ShowError();
                return;
            }
            Statusbar.Content = "操作录像回放中……（按下F12或点击结束回放按钮以结束回放）";
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            RecFunc.isStopTriggered = false;
            if (Directory.Exists(directoryPath))
            {
                // 获取所有 .json 文件
                string[] jsonFiles = Directory.GetFiles(directoryPath, "*.json");

                // 遍历所有文件并输出文件名
                foreach (string file in jsonFiles)
                {
                    // 获取文件名而不是完整路径
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName ==SelectedItem)
                    {
                        Repeating = true;
                        MouseUtils.MoveMouseToCenter();
                        await Task.Run(() =>
                        {
                            while (Repeating)
                            {
                                RecFunc.LoadEventsFromFile(file);
                                RecFunc.PlaybackEvents();
                            }
                        }, token);
                        
                    }
                }
            }
            else
            {
                Console.WriteLine("指定的目录不存在.");
            }
        }

        private void EndRepeat_Click(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
            Dispatcher.Invoke(() =>
            {
                Statusbar.Content = "Welcome to MyRepeater!";
            });
            RecFunc.StopPlaybackEvent();
            Repeating =false;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedItem.Length==0) 
            {
                ShowError();
                return;
            }
            if (Directory.Exists(directoryPath))
            {
                // 获取所有 .json 文件
                string[] jsonFiles = Directory.GetFiles(directoryPath, "*.json");

                // 遍历所有文件并输出文件名
                foreach (string file in jsonFiles)
                {
                    // 获取文件名而不是完整路径
                    string fileName = System.IO.Path.GetFileName(file);
                    if (fileName == SelectedItem) 
                    {
                        File.Delete(file);
                        break;
                    }
                }
                GetJsonFileNames(directoryPath);
            }
            else
            {
                Console.WriteLine("指定的目录不存在.");
            }
        }
        public void GetJsonFileNames(string directoryPath)
        {
            try
            {
                // 确保目录存在
                if (Directory.Exists(directoryPath))
                {
                    // 获取所有 .json 文件
                    string[] jsonFiles = Directory.GetFiles(directoryPath, "*.json");
                    RecordList.Clear();

                    // 遍历所有文件并输出文件名
                    foreach (string file in jsonFiles)
                    {
                        // 获取文件名而不是完整路径
                        string fileName = System.IO.Path.GetFileName(file);
                        RecordList.Add(new Record(fileName));
                    }
                }
                else
                {
                    Console.WriteLine("指定的目录不存在.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发生错误: {ex.Message}");
            }
        }

        private void Record_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(Record.SelectedItem != null)
            { 
                var selectedRecord = Record.SelectedItem as Record;
                SelectedItem = selectedRecord?.RecordName.ToString() ?? string.Empty; 
            }
        }

        private void ShowError()
        {
            MessageBox.Show("请先选择一条记录！");
        }
    }
}

