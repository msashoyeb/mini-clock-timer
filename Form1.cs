using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MiniClockTimer
{
    public partial class Form1 : Form
    {
        // Variables for timer and UI controls
        private readonly Stopwatch stopwatch = new();
        private readonly System.Windows.Forms.Timer uiTimer = new();
        private FlowLayoutPanel timePanel = null!;
        private Label lblMainTime = null!;
        private Label lblMilliSec = null!;
        private Label btnPin = null!;
        private Label btnClose = null!;
        private Label btnStartPause = null!;
        private Label btnReset = null!;

        // Win32 API for dragging the borderless window
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public Form1()
        {
            InitializeComponent();
            Controls.Clear();

            // 1. Form Setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(160, 45);
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.TopMost = true;
            this.StartPosition = FormStartPosition.WindowsDefaultLocation;

            // 2. Setup Icon Buttons
            SetupButtons();

            // 3. Setup Time Panel (Container for time labels)
            timePanel = new FlowLayoutPanel
            {
                Size = new Size(100, 30),
                Location = new Point(30, 8),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 0),
                BackColor = Color.Transparent
            };
            timePanel.MouseDown += DragWindow_MouseDown;

            // 4. Setup Main Time Label (Hours:Minutes:Seconds)
            lblMainTime = new Label
            {
                ForeColor = Color.White,
                Font = new Font("Segoe UI Black", 11, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 0),
                Text = "00:00:00"
            };
            lblMainTime.MouseDown += DragWindow_MouseDown;

            // 5. Setup Millisecond Label (.00)
            lblMilliSec = new Label
            {
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI Black", 8, FontStyle.Regular),
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0),
                Text = ".00"
            };
            lblMilliSec.MouseDown += DragWindow_MouseDown;

            // 6. Add controls to panel and form
            timePanel.Controls.Add(lblMainTime);
            timePanel.Controls.Add(lblMilliSec);

            this.Controls.Add(timePanel);
            this.Controls.Add(btnPin);
            this.Controls.Add(btnClose);
            this.Controls.Add(btnStartPause);
            this.Controls.Add(btnReset);

            // Ensure buttons stay on top of other controls
            btnPin.BringToFront();
            btnClose.BringToFront();
            btnStartPause.BringToFront();
            btnReset.BringToFront();

            // 7. Events and Timer Start
            this.Resize += (s, e) => {
                ApplyRoundedCorners(5);
                this.Invalidate(); // Refresh form to redraw border on resize
            };
            this.MouseDown += DragWindow_MouseDown;
            this.Paint += Form1_Paint; // Subscribe to Paint event for the border

            uiTimer.Interval = 20; // 20ms for smooth millisecond updates
            uiTimer.Tick += ClockTimer_Tick;
            uiTimer.Start();   // Start UI updates (but stopwatch remains stopped initially)

            ApplyRoundedCorners(5);
        }

        private void SetupButtons()
        {
            // Windows built-in icon font
            Font iconFont = new Font("Segoe MDL2 Assets", 9, FontStyle.Regular);

            // Top-Left: Pin Button (Toggle Always on Top)
            btnPin = new Label
            {
                Text = "\uE718",
                Font = iconFont,
                ForeColor = Color.White,
                Size = new Size(20, 20),
                Location = new Point(5, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnPin.Click += (s, e) => {
                this.TopMost = !this.TopMost;
                btnPin.ForeColor = this.TopMost ? Color.White : Color.Gray;
            };

            // Top-Right: Close Button
            btnClose = new Label
            {
                Text = "\uE106",
                Font = iconFont,
                ForeColor = Color.White,
                Size = new Size(20, 20),
                Location = new Point(this.Width - 25, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => Application.Exit();

            // Bottom-Left: Start/Pause Button (Initial state is Play)
            btnStartPause = new Label
            {
                Text = "\uE768", // Play icon
                Font = iconFont,
                ForeColor = Color.LightGreen,
                Size = new Size(20, 20),
                Location = new Point(5, this.Height - 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnStartPause.Click += (s, e) => {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                    btnStartPause.Text = "\uE768"; // Play icon
                    btnStartPause.ForeColor = Color.LightGreen;
                }
                else
                {
                    stopwatch.Start();
                    btnStartPause.Text = "\uE769"; // Pause icon
                    btnStartPause.ForeColor = Color.Orange;
                }
            };

            // Bottom-Right: Reset Button
            btnReset = new Label
            {
                Text = "\uE72C",
                Font = iconFont,
                ForeColor = Color.SkyBlue,
                Size = new Size(20, 20),
                Location = new Point(this.Width - 25, this.Height - 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => {
                stopwatch.Reset();
                lblMainTime.Text = "00:00:00";
                lblMilliSec.Text = ".00";

                // Reset sets to paused state, so show Play icon
                btnStartPause.Text = "\uE768";
                btnStartPause.ForeColor = Color.LightGreen;
            };
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            // Get elapsed time and update labels
            TimeSpan ts = stopwatch.Elapsed;
            lblMainTime.Text = string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
            lblMilliSec.Text = string.Format(".{0:00}", ts.Milliseconds / 10);
        }

        private void DragWindow_MouseDown(object? sender, MouseEventArgs e)
        {
            // Drag form when left mouse button is clicked
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        // Helper method to create a rounded path for both clipping and border drawing
        private GraphicsPath GetRoundedPath(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90); // Top-Left
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90); // Top-Right
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90); // Bottom-Right
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90); // Bottom-Left
            path.CloseFigure();

            return path;
        }

        private void ApplyRoundedCorners(int radius)
        {
            // Cut the sharp corners of the form
            Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
            using GraphicsPath path = GetRoundedPath(bounds, radius);
            this.Region = new Region(path);
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            // Make the drawing smooth
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Create a rectangle 1 pixel smaller so the border is fully visible
            Rectangle borderBounds = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            // Draw a 1px white border using the same rounded shape (Radius 5)
            using GraphicsPath borderPath = GetRoundedPath(borderBounds, 5);
            using Pen whitePen = new Pen(Color.White, 1);

            e.Graphics.DrawPath(whitePen, borderPath);
        }
    }
}