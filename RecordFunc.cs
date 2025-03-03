using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;

namespace MyRepeater
{
    
    class RecordFunc
    {
        // 事件记录采用元组，记录相对于录制开始的时间戳、事件类型和事件数据
        private static List<(TimeSpan timestamp, string eventType, object eventData)> events = new List<(TimeSpan, string, object)>();
        private static IKeyboardMouseEvents? globalHook;
        // 记录录制开始的时刻
        private static DateTime recordStartTime;
        public TriggerMethod TriggerStop=new();
        private static int X;
        private static int Y;
        private static double screenWidth;
        private static double screenHeight;
        public bool isStopTriggered = false;
        public bool isRepeating = false;
        public void StartRecord()
        {
            MouseUtils.MoveMouseToCenter();
            Console.WriteLine("开始录制...");
            events.Clear();
            recordStartTime = DateTime.Now;  // 设置录制起点

            // 订阅全局键盘和鼠标事件
            globalHook = Hook.GlobalEvents();
            globalHook.KeyDown += GlobalHook_KeyDown;
            globalHook.KeyUp += GlobalHook_KeyUp;
            globalHook.MouseMove += GlobalHook_MouseMove;
            globalHook.MouseDownExt += GlobalHook_MouseDownExt;
            globalHook.MouseUpExt += GlobalHook_MouseUpExt;
        }

        public void EndRecord()
        {
            Console.WriteLine("停止录制...");
            if (globalHook != null)
            {
                globalHook.KeyDown -= GlobalHook_KeyDown;
                globalHook.KeyUp -= GlobalHook_KeyUp;
                globalHook.MouseMove -= GlobalHook_MouseMove;
                globalHook.MouseDownExt -= GlobalHook_MouseDownExt;
                globalHook.MouseUpExt -= GlobalHook_MouseUpExt;
                var elapsed = DateTime.Now - recordStartTime;
                events.Add((elapsed, "End", 0));
                globalHook.Dispose();
            }
        }
        public void Reset()
        {
            events.Clear();
        }
        private void GlobalHook_KeyDown(object? sender, KeyEventArgs e)
        {
            var elapsed = DateTime.Now - recordStartTime;
            if (e.KeyCode == Keys.F12)
            {
                TriggerStop.StopTrigger();
                return;
            }
            events.Add((elapsed, "KeyDown", e.KeyCode));
        }

        private void GlobalHook_Stop(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12 && globalHook!=null && !isStopTriggered)
            {
                isStopTriggered = true;
                TriggerStop.StopTrigger();
                return;
            }
        }
        private static void GlobalHook_KeyUp(object? sender, KeyEventArgs e)
        {
            var elapsed = DateTime.Now - recordStartTime;
            events.Add((elapsed, "KeyUp", e.KeyCode));
        }

        private static void GlobalHook_MouseMove(object? sender, MouseEventArgs e)
        {
            var elapsed = DateTime.Now - recordStartTime;
            // 记录鼠标坐标，此事件触发频率较高，可记录全部事件或在实际项目中进行采样处理
            events.Add((elapsed, "MouseMove", new { e.X, e.Y }));
        }

        private static void GlobalHook_MouseDownExt(object? sender, MouseEventExtArgs e)
        {
            var elapsed = DateTime.Now - recordStartTime;
            events.Add((elapsed, "MouseDown", new { e.X, e.Y, e.Button }));
        }

        private static void GlobalHook_MouseUpExt(object? sender, MouseEventExtArgs e)
        {
            var elapsed = DateTime.Now - recordStartTime;
            events.Add((elapsed, "MouseUp", new { e.X, e.Y, e.Button }));
        }
        public void PlaybackEvents()
        {
            Console.WriteLine("开始回放...");


            if (events.Count == 0)
            {
                Console.WriteLine("没有录制的事件。");
                return;
            }

            InputSimulator inputsimulator = new InputSimulator();
            globalHook = Hook.GlobalEvents();
            globalHook.KeyDown += GlobalHook_Stop;
            isRepeating = true;

            // 从零时刻开始
            TimeSpan lastTimestamp = TimeSpan.Zero;

            foreach (var record in events)
            {
                TimeSpan delay = record.timestamp - lastTimestamp;
                if (delay.TotalMilliseconds > 0)
                    Thread.Sleep((int)delay.TotalMilliseconds);

                lastTimestamp = record.timestamp;
                switch (record.eventType)
                {
                    case "KeyDown":
                        inputsimulator.Keyboard.KeyDown((VirtualKeyCode)(int)(long)record.eventData);
                        break;
                    case "KeyUp":
                        inputsimulator.Keyboard.KeyUp((VirtualKeyCode)(int)(long)record.eventData);
                        break;
                    case "MouseMove":
                        dynamic pos = record.eventData;
                        // 这里使用相对移动
                        //inputsimulator.Mouse.MoveMouseBy((int)(pos.X-X), (int)(pos.Y-Y));
                        //X= pos.X;
                        //Y= pos.Y;
                        inputsimulator.Mouse.MoveMouseTo((double)(pos.X*65535/screenWidth),(double) (pos.Y*65535/screenHeight));
                        break;
                    case "MouseDown":
                        dynamic downData = record.eventData;
                        if (downData.Button == MouseButtons.Left)
                            inputsimulator.Mouse.LeftButtonDown();
                        else if (downData.Button == MouseButtons.Right)
                            inputsimulator.Mouse.RightButtonDown();
                        break;
                    case "MouseUp":
                        dynamic upData = record.eventData;
                        if (upData.Button == MouseButtons.Left)
                            inputsimulator.Mouse.LeftButtonUp();
                        else if (upData.Button == MouseButtons.Right)
                            inputsimulator.Mouse.RightButtonUp();
                        break;
                    default:
                        break;
                }
            }
            isRepeating = false;
            Console.WriteLine("回放结束.");
        }

        public void StopPlaybackEvent()
        {
            if (globalHook != null)
            {
                globalHook.KeyDown -= GlobalHook_Stop;
                globalHook.Dispose();
            }
        }


        public void SaveEventToFile(string FilePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(events, Formatting.Indented);
                File.WriteAllText(FilePath, json);
                events.Clear();
                Console.WriteLine($"录制结果已保存到 {FilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存文件时出错: {ex.Message}");
            }
        }

        public void LoadEventsFromFile(string filePath)
        {
            try
            {
                events.Clear();
                string json = File.ReadAllText(filePath);
                events = JsonConvert.DeserializeObject<List<(TimeSpan, string, object)>>(json) ?? new List<(TimeSpan, string, object)>();
                Console.WriteLine("事件数据已加载.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载文件时出错: {ex.Message}");
            }
        }

        public class MouseUtils
        {
            // 导入 user32.dll 中的 SetCursorPos 函数
            [DllImport("user32.dll")]
            public static extern bool SetCursorPos(int x, int y);

            public static void MoveMouseToCenter()
            {
                // 获取虚拟屏幕的宽度和高度（适用于多显示器环境）
                screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

                // 计算屏幕中心
                int centerX = (int)(screenWidth / 2);
                int centerY = (int)(screenHeight / 2);
                X = centerX;
                Y = centerY;

                // 移动鼠标到屏幕中心
                SetCursorPos(centerX, centerY);
            }

        }



    }

    public class TriggerMethod
    {
        public event Action Stop;
        public void StopTrigger()
        {
            // 触发事件
            Stop?.Invoke();
        }
    }
}
