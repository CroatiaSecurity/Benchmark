using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace GorstakBenchmark;

public class MainForm : Form
{
    private Button _btnRun = null!;
    private Button _btnScreenshot = null!;
    private Label _lblStatus = null!;
    private Label _lblOverall = null!;
    private Label _lblBottleneck = null!;
    private Label _lblBottleneckSuggestion = null!;
    private ListBox _lstScores = null!;
    private Panel _pnlChart = null!;
    private Panel _pnlSpinner = null!;
    private Panel _pnlContent = null!;
    private Panel _pnlAnalysis = null!;
    private Label _lblHealthTitle = null!;
    private Label _lblHealthStatus = null!;
    private Label _lblHwInfo = null!;
    private ListBox _lstIssues = null!;
    private System.Windows.Forms.Timer _timerSpinner = null!;
    private int _spinnerAngle;
    private BenchmarkResults? _lastResults;
    private CancellationTokenSource? _benchmarkCts;
    private bool _isRunning;
    private bool _closeRequested;

    private const int Pad = 32;
    private const int Gap = 24;
    private const int ContentWidth = 520;

    // Dark theme colors
    private static readonly Color ContentBg = Color.FromArgb(13, 14, 22);
    private static readonly Color CardBg = Color.FromArgb(22, 24, 34);
    private static readonly Color CardBorder = Color.FromArgb(42, 45, 58);
    private static readonly Color Primary = Color.FromArgb(99, 102, 241);
    private static readonly Color PrimaryHover = Color.FromArgb(129, 132, 255);
    private static readonly Color TextPrimary = Color.FromArgb(226, 228, 235);
    private static readonly Color TextSecondary = Color.FromArgb(156, 163, 175);
    private static readonly Color HealthGood = Color.FromArgb(34, 211, 238);
    private static readonly Color HealthWarning = Color.FromArgb(251, 191, 36);
    private static readonly Color HealthCritical = Color.FromArgb(239, 68, 68);
    private static readonly Color HealthMinor = Color.FromArgb(167, 139, 250);
    private static readonly Color[] ChartColors =
    [
        Color.FromArgb(99, 102, 241),
        Color.FromArgb(59, 130, 246),
        Color.FromArgb(139, 92, 246),
        Color.FromArgb(34, 211, 238),
        Color.FromArgb(167, 139, 250)
    ];

    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Gorstak Benchmark v0.4.0";
        Size = new Size(660, 1100);
        MinimumSize = new Size(620, 700);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = ContentBg;
        Font = new Font("Segoe UI", 9F);
        DoubleBuffered = true;

        var pnlScroll = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ContentBg,
            AutoScroll = true
        };
        Controls.Add(pnlScroll);

        // Content panel height will be adjusted after all cards are added
        _pnlContent = new Panel
        {
            Size = new Size(ContentWidth + Pad * 2, 1400),
            BackColor = ContentBg,
            AutoScroll = false
        };
        pnlScroll.Controls.Add(_pnlContent);
        Resize += (_, _) => CenterContent();
        Load += (_, _) => CenterContent();
        FormClosing += MainForm_FormClosing;

        int y = Pad;

        // === Run card ===
        var pnlRun = CreateCard(_pnlContent, Pad, y, ContentWidth, 72);
        try
        {
            var appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (appIcon != null)
            {
                var picLogo = new PictureBox
                {
                    Location = new Point(Pad, 16),
                    Size = new Size(32, 32),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Image = appIcon.ToBitmap(),
                    BackColor = CardBg
                };
                pnlRun.Controls.Add(picLogo);
            }
        }
        catch { }

        _btnRun = new Button
        {
            Text = "Run benchmark",
            Size = new Size(148, 40),
            Location = new Point(Pad + 40, 16),
            BackColor = Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _btnRun.FlatAppearance.BorderSize = 0;
        _btnRun.FlatAppearance.MouseOverBackColor = PrimaryHover;
        _btnRun.Click += BtnRun_Click;
        pnlRun.Controls.Add(_btnRun);

        _pnlSpinner = new Panel
        {
            Location = new Point(Pad + 196, 14),
            Size = new Size(40, 40),
            BackColor = CardBg,
            Visible = false
        };
        _pnlSpinner.Paint += PnlSpinner_Paint;
        pnlRun.Controls.Add(_pnlSpinner);

        _lblStatus = new Label
        {
            Text = "Click Run to start.",
            Location = new Point(Pad + 248, 22),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9.5F)
        };
        pnlRun.Controls.Add(_lblStatus);
        y += 72 + Gap;

        // === Results card ===
        var pnlResults = CreateCard(_pnlContent, Pad, y, ContentWidth, 148);
        _lblOverall = new Label
        {
            Text = "Overall: \u2014",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(Pad, 18),
            AutoSize = true
        };
        pnlResults.Controls.Add(_lblOverall);
        _lblBottleneck = new Label
        {
            Text = "Bottleneck: \u2014",
            Location = new Point(Pad, 66),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 9.5F)
        };
        pnlResults.Controls.Add(_lblBottleneck);
        _lblBottleneckSuggestion = new Label
        {
            Text = "",
            Location = new Point(Pad, 92),
            MaximumSize = new Size(488, 0),
            AutoSize = true,
            ForeColor = HealthMinor,
            Font = new Font("Segoe UI", 9F)
        };
        pnlResults.Controls.Add(_lblBottleneckSuggestion);
        y += 148 + Gap;

        // === Chart card ===
        var pnlChartCard = CreateCard(_pnlContent, Pad, y, ContentWidth, 280);
        var lblChartTitle = new Label
        {
            Text = "Performance breakdown",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(Pad, 14),
            AutoSize = true
        };
        pnlChartCard.Controls.Add(lblChartTitle);
        _pnlChart = new Panel
        {
            Location = new Point(Pad, 42),
            Size = new Size(488, 224),
            BackColor = Color.FromArgb(18, 20, 30),
            BorderStyle = BorderStyle.None
        };
        _pnlChart.Paint += PnlChart_Paint;
        pnlChartCard.Controls.Add(_pnlChart);
        y += 280 + Gap;

        // === Scores card ===
        var pnlScoresCard = CreateCard(_pnlContent, Pad, y, ContentWidth, 168);
        var lblScoresTitle = new Label
        {
            Text = "Component scores",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(Pad, 14),
            AutoSize = true
        };
        pnlScoresCard.Controls.Add(lblScoresTitle);
        _lstScores = new ListBox
        {
            Location = new Point(Pad, 44),
            Size = new Size(488, 110),
            BackColor = CardBg,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10F)
        };
        _lstScores.Items.Add("CPU      \u2014");
        _lstScores.Items.Add("GPU      \u2014");
        _lstScores.Items.Add("Memory   \u2014");
        _lstScores.Items.Add("Disk     \u2014");
        _lstScores.Items.Add("Network  \u2014");
        pnlScoresCard.Controls.Add(_lstScores);
        y += 168 + Gap;

        // === System Analysis card ===
        _pnlAnalysis = CreateCard(_pnlContent, Pad, y, ContentWidth, 220);
        _lblHealthTitle = new Label
        {
            Text = "System Health Analysis",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Location = new Point(Pad, 14),
            AutoSize = true
        };
        _pnlAnalysis.Controls.Add(_lblHealthTitle);

        _lblHealthStatus = new Label
        {
            Text = "Run benchmark to analyze system health.",
            Location = new Point(Pad, 42),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _pnlAnalysis.Controls.Add(_lblHealthStatus);

        _lblHwInfo = new Label
        {
            Text = "",
            Location = new Point(Pad, 68),
            MaximumSize = new Size(488, 0),
            AutoSize = true,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.5F)
        };
        _pnlAnalysis.Controls.Add(_lblHwInfo);

        _lstIssues = new ListBox
        {
            Location = new Point(Pad, 120),
            Size = new Size(488, 84),
            BackColor = CardBg,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9F)
        };
        _pnlAnalysis.Controls.Add(_lstIssues);
        y += 220 + Gap;

        // === Screenshot button (bottom) ===
        var pnlScreenshot = CreateCard(_pnlContent, Pad, y, ContentWidth, 64);
        _btnScreenshot = new Button
        {
            Text = "\uD83D\uDCF7  Save Screenshot",
            Size = new Size(200, 40),
            Location = new Point((ContentWidth - 200) / 2, 12),
            BackColor = Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Enabled = false
        };
        _btnScreenshot.FlatAppearance.BorderSize = 0;
        _btnScreenshot.FlatAppearance.MouseOverBackColor = PrimaryHover;
        _btnScreenshot.Click += BtnScreenshot_Click;
        pnlScreenshot.Controls.Add(_btnScreenshot);
        y += 64 + Pad;

        // Set final content height
        _pnlContent.Height = y;

        // Spinner timer
        _timerSpinner = new System.Windows.Forms.Timer { Interval = 40 };
        _timerSpinner.Tick += (_, _) =>
        {
            _spinnerAngle = (_spinnerAngle + 15) % 360;
            if (_pnlSpinner.Visible) _pnlSpinner.Invalidate();
        };
    }

    private void CenterContent()
    {
        if (_pnlContent?.Parent == null) return;
        var parent = _pnlContent.Parent;
        int x = Math.Max(0, (parent.ClientSize.Width - _pnlContent.Width) / 2);
        _pnlContent.Location = new Point(x, _pnlContent.Location.Y);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_isRunning) return;
        e.Cancel = true;
        _closeRequested = true;
        try { _benchmarkCts?.Cancel(); } catch { }
        _lblStatus.Text = "Cancelling...";
    }

    private static Panel CreateCard(Control parent, int x, int y, int w, int h)
    {
        var p = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(w, h),
            BackColor = CardBg,
            BorderStyle = BorderStyle.None
        };
        p.Paint += (_, ev) =>
        {
            var r = p.ClientRectangle;
            using var pen = new Pen(CardBorder, 1);
            ev.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
        };
        parent.Controls.Add(p);
        return p;
    }

    private void BtnRun_Click(object? sender, EventArgs e)
    {
        _closeRequested = false;
        _benchmarkCts = new CancellationTokenSource();
        _isRunning = true;
        _btnRun.Enabled = false;
        _btnScreenshot.Enabled = false;
        _lblStatus.Text = "Running...";
        _pnlSpinner.Visible = true;
        _spinnerAngle = 0;
        _timerSpinner.Start();
        Refresh();
        Application.DoEvents();
        BeginInvoke(RunBenchmarkAsync);
    }

    private async void RunBenchmarkAsync()
    {
        try
        {
            var engine = new BenchmarkEngine
            {
                Progress = new Progress<string>(s =>
                {
                    if (!_lblStatus.IsDisposed)
                        _lblStatus.Text = s;
                })
            };
            _lastResults = await engine.RunAsync(_benchmarkCts!.Token);
            if (IsDisposed) return;
            DisplayResults(_lastResults);
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed) _lblStatus.Text = "Cancelled.";
        }
        catch (Exception ex)
        {
            if (!IsDisposed) _lblStatus.Text = "Error: " + ex.Message;
            try { MessageBox.Show(ex.Message, "Benchmark Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch { }
        }
        finally
        {
            _isRunning = false;
            if (!IsDisposed)
            {
                _timerSpinner.Stop();
                _pnlSpinner.Visible = false;
                _btnRun.Enabled = true;
            }
            if (_closeRequested && !IsDisposed)
                BeginInvoke(() => { try { Close(); } catch { } });
        }
    }

    private void PnlSpinner_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = _pnlSpinner.ClientRectangle;
        int d = Math.Min(r.Width, r.Height) - 6;
        int x = (r.Width - d) / 2;
        int y = (r.Height - d) / 2;
        using var pen = new Pen(Primary, 3f);
        g.DrawArc(pen, x, y, d, d, _spinnerAngle, 100);
    }

    private void DisplayResults(BenchmarkResults r)
    {
        _lblOverall.Text = $"Overall: {r.OverallScore:N0}  ({Math.Round(r.OverallPercent, 1)}%)";

        bool balanced = r.BottleneckType.Contains("None") || r.BottleneckSeverity < 5;
        if (balanced)
        {
            _lblBottleneck.Text = "No bottleneck \u2014 CPU and GPU balanced.";
            _lblBottleneckSuggestion.Text = "";
        }
        else
        {
            _lblBottleneck.Text = $"Bottleneck: {r.BottleneckType}  (severity {r.BottleneckSeverity})";
            _lblBottleneckSuggestion.Text = r.BottleneckSeverity >= 15
                ? r.BottleneckType.Contains("CPU")
                    ? "Consider upgrading your CPU for better balance."
                    : "Consider upgrading your GPU for better balance."
                : "";
        }

        _lblStatus.Text = "Complete. Screenshot includes all results + system analysis.";

        _lstScores.Items.Clear();
        _lstScores.Items.Add($"CPU      {r.CpuScore,10:N0}   {Math.Round(r.CpuPercent, 1)}%");
        _lstScores.Items.Add($"GPU      {r.GpuScore,10:N0}   {Math.Round(r.GpuPercent, 1)}%");
        _lstScores.Items.Add($"Memory   {r.MemoryScore,10:N0}   {Math.Round(r.MemoryPercent, 1)}%");
        _lstScores.Items.Add($"Disk     {r.DiskScore,10:N0}   {Math.Round(r.DiskPercent, 1)}%");
        _lstScores.Items.Add($"Network  {r.NetworkScore,10:N0}   {Math.Round(r.NetworkPercent, 1)}%");

        _pnlChart.Invalidate();

        // System analysis
        if (r.SystemAnalysis != null)
        {
            var sa = r.SystemAnalysis;
            Color healthColor = sa.OverallHealth switch
            {
                "Good" => HealthGood,
                "Warning" => HealthWarning,
                "Critical" => HealthCritical,
                _ => HealthMinor
            };
            _lblHealthStatus.Text = $"Health: {sa.OverallHealth}";
            _lblHealthStatus.ForeColor = healthColor;

            _lblHwInfo.Text = $"Board: {sa.Motherboard}  |  BIOS: {sa.BiosVersion} ({sa.BiosDate})\n" +
                              $"RAM: {sa.RamModules}\n" +
                              $"GPU Driver: {sa.GpuDriver} ({sa.GpuDriverDate})";

            _lstIssues.Items.Clear();
            foreach (var issue in sa.Issues)
                _lstIssues.Items.Add(issue);
        }

        _btnScreenshot.Enabled = true;
    }

    private void PnlChart_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var r = _pnlChart.ClientRectangle;
        if (r.Width < 10 || r.Height < 10) return;

        if (_lastResults == null)
        {
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var br = new SolidBrush(TextSecondary);
            using var font = new Font("Segoe UI", 10F);
            g.DrawString("Run benchmark to see chart", font, br, new RectangleF(0, 0, r.Width, r.Height), sf);
            return;
        }

        double[] values =
        [
            _lastResults.CpuPercent,
            _lastResults.GpuPercent,
            _lastResults.MemoryPercent,
            _lastResults.DiskPercent,
            _lastResults.NetworkPercent
        ];
        string[] labels = ["CPU", "GPU", "RAM", "Disk", "Net"];

        double total = 0;
        for (int i = 0; i < values.Length; i++) total += Math.Max(0, values[i]);
        if (total <= 0) total = 1;

        int margin = 28;
        int size = Math.Min(r.Width, r.Height) - margin * 2;
        int x = (r.Width - size) / 2;
        int y = (r.Height - size) / 2;
        var rect = new Rectangle(x, y, size, size);

        float startAngle = 0;
        for (int i = 0; i < values.Length; i++)
        {
            float sweep = (float)(Math.Max(0, values[i]) / total * 360);
            if (sweep > 0)
            {
                using var br = new SolidBrush(ChartColors[i]);
                g.FillPie(br, rect, startAngle, sweep);
                startAngle += sweep;
            }
        }

        int ly = 6;
        using var labelFont = new Font("Segoe UI", 9F);
        using var labelBrush = new SolidBrush(TextPrimary);
        for (int i = 0; i < labels.Length; i++)
        {
            using var cb = new SolidBrush(ChartColors[i]);
            g.FillRectangle(cb, 6, ly, 10, 10);
            g.DrawString($"{labels[i]}  {values[i]:N0}%", labelFont, labelBrush, 22, ly - 1);
            ly += 20;
        }
    }

    private void BtnScreenshot_Click(object? sender, EventArgs e)
    {
        try
        {
            using var dlg = new SaveFileDialog();
            dlg.Filter = "JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|All files (*.*)|*.*";
            dlg.DefaultExt = "jpg";
            dlg.FileName = $"Benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string path = dlg.FileName;

            // Capture the full content panel (all cards including system analysis)
            int w = _pnlContent.Width;
            int h = _pnlContent.Height;
            if (w < 1) w = 1;
            if (h < 1) h = 1;
            using var bmp = new Bitmap(w, h);
            _pnlContent.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));

            if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                bmp.Save(path, ImageFormat.Png);
            else
                bmp.Save(path, ImageFormat.Jpeg);

            _lblStatus.Text = "Screenshot saved.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Screenshot failed.";
            MessageBox.Show(ex.Message, "Screenshot failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
