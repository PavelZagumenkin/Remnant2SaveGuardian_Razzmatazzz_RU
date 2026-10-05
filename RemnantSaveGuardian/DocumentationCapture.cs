using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RemnantSaveGuardian.Properties;
using RemnantSaveGuardian.Views.Pages;
using RemnantSaveGuardian.Views.Windows;

namespace RemnantSaveGuardian;

/// <summary>Reproducible captures of the actual WPF interface with isolated demonstration data.</summary>
internal static class DocumentationCapture
{
    public static bool Enabled { get; private set; }
    private static string output = "";
    public static bool IsRequested(string[] args) => args.Length == 2 && args[0] == "--screenshots";

    public static void Configure(string[] args)
    {
        Enabled = true;
        output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        // Never use the player's folders or persist documentation settings.
        var root = Path.Combine(output, ".demo");
        Settings.Default.SaveFolder = Path.Combine(root, "Saves");
        Settings.Default.BackupFolder = Path.Combine(root, "Backups");
        Settings.Default.GameFolder = Path.Combine(root, "Remnant2");
        foreach (var path in new[] { Settings.Default.SaveFolder, Settings.Default.BackupFolder, Settings.Default.GameFolder })
            Directory.CreateDirectory(path);
        Settings.Default.UpgradeRequired = false;
        Settings.Default.AutoCheckUpdate = false;
        Settings.Default.AutoBackup = false;
        Settings.Default.CreateLogFile = false;
        Settings.Default.Language = "ru";
        Settings.Default.Theme = "Dark";
        Settings.Default.TopMost = false;
        Settings.Default.EnableOpacity = false;
        Settings.Default.AutoHideNaviAndTitleBar = false;
        Settings.Default.WindowWidth = 1280;
        Settings.Default.WindowHeight = 720;
        Settings.Default.AnalyzerFontSize = 16;
        Settings.Default.ShowPossibleItems = true;
        Settings.Default.ShowCoopItems = true;
    }

    public static async Task RunAsync()
    {
        try
        {
            var missing = GameInfo.EventItem.Values.SelectMany(items => items)
                .Where(item => !System.Text.RegularExpressions.Regex.IsMatch(item.Name, "[А-Яа-яЁё]"))
                .Select(item => new { key = item.RawName, name = item.Name }).Distinct().ToArray();
            File.WriteAllText(Path.Combine(output, "localization-audit.json"),
                System.Text.Json.JsonSerializer.Serialize(missing));
            var window = Application.Current.Windows.OfType<MainWindow>().Single();
            window.ShowInTaskbar = false;
            window.Title = "Remnant Save Guardian RU — демонстрационные данные";
            await CaptureAsync(window, typeof(BackupsPage), "backups-ru.png");
            await CaptureAsync(window, typeof(WorldAnalyzerPage), "world-analyzer-ru.png");
            await CaptureAsync(window, typeof(SettingsPage), "settings-ru.png");
            File.WriteAllText(Path.Combine(output, "capture-success.txt"), "WPF: резервные копии, анализатор и настройки; ru; 1.4.3");
            window.Close();
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "capture-error.txt"), ex.ToString());
            Application.Current.Shutdown(1);
        }
    }

    private static async Task CaptureAsync(MainWindow window, Type page, string file)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        if (window.GetFrame().Content?.GetType() != page && !window.Navigate(page))
            throw new InvalidOperationException("Не удалось открыть страницу " + page.Name);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(350);
        // Native window backdrop is not part of the WPF visual tree. Use its theme color for export.
        var visual = (FrameworkElement)window.Content;
        visual.UpdateLayout();
        var width = (int)Math.Ceiling(visual.ActualWidth);
        var height = (int)Math.Ceiling(visual.ActualHeight);
        if (width < 100 || height < 100) throw new InvalidOperationException("Пустая страница");
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(Color.FromRgb(32, 32, 32)), null, new Rect(0, 0, width, height));
            context.DrawRectangle(new VisualBrush(visual), null, new Rect(0, 0, width, height));
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(drawing);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(output, file));
        encoder.Save(stream);
    }

    public static List<DocumentationBackup> Backups() => new()
    {
        new() { Name = "Перед боссом — Яэша", SaveDate = new DateTime(2026, 10, 5, 18, 30, 0), Progression = "Охотник, Медик (142)", Keep = true, Active = true },
        new() { Name = "Н'Эруд — Тёмный горизонт", SaveDate = new DateTime(2026, 10, 5, 17, 15, 0), Progression = "Страж, Инженер (138)", Keep = true },
        new() { Name = "Лосомн — Покинутый берег", SaveDate = new DateTime(2026, 10, 4, 21, 45, 0), Progression = "Ритуалист, Алхимик (125)" },
        new() { Name = "Начало приключения", SaveDate = new DateTime(2026, 10, 4, 20, 0, 0), Progression = "Охотник, Медик (119)" }
    };

    public static List<RemnantWorldEvent> Events()
    {
        var samples = new[]
        {
            ("WidowsCourt", "World_Jungle"),
            ("TheChimney", "World_Jungle"),
            ("SentinelsKeep", "World_Nerud"),
            ("DLC2Story", "World_Jungle"),
            ("DLC3Story", "World_Nerud")
        };
        var result = new List<RemnantWorldEvent>();
        foreach (var (name, world) in samples)
        {
            var value = new RemnantWorldEvent(name, name, new List<string> { world, name }, "Event");
            if (GameInfo.EventItem.TryGetValue(name, out var items))
                value.MissingItems.AddRange(items.Take(3));
            result.Add(value);
        }
        return result;
    }
}

public sealed class DocumentationBackup
{
    public string Name { get; set; } = "";
    public DateTime SaveDate { get; set; }
    public string Progression { get; set; } = "";
    public bool Keep { get; set; }
    public bool Active { get; set; }
}
