using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutisAnalytics.NavisworksAtributos
{
    public class LerAtributosForm : Form
    {
        // ── Cores ──────────────────────────────────────────────────────────────
        private static readonly Color CorFundo      = Color.FromArgb(245, 246, 250);
        private static readonly Color CorPainel     = Color.White;
        private static readonly Color CorHeader     = Color.FromArgb(44,  62,  80);
        private static readonly Color CorAccent     = Color.FromArgb(0,  120, 215);
        private static readonly Color CorTexto      = Color.FromArgb(50,  50,  50);
        private static readonly Color CorTextoClaro = Color.FromArgb(130, 130, 130);
        private static readonly Color CorBorda      = Color.FromArgb(220, 222, 228);
        private static readonly Color CorGridHeader = Color.FromArgb(44,  62,  80);
        private static readonly Color CorGridAlt    = Color.FromArgb(250, 251, 254);

        // ── Fontes ─────────────────────────────────────────────────────────────
        private static readonly Font FonteNormal  = new Font("Segoe UI",  9.5f);
        private static readonly Font FonteSemi    = new Font("Segoe UI Semibold", 9.5f);
        private static readonly Font FonteTitulo  = new Font("Segoe UI Semibold", 10f);
        private static readonly Font FonteHeader  = new Font("Segoe UI", 13f, FontStyle.Bold);
        private static readonly Font FontePequena = new Font("Segoe UI",  8.5f);
        private static readonly Font FonteGrid    = new Font("Segoe UI",  9f);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        // ── Controles principais ───────────────────────────────────────────────
        private TextBox         txtBusca;
        private DataGridView    dgv;
        private Label           lblStatus;
        private CheckedListBox  clbCategorias;

        // ── Dados ──────────────────────────────────────────────────────────────
        private readonly string               _nomeElemento;
        private readonly int                  _qtdSelecionados;
        private readonly List<AtributoCustom> _todos;
        private readonly Dictionary<string, List<AtributoCustom>> _porCategoria;

        // ──────────────────────────────────────────────────────────────────────
        public LerAtributosForm(string nomeElemento, int qtdSelecionados,
            List<AtributoCustom> propriedades)
        {
            _nomeElemento    = nomeElemento;
            _qtdSelecionados = qtdSelecionados;
            _todos           = propriedades ?? new List<AtributoCustom>();

            _porCategoria = _todos
                .GroupBy(p => p.Categoria ?? "")
                .ToDictionary(g => g.Key, g => g.ToList());

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            Montar();
            AtualizarGrid();
        }

        // ── MONTAGEM ──────────────────────────────────────────────────────────

        private void Montar()
        {
            Text            = "Read Properties";
            ClientSize      = new Size(1050, 660);
            MinimumSize     = new Size(800, 500);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor       = CorFundo;
            Font            = FonteNormal;

            MontarHeader();
            MontarBusca();
            MontarRodape();
            MontarCorpo();  // Fill — fica por último
        }

        // ── HEADER ─────────────────────────────────────────────────────────────

        private void MontarHeader()
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = CorHeader };
            pnl.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(CorAccent, 2), 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);

            pnl.Controls.Add(new Label
            {
                Text      = "Read Properties",
                Font      = FonteHeader,
                ForeColor = Color.White,
                AutoSize  = true,
                Location  = new Point(20, 10)
            });

            pnl.Controls.Add(new Label
            {
                Text      = $"{_nomeElemento}  •  {_qtdSelecionados} element(s)  •  {_todos.Count} properties",
                Font      = FontePequena,
                ForeColor = Color.FromArgb(180, 200, 220),
                AutoSize  = true,
                Location  = new Point(20, 46)
            });

            Controls.Add(pnl);
        }

        // ── BUSCA ──────────────────────────────────────────────────────────────

        private void MontarBusca()
        {
            var pnl = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 46,
                BackColor = CorPainel,
                Padding   = new Padding(12, 8, 12, 8)
            };
            pnl.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(CorBorda), 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);

            txtBusca = new TextBox
            {
                Dock        = DockStyle.Fill,
                Font        = FonteNormal,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = CorFundo,
                ForeColor   = CorTexto
            };
            txtBusca.HandleCreated += (s, e) =>
                SendMessage(txtBusca.Handle, EM_SETCUEBANNER, 0, "Search all properties...");
            txtBusca.TextChanged += (s, e) => AtualizarGrid();

            pnl.Controls.Add(txtBusca);
            Controls.Add(pnl);
        }

        // ── RODAPÉ ─────────────────────────────────────────────────────────────

        private void MontarRodape()
        {
            var pnl = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = CorPainel };
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(CorBorda), 0, 0, pnl.Width, 0);

            lblStatus = new Label
            {
                Font      = FontePequena,
                ForeColor = CorTextoClaro,
                AutoSize  = true,
                Location  = new Point(16, 16)
            };
            pnl.Controls.Add(lblStatus);

            var btnFechar = new Button
            {
                Text      = "Close",
                Font      = FonteNormal,
                ForeColor = CorTextoClaro,
                BackColor = CorFundo,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(90, 30),
                Cursor    = Cursors.Hand
            };
            btnFechar.FlatAppearance.BorderColor = CorBorda;
            btnFechar.Click += (s, e) => Close();

            var btnCopiar = new Button
            {
                Text      = "Copy All",
                Font      = FonteSemi,
                ForeColor = Color.White,
                BackColor = CorAccent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(120, 30),
                Cursor    = Cursors.Hand
            };
            btnCopiar.FlatAppearance.BorderSize = 0;
            btnCopiar.Click += BtnCopiar_Click;

            pnl.Controls.Add(btnFechar);
            pnl.Controls.Add(btnCopiar);

            pnl.Resize += (s, e) =>
            {
                btnCopiar.Location = new Point(pnl.Width - 132, 9);
                btnFechar.Location = new Point(pnl.Width - 232, 9);
            };

            Controls.Add(pnl);
        }

        // ── CORPO (SplitContainer) ─────────────────────────────────────────────

        private void MontarCorpo()
        {
            var splitter = new SplitContainer
            {
                Dock          = DockStyle.Fill,
                SplitterWidth = 5,
                BackColor     = CorBorda,
                BorderStyle   = BorderStyle.None,
                FixedPanel    = FixedPanel.Panel1
            };
            splitter.SplitterDistance = 280;

            MontarPainelEsquerdo(splitter.Panel1);
            MontarGrid(splitter.Panel2);

            Controls.Add(splitter);
        }

        // ── PAINEL ESQUERDO (categorias) ──────────────────────────────────────

        private void MontarPainelEsquerdo(SplitterPanel parent)
        {
            parent.BackColor = CorPainel;

            var pnlCat = new Panel { Dock = DockStyle.Fill, BackColor = CorPainel, Padding = new Padding(8) };
            parent.Controls.Add(pnlCat);

            var lblTitulo = new Label
            {
                Text      = "Categories",
                Font      = FonteTitulo,
                ForeColor = CorTexto,
                Dock      = DockStyle.Top,
                Height    = 28
            };
            pnlCat.Controls.Add(lblTitulo);

            // Select all / clear — FlowLayoutPanel evita overlap
            var pnlLinks = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 26,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = false
            };
            var lnkTudo = new LinkLabel
            {
                Text     = "Select all",
                AutoSize = true,
                Font     = FontePequena,
                Margin   = new Padding(0, 4, 8, 0)
            };
            lnkTudo.Click += (s, e) =>
            {
                for (int i = 0; i < clbCategorias.Items.Count; i++)
                    clbCategorias.SetItemChecked(i, true);
            };
            var lnkLimpar = new LinkLabel
            {
                Text     = "Clear",
                AutoSize = true,
                Font     = FontePequena,
                Margin   = new Padding(0, 4, 0, 0)
            };
            lnkLimpar.Click += (s, e) =>
            {
                for (int i = 0; i < clbCategorias.Items.Count; i++)
                    clbCategorias.SetItemChecked(i, false);
            };
            pnlLinks.Controls.Add(lnkTudo);
            pnlLinks.Controls.Add(lnkLimpar);
            pnlCat.Controls.Add(pnlLinks);
            pnlLinks.BringToFront();

            clbCategorias = new CheckedListBox
            {
                Dock                = DockStyle.Fill,
                BorderStyle         = BorderStyle.None,
                Font                = FonteNormal,
                CheckOnClick        = true,
                IntegralHeight      = false,
                BackColor           = CorPainel,
                HorizontalScrollbar = true
            };

            foreach (var cat in _porCategoria.Keys.OrderBy(c => c))
                clbCategorias.Items.Add(cat, true);  // todas marcadas por padrão

            // Calcular HorizontalExtent para mostrar nomes completos
            clbCategorias.HandleCreated += (s, e) => AjustarHorizontalExtent(clbCategorias);

            clbCategorias.ItemCheck += (s, e) =>
            {
                if (IsHandleCreated)
                    BeginInvoke((Action)AtualizarGrid);
            };

            pnlCat.Controls.Add(clbCategorias);
            clbCategorias.BringToFront();
            parent.Controls.Add(pnlCat);
        }

        // ── GRID (painel direito) ──────────────────────────────────────────────

        private void MontarGrid(SplitterPanel parent)
        {
            parent.BackColor = CorPainel;

            dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeight   = 28,
                RowHeadersVisible     = false,
                BackgroundColor       = CorPainel,
                GridColor             = CorBorda,
                BorderStyle           = BorderStyle.None,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                Font                  = FonteGrid,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.None,
                EnableHeadersVisualStyles = false
            };

            dgv.ColumnHeadersDefaultCellStyle.BackColor          = CorGridHeader;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor          = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font               = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = CorGridHeader;
            dgv.DefaultCellStyle.BackColor                  = CorPainel;
            dgv.DefaultCellStyle.ForeColor                  = CorTexto;
            dgv.DefaultCellStyle.SelectionBackColor         = Color.FromArgb(210, 230, 255);
            dgv.DefaultCellStyle.SelectionForeColor         = CorTexto;
            dgv.AlternatingRowsDefaultCellStyle.BackColor   = CorGridAlt;
            dgv.RowTemplate.Height = 22;

            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Categoria",   HeaderText = "Category",   Width = 160, MinimumWidth = 80 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Propriedade", HeaderText = "Property", Width = 210, MinimumWidth = 100 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Valor",       HeaderText = "Value",       Width = 300, MinimumWidth = 100,
                  AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Tipo",        HeaderText = "Type",        Width = 80,  MinimumWidth = 60 });

            parent.Controls.Add(dgv);
        }

        // ── ATUALIZAR GRID ─────────────────────────────────────────────────────

        private void AtualizarGrid()
        {
            var catsMarcadas = new HashSet<string>(clbCategorias == null
                ? _porCategoria.Keys
                : clbCategorias.CheckedItems.Cast<object>().Select(o => o.ToString()));

            var filtro = txtBusca?.Text.Trim().ToLower() ?? "";

            dgv.SuspendLayout();
            dgv.Rows.Clear();

            int count = 0;
            foreach (var cat in _porCategoria.Keys.OrderBy(c => c))
            {
                if (!catsMarcadas.Contains(cat)) continue;
                foreach (var prop in _porCategoria[cat])
                {
                    if (!string.IsNullOrEmpty(filtro))
                    {
                        bool match = (prop.Categoria?.ToLower().Contains(filtro) == true)
                                  || (prop.Nome?.ToLower().Contains(filtro) == true)
                                  || (prop.Valor?.ToLower().Contains(filtro) == true);
                        if (!match) continue;
                    }
                    dgv.Rows.Add(prop.Categoria, prop.Nome, prop.Valor, prop.Tipo ?? "string");
                    count++;
                }
            }

            dgv.ResumeLayout();

            if (lblStatus != null)
                lblStatus.Text = $"{count} property/properties  |  {catsMarcadas.Count} categories selected";
        }

        // ── HELPERS ───────────────────────────────────────────────────────────

        private static void AjustarHorizontalExtent(CheckedListBox clb)
        {
            using (var g = clb.CreateGraphics())
            {
                int maxW = 0;
                foreach (var item in clb.Items)
                {
                    int w = (int)g.MeasureString(item.ToString(), clb.Font).Width + 28; // 28 = checkbox
                    if (w > maxW) maxW = w;
                }
                clb.HorizontalExtent = maxW + 4;
            }
        }

        // ── COPIAR ─────────────────────────────────────────────────────────────

        private void BtnCopiar_Click(object sender, EventArgs e)
        {
            var linhas = new List<string>();
            foreach (DataGridViewRow row in dgv.Rows)
            {
                string cat  = row.Cells["Categoria"].Value?.ToString() ?? "";
                string prop = row.Cells["Propriedade"].Value?.ToString() ?? "";
                string val  = row.Cells["Valor"].Value?.ToString() ?? "";
                linhas.Add($"{cat}\t{prop}\t{val}");
            }
            if (!linhas.Any()) return;
            Clipboard.SetText(string.Join("\r\n", linhas));
            MessageBox.Show($"{linhas.Count} properties copied to clipboard.",
                "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
