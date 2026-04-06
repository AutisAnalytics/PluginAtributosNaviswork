using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutisAnalytics.NavisworksAtributos
{
    /// <summary>
    /// TabControl customizado com design Moderno e indicador visual
    /// </summary>
    public class ModernTabControl : TabControl
    {
        public ModernTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
            ItemSize = new Size(100, 32);
            SizeMode = TabSizeMode.Fixed;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(UITheme.Color.Background);

            // Linha separadora embaixo das abas
            e.Graphics.DrawLine(
                new Pen(UITheme.Color.BorderLight, 2),
                0, ItemSize.Height,
                Width, ItemSize.Height
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            for (int i = 0; i < TabCount; i++)
            {
                DrawTab(e.Graphics, i);
            }
        }

        private void DrawTab(Graphics g, int index)
        {
            var tab = GetTabRect(index);
            bool isSelected = index == SelectedIndex;

            // Fundo da aba
            g.FillRectangle(
                isSelected ? new SolidBrush(UITheme.Color.Surface) : new SolidBrush(UITheme.Color.Background),
                tab
            );

            // Indicador visual embaixo (apenas se selecionado)
            if (isSelected)
            {
                g.FillRectangle(
                    new SolidBrush(UITheme.Color.Primary),
                    new Rectangle(tab.Left, tab.Bottom - 3, tab.Width, 3)
                );
            }

            // Texto da aba
            var font = isSelected ? UITheme.Typography.Label : UITheme.Typography.Body;
            var color = isSelected ? UITheme.Color.Primary : UITheme.Color.TextSecondary;

            g.DrawString(
                TabPages[index].Text,
                font,
                new SolidBrush(color),
                new PointF(tab.Left + 10, tab.Top + 8)
            );
        }
    }

    /// <summary>
    /// Modal de Confirmação com design intuitivo
    /// </summary>
    public class ConfirmationDialog : Form
    {
        public enum ConfirmResult { Confirm, Cancel }
        public ConfirmResult Result { get; private set; }

        private Label lblTitle;
        private Label lblMessage;
        private Button btnConfirm;
        private Button btnCancel;

        public ConfirmationDialog(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
        {
            ClientSize = new Size(420, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            BackColor = UITheme.Color.Background;
            Font = UITheme.Typography.Body;
            Text = "Autis Analytics";

            // Título
            lblTitle = new Label
            {
                Text = title,
                Font = UITheme.Typography.H2,
                ForeColor = UITheme.Color.TextPrimary,
                Location = new Point(24, 20),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Mensagem
            lblMessage = new Label
            {
                Text = message,
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(24, 56),
                Size = new Size(372, 60),
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft
            };
            Controls.Add(lblMessage);

            // Botão Confirmar (Primário)
            btnConfirm = UITheme.CreatePrimaryButton(confirmText);
            btnConfirm.Location = new Point(250, 136);
            btnConfirm.Width = 146;
            btnConfirm.Click += (s, e) =>
            {
                Result = ConfirmResult.Confirm;
                Close();
            };
            Controls.Add(btnConfirm);

            // Botão Cancelar (Secundário)
            btnCancel = UITheme.CreateSecondaryButton(cancelText);
            btnCancel.Location = new Point(72, 136);
            btnCancel.Width = 146;
            btnCancel.Click += (s, e) =>
            {
                Result = ConfirmResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);
        }
    }

    /// <summary>
    /// Indicador de Progresso em Etapas (Wizard)
    /// </summary>
    public class StepIndicator : Control
    {
        public int CurrentStep { get; set; } = 1;
        public int TotalSteps { get; set; } = 3;

        public StepIndicator()
        {
            Height = 60;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UITheme.Color.Background);

            int stepWidth = Width / TotalSteps;
            int circleSize = 32;
            int y = (Height - circleSize) / 2;

            for (int i = 1; i <= TotalSteps; i++)
            {
                int x = (stepWidth * i) - (stepWidth / 2) - (circleSize / 2);

                // Círculo do passo
                var color = i == CurrentStep ? UITheme.Color.Primary :
                           i < CurrentStep ? UITheme.Color.Success :
                           UITheme.Color.BorderMedium;

                e.Graphics.FillEllipse(
                    new SolidBrush(color),
                    x, y, circleSize, circleSize
                );

                // Número do passo
                e.Graphics.DrawString(
                    i.ToString(),
                    UITheme.Typography.Label,
                    new SolidBrush(System.Drawing.Color.White),
                    new RectangleF(x, y, circleSize, circleSize),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }
                );

                // Linha conectora
                if (i < TotalSteps)
                {
                    int nextX = (stepWidth * (i + 1)) - (stepWidth / 2) - (circleSize / 2);
                    var lineColor = i < CurrentStep ? UITheme.Color.Success : UITheme.Color.BorderMedium;
                    e.Graphics.DrawLine(
                        new Pen(lineColor, 2),
                        x + circleSize, y + (circleSize / 2),
                        nextX, y + (circleSize / 2)
                    );
                }
            }
        }
    }

    /// <summary>
    /// Loading Spinner Animado
    /// </summary>
    public class LoadingSpinner : Control
    {
        private Timer animationTimer;
        private int rotation = 0;

        public LoadingSpinner()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            Size = new Size(40, 40);

            animationTimer = new Timer { Interval = 50 };
            animationTimer.Tick += (s, e) =>
            {
                rotation = (rotation + 10) % 360;
                Invalidate();
            };
            animationTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UITheme.Color.Background);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var center = new PointF(Width / 2, Height / 2);
            int radius = Math.Min(Width, Height) / 2 - 2;

            // Save graphics state para rotação
            var state = e.Graphics.Save();
            e.Graphics.TranslateTransform(center.X, center.Y);
            e.Graphics.RotateTransform(rotation);

            // Desenhar arco giratório
            var rect = new RectangleF(-radius, -radius, radius * 2, radius * 2);
            e.Graphics.DrawArc(
                new Pen(UITheme.Color.Primary, 3),
                rect,
                -90,
                120
            );

            e.Graphics.Restore(state);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                animationTimer?.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Toast Notification — Mensagem flutuante
    /// </summary>
    public class ToastNotification : Form
    {
        private Label lblMessage;
        private Timer hideTimer;

        public enum ToastType { Info, Success, Error, Warning }

        public ToastNotification(string message, ToastType type = ToastType.Info, int displayMs = 3000)
        {
            ClientSize = new Size(400, 60);
            StartPosition = FormStartPosition.Manual;
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - 420,
                               Screen.PrimaryScreen.WorkingArea.Bottom - 80);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;

            // Cor de fundo baseado no tipo
            BackColor = type switch
            {
                ToastType.Success => UITheme.Color.SuccessLight,
                ToastType.Error => UITheme.Color.ErrorLight,
                ToastType.Warning => UITheme.Color.WarningLight,
                _ => UITheme.Color.InfoBg
            };

            // Label com mensagem
            var icon = type switch
            {
                ToastType.Success => "✓ ",
                ToastType.Error => "✗ ",
                ToastType.Warning => "⚠ ",
                _ => "ℹ "
            };

            var textColor = type switch
            {
                ToastType.Success => UITheme.Color.Success,
                ToastType.Error => UITheme.Color.Error,
                ToastType.Warning => UITheme.Color.Warning,
                _ => UITheme.Color.InfoText
            };

            lblMessage = new Label
            {
                Text = icon + message,
                Font = UITheme.Typography.Body,
                ForeColor = textColor,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            Controls.Add(lblMessage);

            // Timer para desaparecer
            hideTimer = new Timer { Interval = displayMs };
            hideTimer.Tick += (s, e) =>
            {
                hideTimer.Stop();
                Hide();
                Dispose();
            };
            hideTimer.Start();
        }

        public static void Show(string message, ToastType type = ToastType.Info)
        {
            var toast = new ToastNotification(message, type);
            toast.Show();
        }
    }

    /// <summary>
    /// Badge — Pequeno indicador de quantidade/status
    /// </summary>
    public class Badge : Label
    {
        public Badge()
        {
            AutoSize = false;
            Size = new Size(24, 24);
            Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            ForeColor = System.Drawing.Color.White;
            BackColor = UITheme.Color.Primary;
            TextAlign = ContentAlignment.MiddleCenter;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Fundo circular
            e.Graphics.Clear(UITheme.Color.Background);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(new SolidBrush(BackColor), 0, 0, Width, Height);

            // Texto
            e.Graphics.DrawString(
                Text,
                Font,
                new SolidBrush(ForeColor),
                new RectangleF(0, 0, Width, Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }
            );
        }
    }
}
