<<<<<<< HEAD
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GorstakBenchmark
{
    public class MainForm : Form
    {
        private Button _btnRun;
        private Button _btnCopy;
        private Button _btnExportHtml;
        private Button _btnExportJson;
        private Button _btnScreenshot;
        private Label _lblStatus;
        private Label _lblOverall;
        private Label _lblBottleneck;
        private Label _lblBottleneckSuggestion;
        private ListBox _lstScores;
        private Panel _pnlChart;
        private Panel _pnlSpinner;
        private Panel _pnlContent;
        private Timer _timerSpinner;
        private int _spinnerAngle;
        private BenchmarkResults _lastResults;
        private System.Threading.CancellationTokenSource _benchmarkCts;
        private bool _isRunning;
        private bool _closeRequested;

        private const int Pad = 32;
        private const int Gap = 24;
        private const int ContentWidth = 520;

        /* Blue-on-black / soft purple dark theme – easy on the eyes */
        private static readonly Color ContentBg = Color.FromArgb(13, 14, 22);
        private static readonly Color CardBg = Color.FromArgb(22, 24, 34);
        private static readonly Color CardBorder = Color.FromArgb(42, 45, 58);
        private static readonly Color Primary = Color.FromArgb(99, 102, 241);
        private static readonly Color PrimaryHover = Color.FromArgb(129, 132, 255);
        private static readonly Color TextPrimary = Color.FromArgb(226, 228, 235);
        private static readonly Color TextSecondary = Color.FromArgb(156, 163, 175);
        private static readonly Color[] ChartColors = {
            Color.FromArgb(99, 102, 241),
            Color.FromArgb(59, 130, 246),
            Color.FromArgb(139, 92, 246),
            Color.FromArgb(34, 211, 238),
            Color.FromArgb(167, 139, 250)
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Gorstak Benchmark";
            Size = new Size(660, 1000);
            MinimumSize = new Size(620, 640);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            WindowState = FormWindowState.Normal;
            BackColor = ContentBg;
            Font = new Font("Segoe UI", 9F);
            DoubleBuffered = true;

            int contentHeight = 72 + Gap + 148 + Gap + 280 + Gap + 168 + Gap + 88 + Pad * 2;
            var pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ContentBg,
                AutoScroll = true
            };
            Controls.Add(pnlScroll);

            _pnlContent = new Panel
            {
                Size = new Size(ContentWidth + Pad * 2, contentHeight),
                BackColor = ContentBg,
                AutoScroll = false
            };
            pnlScroll.Controls.Add(_pnlContent);
            Resize += MainForm_Resize;
            Load += (s, e) => CenterContent();
            FormClosing += MainForm_FormClosing;

            int y = Pad;

            var pnlRun = CreateCard(_pnlContent, Pad, y, 520, 72);
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

            var pnlResults = CreateCard(_pnlContent, Pad, y, 520, 148);
            _lblOverall = new Label
            {
                Text = "Overall: —",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(Pad, 18),
                AutoSize = true
            };
            pnlResults.Controls.Add(_lblOverall);
            _lblBottleneck = new Label
            {
                Text = "Bottleneck: —",
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
                ForeColor = Color.FromArgb(167, 139, 250),
                Font = new Font("Segoe UI", 9F)
            };
            pnlResults.Controls.Add(_lblBottleneckSuggestion);
            y += 148 + Gap;

            var pnlChartCard = CreateCard(_pnlContent, Pad, y, 520, 280);
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

            var pnlScoresCard = CreateCard(_pnlContent, Pad, y, 520, 168);
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
            _lstScores.Items.Add("CPU      —");
            _lstScores.Items.Add("GPU      —");
            _lstScores.Items.Add("Memory   —");
            _lstScores.Items.Add("Disk     —");
            _lstScores.Items.Add("Network  —");
            pnlScoresCard.Controls.Add(_lstScores);
            y += 168 + Gap;

            var pnlShareCard = CreateCard(_pnlContent, Pad, y, 520, 88);
            var lblShareTitle = new Label
            {
                Text = "Share results",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(Pad, 14),
                AutoSize = true
            };
            pnlShareCard.Controls.Add(lblShareTitle);
            _btnCopy = CreateSecondaryButton("Copy", Pad, 44);
            _btnCopy.Click += BtnCopy_Click;
            pnlShareCard.Controls.Add(_btnCopy);
            _btnExportHtml = CreateSecondaryButton("Export HTML", Pad + 88, 44);
            _btnExportHtml.Click += BtnExportHtml_Click;
            pnlShareCard.Controls.Add(_btnExportHtml);
            _btnExportJson = CreateSecondaryButton("Export JSON", Pad + 208, 44);
            _btnExportJson.Click += BtnExportJson_Click;
            pnlShareCard.Controls.Add(_btnExportJson);
            _btnScreenshot = CreateSecondaryButton("Screenshot JPG", Pad + 328, 44);
            _btnScreenshot.Click += BtnScreenshot_Click;
            pnlShareCard.Controls.Add(_btnScreenshot);

            _timerSpinner = new Timer { Interval = 40 };
            _timerSpinner.Tick += TimerSpinner_Tick;

            _btnCopy.Enabled = _btnExportHtml.Enabled = _btnExportJson.Enabled = _btnScreenshot.Enabled = false;
        }

        private void CenterContent()
        {
            if (_pnlContent == null) return;
            var parent = _pnlContent.Parent;
            if (parent == null) return;
            int x = Math.Max(0, (parent.ClientSize.Width - _pnlContent.Width) / 2);
            int y = Math.Max(0, (parent.ClientSize.Height - _pnlContent.Height) / 2);
            _pnlContent.Location = new Point(x, y);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            CenterContent();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isRunning) return;
            e.Cancel = true;
            _closeRequested = true;
            if (_benchmarkCts != null)
            {
                try { _benchmarkCts.Cancel(); } catch { }
                _lblStatus.Text = "Cancelling...";
            }
        }

        private Panel CreateCard(Control parent, int x, int y, int w, int h)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = CardBg,
                BorderStyle = BorderStyle.None
            };
            p.Paint += (s, ev) =>
            {
                var r = p.ClientRectangle;
                using (var pen = new Pen(CardBorder, 1))
                    ev.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
            };
            parent.Controls.Add(p);
            return p;
        }

        private Button CreateSecondaryButton(string text, int x, int y)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(120, 36),
                Location = new Point(x, y),
                BackColor = CardBg,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            b.FlatAppearance.BorderColor = CardBorder;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 41, 55);
            return b;
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            _closeRequested = false;
            _benchmarkCts = new System.Threading.CancellationTokenSource();
            _isRunning = true;
            _btnRun.Enabled = false;
            _lblStatus.Text = "Running...";
            _pnlSpinner.Visible = true;
            _spinnerAngle = 0;
            _timerSpinner.Start();
            _btnCopy.Enabled = _btnExportHtml.Enabled = _btnExportJson.Enabled = _btnScreenshot.Enabled = false;
            Refresh();
            Application.DoEvents();
            BeginInvoke(new Action(RunBenchmarkAsync));
        }

        private async void RunBenchmarkAsync()
        {
            try
            {
                var engine = new BenchmarkEngine
                {
                    Progress = new Progress<string>(s =>
                    {
                        if (_lblStatus != null && !_lblStatus.IsDisposed)
                            _lblStatus.Text = s;
                    })
                };
                _lastResults = await engine.RunAsync(_benchmarkCts.Token);
                if (IsDisposed) return;
                DisplayResults(_lastResults);
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed && _lblStatus != null)
                    _lblStatus.Text = "Cancelled.";
            }
            catch (Exception ex)
            {
                if (!IsDisposed && _lblStatus != null)
                    _lblStatus.Text = "Error: " + ex.Message;
                try { MessageBox.Show(ex.Message, "Benchmark Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch { }
            }
            finally
            {
                _isRunning = false;
                if (!IsDisposed && _btnRun != null)
                {
                    _timerSpinner.Stop();
                    _pnlSpinner.Visible = false;
                    _btnRun.Enabled = true;
                }
                if (_closeRequested && !IsDisposed)
                    BeginInvoke(new Action(() => { try { Close(); } catch { } }));
            }
        }

        private void TimerSpinner_Tick(object sender, EventArgs e)
        {
            _spinnerAngle = (_spinnerAngle + 15) % 360;
            if (_pnlSpinner != null && _pnlSpinner.Visible)
                _pnlSpinner.Invalidate();
        }

        private void PnlSpinner_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = _pnlSpinner.ClientRectangle;
            int d = Math.Min(r.Width, r.Height) - 6;
            int x = (r.Width - d) / 2;
            int y = (r.Height - d) / 2;
            using (var pen = new Pen(Primary, 3f))
                g.DrawArc(pen, x, y, d, d, _spinnerAngle, 100);
        }

        private static double CapPercent(double p) { return Math.Round(p, 1); }

        private void DisplayResults(BenchmarkResults r)
        {
            _lastResults = r;
            _lblOverall.Text = string.Format("Overall: {0:N0}  ({1}%)", r.OverallScore, Math.Round(r.OverallPercent, 1));

            bool balanced = (r.BottleneckType.IndexOf("None") >= 0) || (r.BottleneckSeverity < 5);
            if (balanced)
            {
                _lblBottleneck.Text = "No bottleneck — CPU and GPU balanced.";
                _lblBottleneckSuggestion.Text = "";
            }
            else
            {
                _lblBottleneck.Text = string.Format("Bottleneck: {0}  (severity {1})", r.BottleneckType, r.BottleneckSeverity);
                if (r.BottleneckSeverity >= 15 && r.BottleneckType.IndexOf("CPU") >= 0)
                    _lblBottleneckSuggestion.Text = "Consider upgrading your CPU for better balance.";
                else if (r.BottleneckSeverity >= 15 && r.BottleneckType.IndexOf("GPU") >= 0)
                    _lblBottleneckSuggestion.Text = "Consider upgrading your GPU for better balance.";
                else
                    _lblBottleneckSuggestion.Text = "";
            }
            _lblStatus.Text = "Complete. Use Share to copy or export.";

            _lstScores.Items.Clear();
            _lstScores.Items.Add(string.Format("CPU      {0,10:N0}   {1}%", r.CpuScore, Math.Round(r.CpuPercent, 1)));
            _lstScores.Items.Add(string.Format("GPU      {0,10:N0}   {1}%", r.GpuScore, Math.Round(r.GpuPercent, 1)));
            _lstScores.Items.Add(string.Format("Memory   {0,10:N0}   {1}%", r.MemoryScore, Math.Round(r.MemoryPercent, 1)));
            _lstScores.Items.Add(string.Format("Disk     {0,10:N0}   {1}%", r.DiskScore, Math.Round(r.DiskPercent, 1)));
            _lstScores.Items.Add(string.Format("Network  {0,10:N0}   {1}%", r.NetworkScore, Math.Round(r.NetworkPercent, 1)));

            try { _pnlChart.Invalidate(); } catch { }
            _btnCopy.Enabled = _btnExportHtml.Enabled = _btnExportJson.Enabled = _btnScreenshot.Enabled = true;
        }

        private void PnlChart_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = _pnlChart.ClientRectangle;
            if (r.Width < 10 || r.Height < 10) return;

            if (_lastResults == null)
            {
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var br = new SolidBrush(TextSecondary))
                using (var font = new Font("Segoe UI", 10F))
                    g.DrawString("Run benchmark to see chart", font, br, new RectangleF(0, 0, r.Width, r.Height), sf);
                return;
            }

            double[] values = {
                _lastResults.CpuPercent,
                _lastResults.GpuPercent,
                _lastResults.MemoryPercent,
                _lastResults.DiskPercent,
                _lastResults.NetworkPercent
            };
            string[] labels = { "CPU", "GPU", "RAM", "Disk", "Net" };

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
                    using (var br = new SolidBrush(ChartColors[i]))
                        g.FillPie(br, rect, startAngle, sweep);
                    startAngle += sweep;
                }
            }

            int ly = 6;
            using (var font = new Font("Segoe UI", 9F))
            using (var brush = new SolidBrush(TextPrimary))
                for (int i = 0; i < labels.Length; i++)
                {
                    using (var cb = new SolidBrush(ChartColors[i]))
                        g.FillRectangle(cb, 6, ly, 10, 10);
                    g.DrawString(string.Format("{0}  {1:N0}%", labels[i], values[i]), font, brush, 22, ly - 1);
                    ly += 20;
                }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            try
            {
                Clipboard.SetText(_lastResults.GetShareableText());
                _lblStatus.Text = "Copied to clipboard.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Copy failed.";
                MessageBox.Show(ex.Message, "Copy failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnExportHtml_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "HTML (*.html)|*.html|All files (*.*)|*.*";
                    dlg.DefaultExt = "html";
                    dlg.FileName = string.Format("Benchmark_{0:yyyyMMdd_HHmmss}.html", _lastResults.RunDate);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dlg.FileName, _lastResults.GetHtmlReport());
                        _lblStatus.Text = "HTML saved.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnExportJson_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "JSON (*.json)|*.json|All files (*.*)|*.*";
                    dlg.DefaultExt = "json";
                    dlg.FileName = string.Format("Benchmark_{0:yyyyMMdd_HHmmss}.json", _lastResults.RunDate);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dlg.FileName, _lastResults.GetJson());
                        _lblStatus.Text = "JSON saved.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnScreenshot_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|All files (*.*)|*.*";
                    dlg.DefaultExt = "jpg";
                    dlg.FileName = string.Format("Benchmark_{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now);
                    if (dlg.ShowDialog() != DialogResult.OK) return;

                    string path = dlg.FileName;

                    // Capture the full content panel (including scrolled-off areas)
                    int w = _pnlContent.Width;
                    int h = _pnlContent.Height;
                    if (w < 1) w = 1;
                    if (h < 1) h = 1;
                    using (var bmp = new Bitmap(w, h))
                    {
                        _pnlContent.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
                        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            bmp.Save(path, ImageFormat.Png);
                        else
                            bmp.Save(path, ImageFormat.Jpeg);
                    }
                    _lblStatus.Text = "Screenshot saved.";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Screenshot failed.";
                MessageBox.Show(ex.Message, "Screenshot failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
=======
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GorstakBenchmark
{
    public class MainForm : Form
    {
        private Button _btnRun;
        private Button _btnCopy;
        private Button _btnExportHtml;
        private Button _btnExportJson;
        private Button _btnScreenshot;
        private Label _lblStatus;
        private Label _lblOverall;
        private Label _lblBottleneck;
        private Label _lblBottleneckSuggestion;
        private ListBox _lstScores;
        private Panel _pnlChart;
        private Panel _pnlSpinner;
        private Panel _pnlContent;
        private Timer _timerSpinner;
        private int _spinnerAngle;
        private BenchmarkResults _lastResults;
        private System.Threading.CancellationTokenSource _benchmarkCts;
        private bool _isRunning;
        private bool _closeRequested;

        private const int Pad = 32;
        private const int Gap = 24;
        private const int ContentWidth = 520;

        /* Blue-on-black / soft purple dark theme – easy on the eyes */
        private static readonly Color ContentBg = Color.FromArgb(13, 14, 22);
        private static readonly Color CardBg = Color.FromArgb(22, 24, 34);
        private static readonly Color CardBorder = Color.FromArgb(42, 45, 58);
        private static readonly Color Primary = Color.FromArgb(99, 102, 241);
        private static readonly Color PrimaryHover = Color.FromArgb(129, 132, 255);
        private static readonly Color TextPrimary = Color.FromArgb(226, 228, 235);
        private static readonly Color TextSecondary = Color.FromArgb(156, 163, 175);
        private static readonly Color[] ChartColors = {
            Color.FromArgb(99, 102, 241),
            Color.FromArgb(59, 130, 246),
            Color.FromArgb(139, 92, 246),
            Color.FromArgb(34, 211, 238),
            Color.FromArgb(167, 139, 250)
        };

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Gorstak Benchmark";
            Size = new Size(660, 1000);
            MinimumSize = new Size(620, 640);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            WindowState = FormWindowState.Normal;
            BackColor = ContentBg;
            Font = new Font("Segoe UI", 9F);
            DoubleBuffered = true;

            int contentHeight = 72 + Gap + 148 + Gap + 280 + Gap + 168 + Gap + 88 + Pad * 2;
            var pnlScroll = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ContentBg,
                AutoScroll = true
            };
            Controls.Add(pnlScroll);

            _pnlContent = new Panel
            {
                Size = new Size(ContentWidth + Pad * 2, contentHeight),
                BackColor = ContentBg,
                AutoScroll = false
            };
            pnlScroll.Controls.Add(_pnlContent);
            Resize += MainForm_Resize;
            Load += (s, e) => CenterContent();
            FormClosing += MainForm_FormClosing;

            int y = Pad;

            var pnlRun = CreateCard(_pnlContent, Pad, y, 520, 72);
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

            var pnlResults = CreateCard(_pnlContent, Pad, y, 520, 148);
            _lblOverall = new Label
            {
                Text = "Overall: —",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(Pad, 18),
                AutoSize = true
            };
            pnlResults.Controls.Add(_lblOverall);
            _lblBottleneck = new Label
            {
                Text = "Bottleneck: —",
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
                ForeColor = Color.FromArgb(167, 139, 250),
                Font = new Font("Segoe UI", 9F)
            };
            pnlResults.Controls.Add(_lblBottleneckSuggestion);
            y += 148 + Gap;

            var pnlChartCard = CreateCard(_pnlContent, Pad, y, 520, 280);
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

            var pnlScoresCard = CreateCard(_pnlContent, Pad, y, 520, 168);
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
            _lstScores.Items.Add("CPU      —");
            _lstScores.Items.Add("GPU      —");
            _lstScores.Items.Add("Memory   —");
            _lstScores.Items.Add("Disk     —");
            _lstScores.Items.Add("Network  —");
            pnlScoresCard.Controls.Add(_lstScores);
            y += 168 + Gap;

            var pnlShareCard = CreateCard(_pnlContent, Pad, y, 520, 88);
            var lblShareTitle = new Label
            {
                Text = "Share results",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = TextPrimary,
                Location = new Point(Pad, 14),
                AutoSize = true
            };
            pnlShareCard.Controls.Add(lblShareTitle);
            _btnCopy = CreateSecondaryButton("Copy", Pad, 44);
            _btnCopy.Click += BtnCopy_Click;
            pnlShareCard.Controls.Add(_btnCopy);
            _btnExportHtml = CreateSecondaryButton("Export HTML", Pad + 88, 44);
            _btnExportHtml.Click += BtnExportHtml_Click;
            pnlShareCard.Controls.Add(_btnExportHtml);
            _btnExportJson = CreateSecondaryButton("Export JSON", Pad + 208, 44);
            _btnExportJson.Click += BtnExportJson_Click;
            pnlShareCard.Controls.Add(_btnExportJson);
            _btnScreenshot = CreateSecondaryButton("Screenshot JPG", Pad + 328, 44);
            _btnScreenshot.Click += BtnScreenshot_Click;
            pnlShareCard.Controls.Add(_btnScreenshot);

            _timerSpinner = new Timer { Interval = 40 };
            _timerSpinner.Tick += TimerSpinner_Tick;

            _btnCopy.Enabled = _btnExportHtml.Enabled = _btnExportJson.Enabled = _btnScreenshot.Enabled = false;
        }

        private void CenterContent()
        {
            if (_pnlContent == null) return;
            var parent = _pnlContent.Parent;
            if (parent == null) return;
            int x = Math.Max(0, (parent.ClientSize.Width - _pnlContent.Width) / 2);
            int y = Math.Max(0, (parent.ClientSize.Height - _pnlContent.Height) / 2);
            _pnlContent.Location = new Point(x, y);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            CenterContent();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isRunning) return;
            e.Cancel = true;
            _closeRequested = true;
            if (_benchmarkCts != null)
            {
                try { _benchmarkCts.Cancel(); } catch { }
                _lblStatus.Text = "Cancelling...";
            }
        }

        private Panel CreateCard(Control parent, int x, int y, int w, int h)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = CardBg,
                BorderStyle = BorderStyle.None
            };
            p.Paint += (s, ev) =>
            {
                var r = p.ClientRectangle;
                using (var pen = new Pen(CardBorder, 1))
                    ev.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
            };
            parent.Controls.Add(p);
            return p;
        }

        private Button CreateSecondaryButton(string text, int x, int y)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(120, 36),
                Location = new Point(x, y),
                BackColor = CardBg,
                ForeColor = TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            b.FlatAppearance.BorderColor = CardBorder;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 41, 55);
            return b;
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            _closeRequested = false;
            _benchmarkCts = new System.Threading.CancellationTokenSource();
            _isRunning = true;
            _btnRun.Enabled = false;
            _lblStatus.Text = "Running...";
            _pnlSpinner.Visible = true;
            _spinnerAngle = 0;
            _timerSpinner.Start();
            _btnCopy.Enabled = _btnExportHtml.Enabled = _btnExportJson.Enabled = _btnScreenshot.Enabled = false;
            Refresh();
            Application.DoEvents();
            BeginInvoke(new Action(RunBenchmarkAsync));
        }

        private async void RunBenchmarkAsync()
        {
            try
            {
                var engine = new BenchmarkEngine
                {
                    Progress = new Progress<string>(s =>
                    {
                        if (_lblStatus != null && !_lblStatus.IsDisposed)
                            _lblStatus.Text = s;
                    })
                };
                _lastResults = await engine.RunAsync(_benchmarkCts.Token);
                if (IsDisposed) return;
                DisplayResults(_lastResults);
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed && _lblStatus != null)
                    _lblStatus.Text = "Cancelled.";
            }
            catch (Exception ex)
            {
                if (!IsDisposed && _lblStatus != null)
                    _lblStatus.Text = "Error: " + ex.Message;
                try { MessageBox.Show(ex.Message, "Benchmark Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch { }
            }
            finally
            {
                _isRunning = false;
                if (!IsDisposed && _btnRun != null)
                {
                    _timerSpinner.Stop();
                    _pnlSpinner.Visible = false;
                    _btnRun.Enabled = true;
                }
                if (_closeRequested && !IsDisposed)
                    BeginInvoke(new Action(() => { try { Close(); } catch { } }));
            }
        }

        private void TimerSpinner_Tick(object sender, EventArgs e)
        {
            _spinnerAngle = (_spinnerAngle + 15) % 360;
            if (_pnlSpinner != null && _pnlSpinner.Visible)
                _pnlSpinner.Invalidate();
        }

        private void PnlSpinner_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = _pnlSpinner.ClientRectangle;
            int d = Math.Min(r.Width, r.Height) - 6;
            int x = (r.Width - d) / 2;
            int y = (r.Height - d) / 2;
            using (var pen = new Pen(Primary, 3f))
                g.DrawArc(pen, x, y, d, d, _spinnerAngle, 100);
        }

        private static double CapPercent(double p) { return Math.Round(p, 1); }

        private void DisplayResults(BenchmarkResults r)
        {
            _lastResults = r;
            _lblOverall.Text = string.Format("Overall: {0:N0}  ({1}%)", r.OverallScore, Math.Round(r.OverallPercent, 1));

            bool balanced = (r.BottleneckType.IndexOf("None") >= 0) || (r.BottleneckSeverity < 5);
            if (balanced)
            {
                _lblBottleneck.Text = "No bottleneck — CPU and GPU balanced.";
                _lblBottleneckSuggestion.Text = "";
            }
            else
            {
                _lblBottleneck.Text = string.Format("Bottleneck: {0}  (severity {1})", r.BottleneckType, r.BottleneckSeverity);
                if (r.BottleneckSeverity >= 15 && r.BottleneckType.IndexOf("CPU") >= 0)
                    _lblBottleneckSuggestion.Text = "Consider upgrading your CPU for better balance.";
                else if (r.BottleneckSeverity >= 15 && r.BottleneckType.IndexOf("GPU") >= 0)
                    _lblBottleneckSuggestion.Text = "Consider upgrading your GPU for better balance.";
                else
                    _lblBottleneckSuggestion.Text = "";
            }
            _lblStatus.Text = "Complete. Use Share to copy or export.";

            _lstScores.Items.Clear();
            _lstScores.Items.Add(string.Format("CPU      {0,10:N0}   {1}%", r.CpuScore, Math.Round(r.CpuPercent, 1)));
            _lstScores.Items.Add(string.Format("GPU      {0,10:N0}   {1}%", r.GpuScore, Math.Round(r.GpuPercent, 1)));
            _lstScores.Items.Add(string.Format("Memory   {0,10:N0}   {1}%", r.MemoryScore, Math.Round(r.MemoryPercent, 1)));
            _lstScores.Items.Add(string.Format("Disk     {0,10:N0}   {1}%", r.DiskScore, Math.Round(r.DiskPercent, 1)));
            _lstScores.Items.Add(string.Format("Network  {0,10:N0}   {1}%", r.NetworkScore, Math.Round(r.NetworkPercent, 1)));

            try { _pnlChart.Invalidate(); } catch { }
            _btnCopy.Enabled = _btnExportHtml.Enabled = _btnExportJson.Enabled = _btnScreenshot.Enabled = true;
        }

        private void PnlChart_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = _pnlChart.ClientRectangle;
            if (r.Width < 10 || r.Height < 10) return;

            if (_lastResults == null)
            {
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var br = new SolidBrush(TextSecondary))
                using (var font = new Font("Segoe UI", 10F))
                    g.DrawString("Run benchmark to see chart", font, br, new RectangleF(0, 0, r.Width, r.Height), sf);
                return;
            }

            double[] values = {
                _lastResults.CpuPercent,
                _lastResults.GpuPercent,
                _lastResults.MemoryPercent,
                _lastResults.DiskPercent,
                _lastResults.NetworkPercent
            };
            string[] labels = { "CPU", "GPU", "RAM", "Disk", "Net" };

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
                    using (var br = new SolidBrush(ChartColors[i]))
                        g.FillPie(br, rect, startAngle, sweep);
                    startAngle += sweep;
                }
            }

            int ly = 6;
            using (var font = new Font("Segoe UI", 9F))
            using (var brush = new SolidBrush(TextPrimary))
                for (int i = 0; i < labels.Length; i++)
                {
                    using (var cb = new SolidBrush(ChartColors[i]))
                        g.FillRectangle(cb, 6, ly, 10, 10);
                    g.DrawString(string.Format("{0}  {1:N0}%", labels[i], values[i]), font, brush, 22, ly - 1);
                    ly += 20;
                }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            try
            {
                Clipboard.SetText(_lastResults.GetShareableText());
                _lblStatus.Text = "Copied to clipboard.";
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Copy failed.";
                MessageBox.Show(ex.Message, "Copy failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnExportHtml_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "HTML (*.html)|*.html|All files (*.*)|*.*";
                    dlg.DefaultExt = "html";
                    dlg.FileName = string.Format("Benchmark_{0:yyyyMMdd_HHmmss}.html", _lastResults.RunDate);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dlg.FileName, _lastResults.GetHtmlReport());
                        _lblStatus.Text = "HTML saved.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnExportJson_Click(object sender, EventArgs e)
        {
            if (_lastResults == null) return;
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "JSON (*.json)|*.json|All files (*.*)|*.*";
                    dlg.DefaultExt = "json";
                    dlg.FileName = string.Format("Benchmark_{0:yyyyMMdd_HHmmss}.json", _lastResults.RunDate);
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dlg.FileName, _lastResults.GetJson());
                        _lblStatus.Text = "JSON saved.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnScreenshot_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Filter = "JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|All files (*.*)|*.*";
                    dlg.DefaultExt = "jpg";
                    dlg.FileName = string.Format("Benchmark_{0:yyyyMMdd_HHmmss}.jpg", DateTime.Now);
                    if (dlg.ShowDialog() != DialogResult.OK) return;

                    string path = dlg.FileName;

                    // Capture the full content panel (including scrolled-off areas)
                    int w = _pnlContent.Width;
                    int h = _pnlContent.Height;
                    if (w < 1) w = 1;
                    if (h < 1) h = 1;
                    using (var bmp = new Bitmap(w, h))
                    {
                        _pnlContent.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
                        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                            bmp.Save(path, ImageFormat.Png);
                        else
                            bmp.Save(path, ImageFormat.Jpeg);
                    }
                    _lblStatus.Text = "Screenshot saved.";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Screenshot failed.";
                MessageBox.Show(ex.Message, "Screenshot failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
>>>>>>> 3d3535ab3f0cc7ffda6c31d1b5ec2b6adbb77c7e
