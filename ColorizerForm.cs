using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Color = System.Drawing.Color;
using NwColor = Autodesk.Navisworks.Api.Color;

namespace AutisAnalytics.NavisworksAtributos
{
    public class ColorizerForm : Form
    {
        // ── Controles ──────────────────────────────────────────────────
        private RadioButton radioModels;
        private RadioButton radioSets;
        private DataGridView grid;
        private Label lblInfo;
        private Label lblTotal;
        private readonly Random _random = new Random();

        // ── Dados ──────────────────────────────────────────────────────
        private readonly List<SetInfo> _allSets = new List<SetInfo>();

        private class SetInfo
        {
            public string Nome { get; set; }
            public string Tipo { get; set; }
            public SavedItem Item { get; set; }
            public ModelItemCollection Itens { get; set; }
            public HashSet<ModelItem> ItensSet { get; set; }
            public Color CorAtribuida { get; set; }
            public bool Selecionado { get; set; }
        }

        public ColorizerForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            Montar();
            CarregarSetsAutomaticamente();
        }

        private void Montar()
        {
            Text = "Autis Analytics";
            Size = new Size(800, 600);
            MinimumSize = new Size(640, 480);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UITheme.Color.Background;
            Font = UITheme.Typography.Body;

            // ── Header ─────────────────────────────────────────────────
            var pnlHeader = UITheme.CreateHeader("Colorizer", "Color elements with random colors");
            Controls.Add(pnlHeader);

            // ── Footer ─────────────────────────────────────────────────
            var pnlFooter = UITheme.CreateFooter();

            lblInfo = new Label
            {
                Text = "",
                Font = UITheme.Typography.BodySmall,
                ForeColor = UITheme.Color.TextTertiary,
                AutoSize = true,
                Location = new Point(16, 16)
            };
            pnlFooter.Controls.Add(lblInfo);

            var btnFechar = UITheme.CreateSecondaryButton("Close");
            btnFechar.Width = 100;
            btnFechar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnFechar.Location = new Point(pnlFooter.Width - 116, 8);
            btnFechar.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnFechar);
            pnlFooter.Resize += (s, e) => btnFechar.Location = new Point(pnlFooter.Width - 116, 8);

            Controls.Add(pnlFooter);

            // ── Body ───────────────────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UITheme.Spacing.LG, UITheme.Spacing.MD, UITheme.Spacing.LG, UITheme.Spacing.MD)
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            // Card principal
            var card = UITheme.CreateCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(UITheme.Spacing.LG);
            pnlBody.Controls.Add(card);

            // ── Linha superior: Modo + Botões ──────────────────────────
            var pnlTopo = new Panel { Dock = DockStyle.Top, Height = 84 };

            // Radio buttons — modo
            radioModels = new RadioButton
            {
                Text = "Models / Files",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 4)
            };
            radioModels.CheckedChanged += (s, e) => AtualizarModo();
            pnlTopo.Controls.Add(radioModels);

            radioSets = new RadioButton
            {
                Text = "Selection / Search Sets",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextPrimary,
                AutoSize = true,
                Location = new Point(140, 4),
                Checked = true
            };
            radioSets.CheckedChanged += (s, e) => AtualizarModo();
            pnlTopo.Controls.Add(radioSets);

            lblTotal = new Label
            {
                Text = "",
                Font = UITheme.Typography.BodySmall,
                ForeColor = UITheme.Color.TextTertiary,
                AutoSize = true,
                Location = new Point(350, 8)
            };
            pnlTopo.Controls.Add(lblTotal);

            // Botões de ação
            var btnRandom = UITheme.CreatePrimaryButton("Random Colors");
            btnRandom.Location = new Point(0, 40);
            btnRandom.Size = new Size(180, 36);
            btnRandom.Click += BtnRandomColors_Click;
            pnlTopo.Controls.Add(btnRandom);

            var btnReset = new Button
            {
                Text = "Reset Colors",
                Font = UITheme.Typography.Label,
                ForeColor = Color.White,
                BackColor = UITheme.Color.Error,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(188, 40),
                Size = new Size(150, 36)
            };
            btnReset.FlatAppearance.BorderSize = 0;
            btnReset.MouseEnter += (s, e) => btnReset.BackColor = UITheme.Color.ErrorHover;
            btnReset.MouseLeave += (s, e) => btnReset.BackColor = UITheme.Color.Error;
            btnReset.Click += BtnResetAll_Click;
            pnlTopo.Controls.Add(btnReset);

            var btnSelAll = UITheme.CreateSecondaryButton("Select All");
            btnSelAll.Location = new Point(350, 42);
            btnSelAll.Size = new Size(130, 32);
            btnSelAll.Font = UITheme.Typography.BodySmall;
            btnSelAll.Click += (s, e) => { foreach (DataGridViewRow r in grid.Rows) r.Cells["Sel"].Value = true; };
            pnlTopo.Controls.Add(btnSelAll);

            var btnSelNone = UITheme.CreateSecondaryButton("Clear Selection");
            btnSelNone.Location = new Point(488, 42);
            btnSelNone.Size = new Size(120, 32);
            btnSelNone.Font = UITheme.Typography.BodySmall;
            btnSelNone.Click += (s, e) => { foreach (DataGridViewRow r in grid.Rows) r.Cells["Sel"].Value = false; };
            pnlTopo.Controls.Add(btnSelNone);

            card.Controls.Add(pnlTopo);

            // ── Grid ───────────────────────────────────────────────────
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 30 },
                ColumnHeadersHeight = 34,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ReadOnly = false
            };

            UITheme.StyleDataGridView(grid);

            // Colunas
            grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Sel", HeaderText = "", Width = 32,
                FillWeight = 8, AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Cor", HeaderText = "Cor", Width = 44,
                FillWeight = 8, ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Nome", HeaderText = "Set Name", FillWeight = 55, ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Tipo", HeaderText = "Tipo", FillWeight = 15, ReadOnly = true
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Qtd", HeaderText = "Elementos", FillWeight = 12, ReadOnly = true
            });

            grid.CellPainting += Grid_CellPainting;

            card.Controls.Add(grid);
            grid.BringToFront();
        }

        // ────────────────────────────────────────────────────────────────
        //  CARREGAR SETS AUTOMATICAMENTE
        // ────────────────────────────────────────────────────────────────

        private void CarregarSetsAutomaticamente()
        {
            _allSets.Clear();
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) return;

            foreach (var setInfo in SelectionSetCache.Collect(doc))
            {
                _allSets.Add(new SetInfo
                {
                    Nome = setInfo.Nome,
                    Tipo = setInfo.Tipo,
                    Item = setInfo.Item,
                    Itens = setInfo.Itens,
                    ItensSet = setInfo.ItensSet,
                    CorAtribuida = Color.Transparent,
                    Selecionado = false
                });
            }

            PreencherGrid();
            AutoMarcarSetsComSelecao(doc);
        }

        private void AutoMarcarSetsComSelecao(Document doc)
        {
            var selecao = doc.CurrentSelection.SelectedItems;
            if (selecao.Count == 0) return;

            var selecaoSet = new HashSet<ModelItem>();
            foreach (ModelItem mi in selecao)
                selecaoSet.Add(mi);

            for (int i = 0; i < _allSets.Count; i++)
            {
                var si = _allSets[i];
                if (si.ItensSet != null && si.ItensSet.Overlaps(selecaoSet))
                    grid.Rows[i].Cells["Sel"].Value = true;
            }
        }

        private void PreencherGrid()
        {
            grid.Rows.Clear();
            foreach (var si in _allSets)
            {
                int idx = grid.Rows.Add();
                grid.Rows[idx].Cells["Sel"].Value = false;
                grid.Rows[idx].Cells["Cor"].Value = "";
                grid.Rows[idx].Cells["Nome"].Value = si.Nome;
                grid.Rows[idx].Cells["Tipo"].Value = si.Tipo;
                grid.Rows[idx].Cells["Qtd"].Value = "";
                grid.Rows[idx].Tag = si;
            }

            lblTotal.Text = $"Total: {_allSets.Count} sets";
        }

        // ────────────────────────────────────────────────────────────────
        //  CELL PAINTING — Coluna de cor
        // ────────────────────────────────────────────────────────────────

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].Name != "Cor") return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

            var si = grid.Rows[e.RowIndex].Tag as SetInfo;
            if (si != null && si.CorAtribuida != Color.Transparent)
            {
                var rect = new Rectangle(
                    e.CellBounds.X + 8, e.CellBounds.Y + 5,
                    e.CellBounds.Width - 16, e.CellBounds.Height - 10);

                using (var path = CriarRoundedRect(rect, 3))
                using (var brush = new SolidBrush(si.CorAtribuida))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
                using (var path = CriarRoundedRect(rect, 3))
                using (var pen = new Pen(Color.FromArgb(60, 0, 0, 0)))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            e.Handled = true;
        }

        // ────────────────────────────────────────────────────────────────
        //  MODO
        // ────────────────────────────────────────────────────────────────

        private void AtualizarModo()
        {
            grid.Enabled = radioSets.Checked;
            grid.BackgroundColor = radioSets.Checked
                ? UITheme.Color.Surface
                : UITheme.Color.BackgroundSecondary;
        }

        // ────────────────────────────────────────────────────────────────
        //  CORES ALEATÓRIAS
        // ────────────────────────────────────────────────────────────────

        private void BtnRandomColors_Click(object sender, EventArgs e)
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null || doc.IsClear) return;

            if (radioModels.Checked)
                ColorizarModels(doc);
            else
                ColorizarSets(doc);
        }

        private void ColorizarModels(Document doc)
        {
            int count = 0;
            var ext = System.IO.Path.GetExtension(doc.CurrentFileName ?? "").ToLower();

            using (var trans = doc.BeginTransaction("Autis_Color_Models"))
            {
                if (ext == ".nwd")
                {
                    foreach (var root in doc.Models.RootItems)
                        foreach (var child in root.Children)
                        {
                            var coll = new ModelItemCollection();
                            coll.Add(child);
                            doc.Models.OverridePermanentColor(
                                (System.Collections.Generic.IEnumerable<ModelItem>)coll,
                                GerarNwCorAleatoria());
                            count++;
                        }
                }
                else
                {
                    foreach (var root in doc.Models.RootItems)
                    {
                        var coll = new ModelItemCollection();
                        coll.Add(root);
                        doc.Models.OverridePermanentColor(
                            (System.Collections.Generic.IEnumerable<ModelItem>)coll,
                            GerarNwCorAleatoria());
                        count++;
                    }
                }

                trans.Commit();
            }

            lblInfo.ForeColor = UITheme.Color.Success;
            lblInfo.Text = $"✓ {count} model(s) colored!";
        }

        private void ColorizarSets(Document doc)
        {
            var setsParaColorir = new List<int>();

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var chk = grid.Rows[i].Cells["Sel"].Value;
                if (chk is true) setsParaColorir.Add(i);
            }

            if (setsParaColorir.Count == 0)
            {
                ToastNotification.Show("Select at least one set to color.", ToastNotification.ToastType.Warning);
                return;
            }

            var cores = GerarCoresUnicas(setsParaColorir.Count);

            int count = 0;
            int totalElementos = 0;

            using (var trans = doc.BeginTransaction("Autis_Color_Sets"))
            {
                for (int i = 0; i < setsParaColorir.Count; i++)
                {
                    int idx = setsParaColorir[i];
                    var si = _allSets[idx];
                    var coll = si.Itens;

                    if (coll != null && coll.Count > 0)
                    {
                        var (r, g, b) = cores[i];
                        var nwCor = new NwColor(r / 255.0, g / 255.0, b / 255.0);

                        doc.Models.OverridePermanentColor(
                            (System.Collections.Generic.IEnumerable<ModelItem>)coll, nwCor);

                        si.CorAtribuida = Color.FromArgb(r, g, b);
                        grid.Rows[idx].Cells["Qtd"].Value = coll.Count.ToString();

                        count++;
                        totalElementos += coll.Count;
                    }
                }

                trans.Commit();
            }

            grid.InvalidateColumn(grid.Columns["Cor"].Index);

            lblInfo.ForeColor = UITheme.Color.Success;
            lblInfo.Text = $"✓ {count} set(s) colored — {totalElementos} elements";
        }

        // ── Reset ────────────────────────────────────────────────────

        private void BtnResetAll_Click(object sender, EventArgs e)
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null || doc.IsClear) return;

            doc.Models.ResetAllPermanentMaterials();
            doc.Models.ResetAllTemporaryMaterials();

            foreach (var si in _allSets)
                si.CorAtribuida = Color.Transparent;

            foreach (DataGridViewRow row in grid.Rows)
                row.Cells["Qtd"].Value = "";

            grid.InvalidateColumn(grid.Columns["Cor"].Index);

            ToastNotification.Show("All colors have been reset.", ToastNotification.ToastType.Info);
            lblInfo.ForeColor = UITheme.Color.TextTertiary;
            lblInfo.Text = "";
        }

        // ── Helpers ──────────────────────────────────────────────────

        private List<(int r, int g, int b)> GerarCoresUnicas(int quantidade)
        {
            var cores = new List<(int r, int g, int b)>();
            double goldenRatio = 0.618033988749895;
            double hue = _random.NextDouble();

            for (int i = 0; i < quantidade; i++)
            {
                hue += goldenRatio;
                hue %= 1.0;
                double sat = 0.7 + (_random.NextDouble() * 0.2);
                double lum = 0.45 + (_random.NextDouble() * 0.15);

                double q = lum < 0.5 ? lum * (1 + sat) : lum + sat - lum * sat;
                double p = 2 * lum - q;
                double rd = HueToRgb(p, q, hue + 1.0 / 3.0);
                double gd = HueToRgb(p, q, hue);
                double bd = HueToRgb(p, q, hue - 1.0 / 3.0);
                cores.Add(((int)(rd * 255), (int)(gd * 255), (int)(bd * 255)));
            }
            return cores;
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
            return p;
        }

        private NwColor GerarNwCorAleatoria()
        {
            return new NwColor(
                _random.Next(40, 240) / 255.0,
                _random.Next(40, 240) / 255.0,
                _random.Next(40, 240) / 255.0);
        }

        private static GraphicsPath CriarRoundedRect(Rectangle rect, int raio)
        {
            var path = new GraphicsPath();
            int d = raio * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
