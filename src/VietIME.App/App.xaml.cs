using System.Drawing;
using System.Windows;
using VietIME.Core.Engines;
using VietIME.Hook;

namespace VietIME.App;

/// <summary>
/// Ứng dụng chính VietIME - Bộ gõ tiếng Việt cho Windows
/// </summary>
public partial class App : System.Windows.Application
{
    private KeyboardHook? _hook;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private MainWindow? _settingsWindow;
    
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Khởi tạo keyboard hook
        _hook = new KeyboardHook();
        _hook.Engine = new TelexEngine();
        _hook.EnabledChanged += Hook_EnabledChanged;
        _hook.Error += Hook_Error;
        _hook.Install();
        
        // Tạo system tray icon
        CreateTrayIcon();
        
        // Hiển thị thông báo
        ShowBalloonTip("VietIME đã khởi động", "Nhấn Ctrl+Shift để bật/tắt");
    }
    
    private void Application_Exit(object sender, ExitEventArgs e)
    {
        // Dọn dẹp resources
        _hook?.Dispose();
        _trayIcon?.Dispose();
    }
    
    private void CreateTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = CreateIcon(),
            Text = "VietIME - Bật (Telex)",
            Visible = true
        };
        
        // Context menu
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        
        // Toggle item
        var toggleItem = new System.Windows.Forms.ToolStripMenuItem("✓ Bật VietIME");
        toggleItem.Click += (s, e) => ToggleIME();
        contextMenu.Items.Add(toggleItem);
        
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        // Kiểu gõ
        var telexItem = new System.Windows.Forms.ToolStripMenuItem("● Telex");
        telexItem.Click += (s, e) => SetEngine("Telex");
        contextMenu.Items.Add(telexItem);
        
        var vniItem = new System.Windows.Forms.ToolStripMenuItem("○ VNI");
        vniItem.Click += (s, e) => SetEngine("VNI");
        contextMenu.Items.Add(vniItem);
        
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        // Settings
        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("⚙ Cài đặt...");
        settingsItem.Click += (s, e) => ShowSettings();
        contextMenu.Items.Add(settingsItem);
        
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        // Exit
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("✕ Thoát");
        exitItem.Click += (s, e) => Shutdown();
        contextMenu.Items.Add(exitItem);
        
        _trayIcon.ContextMenuStrip = contextMenu;
        
        // Double click để toggle
        _trayIcon.DoubleClick += (s, e) => ToggleIME();
    }
    
    private Icon CreateIcon()
    {
        // Tạo icon đơn giản bằng code
        // Trong production nên dùng file .ico
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            
            // Nền xanh nếu bật, xám nếu tắt
            var bgColor = (_hook?.IsEnabled ?? true) ? Color.FromArgb(33, 150, 243) : Color.Gray;
            using var brush = new SolidBrush(bgColor);
            g.FillEllipse(brush, 2, 2, 28, 28);
            
            // Chữ "V" trắng
            using var font = new Font("Arial", 16, System.Drawing.FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            g.DrawString("V", font, textBrush, 6, 4);
        }
        
        return Icon.FromHandle(bitmap.GetHicon());
    }
    
    private void UpdateTrayIcon()
    {
        if (_trayIcon == null) return;
        
        _trayIcon.Icon = CreateIcon();
        
        var enabled = _hook?.IsEnabled ?? false;
        var engineName = _hook?.Engine?.Name ?? "Telex";
        
        _trayIcon.Text = enabled 
            ? $"VietIME - Bật ({engineName})" 
            : "VietIME - Tắt";
        
        // Update menu
        if (_trayIcon.ContextMenuStrip?.Items[0] is System.Windows.Forms.ToolStripMenuItem toggleItem)
        {
            toggleItem.Text = enabled ? "✓ Bật VietIME" : "○ Bật VietIME";
        }
    }
    
    private void ToggleIME()
    {
        if (_hook != null)
        {
            _hook.IsEnabled = !_hook.IsEnabled;
        }
    }
    
    private void SetEngine(string engineName)
    {
        if (_hook == null) return;
        
        _hook.Engine = engineName switch
        {
            "VNI" => new VniEngine(),
            _ => new TelexEngine()
        };
        
        UpdateTrayIcon();
        
        // Update menu items
        if (_trayIcon?.ContextMenuStrip != null)
        {
            foreach (var item in _trayIcon.ContextMenuStrip.Items)
            {
                if (item is System.Windows.Forms.ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text.Contains("Telex"))
                        menuItem.Text = engineName == "Telex" ? "● Telex" : "○ Telex";
                    else if (menuItem.Text.Contains("VNI"))
                        menuItem.Text = engineName == "VNI" ? "● VNI" : "○ VNI";
                }
            }
        }
        
        ShowBalloonTip("Đổi kiểu gõ", $"Đã chuyển sang {engineName}");
    }
    
    private void ShowSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsVisible)
        {
            _settingsWindow = new MainWindow(_hook);
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }
    
    private void ShowBalloonTip(string title, string text)
    {
        _trayIcon?.ShowBalloonTip(2000, title, text, System.Windows.Forms.ToolTipIcon.Info);
    }
    
    private void Hook_EnabledChanged(object? sender, bool enabled)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateTrayIcon();
            ShowBalloonTip("VietIME", enabled ? "Đã bật" : "Đã tắt");
        });
    }
    
    private void Hook_Error(object? sender, string error)
    {
        Dispatcher.Invoke(() =>
        {
            System.Windows.MessageBox.Show(error, "VietIME - Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}

