using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutisAnalytics.NavisworksAtributos
{
    /// <summary>
    /// Sistema de Design — Cores, Fonts, Espaçamentos e Componentes Reutilizáveis
    ///
    /// Baseado em:
    /// - Material Design 3 (Google)
    /// - Anthropic Visual Language
    /// - Lei de Hick (reduzir opções visuais)
    /// - Psicologia de cores (confiança, progesso, ação)
    /// </summary>
    public static class UITheme
    {
        // ═══════════════════════════════════════════════════════════════════════════
        // PALETA DE CORES — Semântica clara
        // ═══════════════════════════════════════════════════════════════════════════

        public static class Color
        {
            // Fundos
            public static readonly System.Drawing.Color Background = System.Drawing.Color.FromArgb(248, 249, 250);
            public static readonly System.Drawing.Color BackgroundSecondary = System.Drawing.Color.FromArgb(240, 242, 246);
            public static readonly System.Drawing.Color Surface = System.Drawing.Color.White;
            public static readonly System.Drawing.Color SurfaceHover = System.Drawing.Color.FromArgb(250, 251, 254);

            // Primário — Azul vibrante (confiança, ação)
            public static readonly System.Drawing.Color Primary = System.Drawing.Color.FromArgb(21, 101, 192);      // #1565C0
            public static readonly System.Drawing.Color PrimaryHover = System.Drawing.Color.FromArgb(13, 71, 161);  // #0D47A1
            public static readonly System.Drawing.Color PrimaryLight = System.Drawing.Color.FromArgb(66, 133, 244);  // #4285F4
            public static readonly System.Drawing.Color PrimaryVeryLight = System.Drawing.Color.FromArgb(227, 237, 246); // #E3EDF6

            // Sucesso — Verde (confirmação, completo)
            public static readonly System.Drawing.Color Success = System.Drawing.Color.FromArgb(27, 131, 96);       // #1B8360
            public static readonly System.Drawing.Color SuccessHover = System.Drawing.Color.FromArgb(18, 97, 71);   // #126147
            public static readonly System.Drawing.Color SuccessLight = System.Drawing.Color.FromArgb(200, 230, 201); // #C8E6C9

            // Aviso — Âmbar (atenção)
            public static readonly System.Drawing.Color Warning = System.Drawing.Color.FromArgb(243, 152, 0);       // #F39800
            public static readonly System.Drawing.Color WarningLight = System.Drawing.Color.FromArgb(255, 243, 224); // #FFF3E0

            // Erro — Vermelho (perigo, destruição)
            public static readonly System.Drawing.Color Error = System.Drawing.Color.FromArgb(183, 28, 28);         // #B71C1C
            public static readonly System.Drawing.Color ErrorHover = System.Drawing.Color.FromArgb(136, 14, 14);    // #880E0E
            public static readonly System.Drawing.Color ErrorLight = System.Drawing.Color.FromArgb(255, 235, 238);  // #FFEBEE

            // Neutros — Escala de cinza
            public static readonly System.Drawing.Color TextPrimary = System.Drawing.Color.FromArgb(25, 25, 25);    // Muito escuro
            public static readonly System.Drawing.Color TextSecondary = System.Drawing.Color.FromArgb(100, 100, 100); // Médio
            public static readonly System.Drawing.Color TextTertiary = System.Drawing.Color.FromArgb(140, 140, 140); // Claro
            public static readonly System.Drawing.Color TextDisabled = System.Drawing.Color.FromArgb(189, 189, 189); // Desativado

            // Bordas
            public static readonly System.Drawing.Color BorderLight = System.Drawing.Color.FromArgb(225, 226, 230);
            public static readonly System.Drawing.Color BorderMedium = System.Drawing.Color.FromArgb(210, 210, 210);
            public static readonly System.Drawing.Color BorderDark = System.Drawing.Color.FromArgb(190, 190, 190);

            // Especiais
            public static readonly System.Drawing.Color Divider = System.Drawing.Color.FromArgb(240, 240, 240);
            public static readonly System.Drawing.Color InfoBg = System.Drawing.Color.FromArgb(227, 237, 246);
            public static readonly System.Drawing.Color InfoText = System.Drawing.Color.FromArgb(13, 71, 161);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TIPOGRAFIA — Hierarquia clara
        // ═══════════════════════════════════════════════════════════════════════════

        public static class Typography
        {
            private const string FontFamily = "Segoe UI";

            // Display — Títulos grandes (relativamente raro)
            public static readonly Font Display = new Font(FontFamily, 24f, FontStyle.Bold);

            // Heading 1 — Títulos principais
            public static readonly Font H1 = new Font(FontFamily, 18f, FontStyle.Bold);

            // Heading 2 — Subtítulos
            public static readonly Font H2 = new Font(FontFamily, 14f, FontStyle.Bold);

            // Heading 3 — Section headers
            public static readonly Font H3 = new Font(FontFamily, 13f, FontStyle.Bold);

            // Body Large — Texto corpo principal
            public static readonly Font BodyLarge = new Font(FontFamily, 11f, FontStyle.Regular);

            // Body Regular — Padrão
            public static readonly Font Body = new Font(FontFamily, 10f, FontStyle.Regular);

            // Body Small — Informação secundária
            public static readonly Font BodySmall = new Font(FontFamily, 9f, FontStyle.Regular);

            // Label — Rótulos de campo
            public static readonly Font Label = new Font(FontFamily, 9.5f, FontStyle.Bold);

            // Monospace — Para código/IDs
            public static readonly Font Monospace = new Font("Consolas", 11f, FontStyle.Regular);
            public static readonly Font MonospaceBold = new Font("Consolas", 12f, FontStyle.Bold);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // ESPAÇAMENTO — Grid 8px (design system padrão)
        // ═══════════════════════════════════════════════════════════════════════════

        public static class Spacing
        {
            public const int XS = 4;       // 4px
            public const int SM = 8;       // 8px
            public const int MD = 12;      // 12px
            public const int LG = 16;      // 16px
            public const int XL = 24;      // 24px
            public const int XXL = 32;     // 32px
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // SOMBRAS — Elevation (depth)
        // ═══════════════════════════════════════════════════════════════════════════

        public static class Elevation
        {
            public const int None = 0;
            public const int Level1 = 1;   // Subtle shadow
            public const int Level2 = 2;   // Hover state
            public const int Level3 = 4;   // Modal/Dialog
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // COMPONENTES REUTILIZÁVEIS
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um botão PRIMÁRIO com estilo moderno
        /// Uso: Ações principais (Salvar, Ativar, Enviar)
        /// </summary>
        public static Button CreatePrimaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Font = Typography.Label,
                ForeColor = System.Drawing.Color.White,
                BackColor = Color.Primary,
                FlatStyle = FlatStyle.Flat,
                Height = 36,
                AutoSize = false,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.PrimaryHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Primary;
            return btn;
        }

        /// <summary>
        /// Cria um botão SECUNDÁRIO com estilo moderno
        /// Uso: Ações secundárias (Cancelar, Limpar, Fechar)
        /// </summary>
        public static Button CreateSecondaryButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Font = Typography.Label,
                ForeColor = Color.TextPrimary,
                BackColor = Color.Surface,
                FlatStyle = FlatStyle.Flat,
                Height = 36,
                AutoSize = false,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.BorderMedium;
            btn.MouseEnter += (s, e) =>
            {
                btn.BackColor = Color.SurfaceHover;
                btn.FlatAppearance.BorderColor = Color.Primary;
            };
            btn.MouseLeave += (s, e) =>
            {
                btn.BackColor = Color.Surface;
                btn.FlatAppearance.BorderColor = Color.BorderMedium;
            };
            return btn;
        }

        /// <summary>
        /// Cria um botão SUCESSO com estilo moderno
        /// Uso: Ações de confirmação (Gravar, Ativar, Confirmar)
        /// </summary>
        public static Button CreateSuccessButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Font = Typography.Label,
                ForeColor = System.Drawing.Color.White,
                BackColor = Color.Success,
                FlatStyle = FlatStyle.Flat,
                Height = 36,
                AutoSize = false,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.SuccessHover;
            btn.MouseLeave += (s, e) => btn.BackColor = Color.Success;
            return btn;
        }

        /// <summary>
        /// Cria um painel de HEADER com título e ícone
        /// </summary>
        public static Panel CreateHeader(string title, string subtitle = "")
        {
            var panel = new Panel
            {
                BackColor = Color.Primary,
                Height = string.IsNullOrEmpty(subtitle) ? 60 : 90,
                Dock = DockStyle.Top
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = Typography.H1,
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Location = new Point(24, 12)
            };
            panel.Controls.Add(lblTitle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var lblSubtitle = new Label
                {
                    Text = subtitle,
                    Font = Typography.BodySmall,
                    ForeColor = System.Drawing.Color.FromArgb(200, 220, 255),
                    AutoSize = true,
                    Location = new Point(24, 54)
                };
                panel.Controls.Add(lblSubtitle);
            }

            return panel;
        }

        /// <summary>
        /// Cria um painel de FOOTER com info e botões
        /// </summary>
        public static Panel CreateFooter()
        {
            var panel = new Panel
            {
                BackColor = System.Drawing.Color.FromArgb(245, 245, 245),
                Height = 52,
                Dock = DockStyle.Bottom
            };

            panel.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(
                    new Pen(Color.BorderLight),
                    new Point(0, 0),
                    new Point(panel.Width, 0)
                );
            };

            return panel;
        }

        /// <summary>
        /// Cria um painel "CARD" com borda sutil
        /// </summary>
        public static Panel CreateCard()
        {
            var panel = new Panel
            {
                BackColor = Color.Surface
            };

            panel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.BorderLight, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };

            return panel;
        }

        /// <summary>
        /// Cria um label informativo com ícone e cor
        /// </summary>
        public static Label CreateInfoLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = Typography.BodySmall,
                ForeColor = Color.InfoText,
                AutoSize = true,
                BackColor = System.Drawing.Color.Transparent
            };
        }

        /// <summary>
        /// Cria um label de erro com ícone
        /// </summary>
        public static Label CreateErrorLabel(string text)
        {
            return new Label
            {
                Text = "⚠ " + text,
                Font = Typography.BodySmall,
                ForeColor = Color.Error,
                AutoSize = true,
                BackColor = System.Drawing.Color.Transparent
            };
        }

        /// <summary>
        /// Cria um label de sucesso
        /// </summary>
        public static Label CreateSuccessLabel(string text)
        {
            return new Label
            {
                Text = "✓ " + text,
                Font = Typography.BodySmall,
                ForeColor = Color.Success,
                AutoSize = true,
                BackColor = System.Drawing.Color.Transparent
            };
        }

        /// <summary>
        /// Cria um separador visual (linha)
        /// </summary>
        public static Panel CreateDivider()
        {
            var divider = new Panel
            {
                Height = 1,
                BackColor = Color.Divider,
                Dock = DockStyle.Top
            };
            return divider;
        }

        /// <summary>
        /// Estilizar um TextBox com design moderno
        /// </summary>
        public static void StyleTextBox(TextBox textBox)
        {
            textBox.Font = Typography.Body;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.ForeColor = Color.TextPrimary;
            textBox.BackColor = Color.Surface;
            textBox.Height = 32;
        }

        /// <summary>
        /// Estilizar um DataGridView com design moderno
        /// </summary>
        public static void StyleDataGridView(DataGridView grid)
        {
            grid.Font = Typography.Body;
            grid.BackgroundColor = Color.Background;
            grid.GridColor = Color.BorderLight;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.BackgroundSecondary;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = Typography.Label;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.BackgroundSecondary;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.TextPrimary;
            grid.DefaultCellStyle.BackColor = Color.Surface;
            grid.DefaultCellStyle.ForeColor = Color.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Color.PrimaryLight;
            grid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.SurfaceHover;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
        }

        /// <summary>
        /// Estilizar um ListView com design moderno
        /// </summary>
        public static void StyleListView(ListView listView)
        {
            listView.Font = Typography.Body;
            listView.BackColor = Color.Surface;
            listView.ForeColor = Color.TextPrimary;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.GridLines = false;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }
    }
}
