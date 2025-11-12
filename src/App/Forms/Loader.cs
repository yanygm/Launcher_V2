using Launcher.App.Utility;

namespace Launcher.App.Forms
{
    public partial class Loader : Form
    {
        private readonly System.Windows.Forms.Timer animationTimer;

        public Loader()
        {
            InitializeComponent();

            // 在构造函数中初始化定时器（在 UI 线程运行）
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50; // 25ms 每次 tick，可根据需要调整
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            StartAnimation();

            // load Data files (放到后台线程执行，避免阻塞 UI)
            Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("读取Data文件...");
                    var packFolderManager = KartRhoFile.Dump(Path.GetFullPath(Path.Combine(Program.RootDirectory, @"Data\aaa.pk")));
                    if (packFolderManager == null)
                    {
                        // MsgErrorReadData 可能会弹窗，必须回到 UI 线程调用
                        this.Invoke(() => Utils.MsgErrorReadData());
                        return;
                    }
                    packFolderManager.Reset();
                    Console.WriteLine("Data文件读取完成!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"读取Data文件时出错: {ex.Message}");
                }
                finally
                {
                    // 操作完成后停止动画并进行 UI 收尾并关闭窗体
                    try
                    {
                        this.Invoke(() =>
                        {
                            try
                            {
                                animationTimer.Stop();
                                animationTimer.Dispose();
                            }
                            catch { }

                            try
                            {
                                ProgressBar.Value = ProgressBar.Minimum;
                            }
                            catch { }

                            Utils.PrintDivLine();

                            try
                            {
                                // 先 Close，然后 Dispose（双重保险，Close 之后窗体会被释放，Dispose 可能抛异常，故包裹 try）
                                this.Close();
                                this.Dispose();
                            }
                            catch { }
                        });
                    }
                    catch
                    {
                        // 如果窗体已关闭或 Invoke 失败，吞掉异常以避免崩溃
                    }
                }
            });
        }

        private void StartAnimation()
        {
            // 启动定时器（UI 线程），Tick 时安全更新 ProgressBar
            if (!animationTimer.Enabled)
                animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 循环进度条：不断增加 Value，超过 Maximum 时回到 Minimum（可保留超出的步长或直接回到 Minimum）
                int step = 5; // 每次增加的步长，可按需调整
                int next = ProgressBar.Value + step;
                if (next > ProgressBar.Maximum)
                {
                    // 超出最大值后从 Minimum 重新开始
                    ProgressBar.Value = ProgressBar.Minimum;
                }
                else
                {
                    ProgressBar.Value = next;
                }
            }
            catch { }
        }
    }
}
