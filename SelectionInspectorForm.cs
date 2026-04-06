using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;

namespace AutisAnalytics.NavisworksAtributos
{
    public class SelectionInspectorForm : Form
    {
        // ── Controles ──────────────────────────────────────────────────
        private CheckedListBox clbCategorias;
        private DataGridView grid;
        private Label lblStatus;

        private readonly ModelItemCollection _itens;

        // Dados: lista de dicionários (chave = "Categoria\Propriedade", valor = string)
        private List<Dictionary<string, string>> _dadosPorItem;
        // Todas as categorias disponíveis
        private Dictionary<string, List<string>> _categoriaProps;
        // Colunas atualmente visíveis
        private List<string> _colunasVisiveis;

        public SelectionInspectorForm(ModelItemCollection itens)
        {
            _itens = itens;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            CarregarDados();
            Montar();
        }

        // ────────────────────────────────────────────────────────────────
        //  CARREGAR DADOS
        // ────────────────────────────────────────────────────────────────

        private void CarregarDados()
        {
            _dadosPorItem = new List<Dictionary<string, string>>();
            _categoriaProps = new Dictionary<string, List<string>>();
            var categoriaPropsSet = new Dictionary<string, HashSet<string>>();

            foreach (ModelItem item in _itens)
            {
                var props = new Dictionary<string, string>();

                foreach (var cat in item.PropertyCategories)
                {
                    var catName = cat.DisplayName ?? cat.Name;
                    if (!_categoriaProps.ContainsKey(catName))
                    {
                        _categoriaProps[catName] = new List<string>();
                        categoriaPropsSet[catName] = new HashSet<string>();
                    }

                    foreach (var prop in cat.Properties)
                    {
                        var propName = prop.DisplayName ?? prop.Name;
                        var chave = catName + "\n" + propName;
                        var valor = FormatarValor(prop.Value);

                        props[chave] = valor;

                        if (categoriaPropsSet[catName].Add(propName))
                            _categoriaProps[catName].Add(propName);
                    }
                }

                _dadosPorItem.Add(props);
            }
        }

        private static string FormatarValor(VariantData vd)
        {
            if (vd == null || vd.IsNone) return "";
            try { return vd.IsDisplayString ? vd.ToDisplayString() : vd.ToString(); }
            catch { return ""; }
        }

        // ────────────────────────────────────────────────────────────────
        //  MONTAR UI
        // ────────────────────────────────────────────────────────────────

        private void Montar()
        {
            Text = "Autis Analytics — Selection Inspector";
            Size = new Size(1200, 720);
            MinimumSize = new Size(900, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UITheme.Color.Background;
            Font = UITheme.Typography.Body;

            // ── Header ──
            var pnlHeader = UITheme.CreateHeader(
                "Selection Inspector",
                $"{_itens.Count} element(s) selected  |  {_categoriaProps.Count} categories");
            Controls.Add(pnlHeader);

            // ── Footer ──
            var pnlFooter = UITheme.CreateFooter();

            lblStatus = new Label
            {
                Text = "Select categories to display",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextTertiary,
                AutoSize = true,
                Location = new Point(UITheme.Spacing.LG, 16)
            };
            pnlFooter.Controls.Add(lblStatus);

            var btnFechar = UITheme.CreateSecondaryButton("Close");
            btnFechar.Width = 90;
            btnFechar.DialogResult = DialogResult.Cancel;
            pnlFooter.Controls.Add(btnFechar);

            var btnExportCsv = UITheme.CreatePrimaryButton("Export CSV");
            btnExportCsv.Width = 110;
            btnExportCsv.Click += BtnExportCsv_Click;
            pnlFooter.Controls.Add(btnExportCsv);

            var btnExportExcel = UITheme.CreateSuccessButton("Export Excel");
            btnExportExcel.Width = 110;
            btnExportExcel.Click += BtnExportExcel_Click;
            pnlFooter.Controls.Add(btnExportExcel);

            pnlFooter.Resize += (s, e) =>
            {
                btnFechar.Location = new Point(pnlFooter.Width - 106, 8);
                btnExportCsv.Location = new Point(pnlFooter.Width - 228, 8);
                btnExportExcel.Location = new Point(pnlFooter.Width - 350, 8);
            };

            Controls.Add(pnlFooter);
            CancelButton = btnFechar;

            // ── Body ──
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UITheme.Spacing.MD, UITheme.Spacing.SM, UITheme.Spacing.MD, 0)
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 6,
                BackColor = UITheme.Color.Background,
                BorderStyle = BorderStyle.None,
                FixedPanel = FixedPanel.Panel1,
                SplitterDistance = 260
            };
            pnlBody.Controls.Add(splitter);

            MontarPainelCategorias(splitter.Panel1);
            MontarGrid(splitter.Panel2);
        }

        // ────────────────────────────────────────────────────────────────
        //  PAINEL CATEGORIAS
        // ────────────────────────────────────────────────────────────────

        private void MontarPainelCategorias(Control parent)
        {
            var card = UITheme.CreateCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(UITheme.Spacing.SM);
            parent.Controls.Add(card);

            var lbl = new Label
            {
                Text = "Categories",
                Font = UITheme.Typography.H3,
                ForeColor = UITheme.Color.TextPrimary,
                Dock = DockStyle.Top,
                Height = 28
            };
            card.Controls.Add(lbl);

            // FlowLayoutPanel evita overlap entre "Selecionar Tudo" e "Limpar"
            var pnlBtns = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 28,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = false
            };
            var btnAll = new LinkLabel
            {
                Text            = "Select All",
                AutoSize        = true,
                Font            = UITheme.Typography.BodySmall,
                LinkColor       = UITheme.Color.Primary,
                ActiveLinkColor = UITheme.Color.PrimaryHover,
                Margin          = new Padding(0, 4, 8, 0)
            };
            btnAll.Click += (s, e) =>
            {
                for (int i = 0; i < clbCategorias.Items.Count; i++)
                    clbCategorias.SetItemChecked(i, true);
            };

            var btnNone = new LinkLabel
            {
                Text            = "Clear",
                AutoSize        = true,
                Font            = UITheme.Typography.BodySmall,
                LinkColor       = UITheme.Color.Error,
                ActiveLinkColor = UITheme.Color.ErrorHover,
                Margin          = new Padding(0, 4, 0, 0)
            };
            btnNone.Click += (s, e) =>
            {
                for (int i = 0; i < clbCategorias.Items.Count; i++)
                    clbCategorias.SetItemChecked(i, false);
            };

            pnlBtns.Controls.Add(btnAll);
            pnlBtns.Controls.Add(btnNone);
            card.Controls.Add(pnlBtns);
            pnlBtns.BringToFront();

            clbCategorias = new CheckedListBox
            {
                Dock                = DockStyle.Fill,
                BorderStyle         = BorderStyle.None,
                Font                = UITheme.Typography.Body,
                CheckOnClick        = true,
                IntegralHeight      = false,
                HorizontalScrollbar = true,
                BackColor           = UITheme.Color.Surface,
                ForeColor           = UITheme.Color.TextPrimary
            };

            foreach (var cat in _categoriaProps.Keys.OrderBy(c => c))
                clbCategorias.Items.Add(cat, false);

            // Calcular HorizontalExtent para mostrar nomes completos
            clbCategorias.HandleCreated += (s, e) => AjustarHorizontalExtent(clbCategorias);

            clbCategorias.ItemCheck += (s, e) =>
            {
                if (IsHandleCreated)
                    BeginInvoke((Action)(() => AtualizarGrid()));
            };

            card.Controls.Add(clbCategorias);
            clbCategorias.BringToFront();
        }

        // ────────────────────────────────────────────────────────────────
        //  GRID
        // ────────────────────────────────────────────────────────────────

        private void MontarGrid(Control parent)
        {
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RowHeadersVisible = true,
                RowHeadersWidth = 42,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 28 },
                ColumnHeadersHeight = 44,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
            };

            UITheme.StyleDataGridView(grid);

            // Permitir wrap no header para "Categoria\nPropriedade"
            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;

            parent.Controls.Add(grid);
        }

        // ────────────────────────────────────────────────────────────────
        //  ATUALIZAR GRID
        // ────────────────────────────────────────────────────────────────

        private void AtualizarGrid()
        {
            grid.SuspendLayout();
            grid.Columns.Clear();
            grid.Rows.Clear();

            var catsMarcadas = new List<string>();
            foreach (var item in clbCategorias.CheckedItems)
                catsMarcadas.Add(item.ToString());

            if (catsMarcadas.Count == 0)
            {
                lblStatus.Text = "Select categories to display";
                grid.ResumeLayout();
                return;
            }

            _colunasVisiveis = new List<string>();
            foreach (var cat in catsMarcadas.OrderBy(c => c))
            {
                if (!_categoriaProps.ContainsKey(cat)) continue;
                foreach (var prop in _categoriaProps[cat])
                {
                    var chave = cat + "\n" + prop;
                    _colunasVisiveis.Add(chave);

                    var col = new DataGridViewTextBoxColumn
                    {
                        Name = chave,
                        HeaderText = cat + "\n" + prop,
                        Width = 130,
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };
                    grid.Columns.Add(col);
                }
            }

            for (int i = 0; i < _dadosPorItem.Count; i++)
            {
                int rowIdx = grid.Rows.Add();
                grid.Rows[rowIdx].HeaderCell.Value = (i + 1).ToString();

                var dados = _dadosPorItem[i];
                foreach (var chave in _colunasVisiveis)
                {
                    string valor;
                    if (dados.TryGetValue(chave, out valor))
                        grid.Rows[rowIdx].Cells[chave].Value = valor;
                }
            }

            grid.ResumeLayout();

            lblStatus.Text = $"{_itens.Count} element(s)  |  {catsMarcadas.Count} categories  |  {_colunasVisiveis.Count} columns";
        }

        // ────────────────────────────────────────────────────────────────
        //  EXPORT CSV
        // ────────────────────────────────────────────────────────────────

        private void BtnExportCsv_Click(object sender, EventArgs e)
        {
            if (_colunasVisiveis == null || _colunasVisiveis.Count == 0)
            {
                ToastNotification.Show("Select categories first.", ToastNotification.ToastType.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV (*.csv)|*.csv";
                sfd.Title = "Export to CSV";
                sfd.FileName = "selection_inspector";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();

                sb.AppendLine(string.Join(",",
                    _colunasVisiveis.Select(c => EscaparCsv(c.Replace("\n", " / ")))));

                for (int i = 0; i < _dadosPorItem.Count; i++)
                {
                    var dados = _dadosPorItem[i];
                    var valores = _colunasVisiveis.Select(c =>
                    {
                        string v;
                        return dados.TryGetValue(c, out v) ? EscaparCsv(v) : "";
                    });
                    sb.AppendLine(string.Join(",", valores));
                }

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                ToastNotification.Show($"CSV exported successfully!", ToastNotification.ToastType.Success);
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  EXPORT EXCEL (XML Spreadsheet)
        // ────────────────────────────────────────────────────────────────

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (_colunasVisiveis == null || _colunasVisiveis.Count == 0)
            {
                ToastNotification.Show("Select categories first.", ToastNotification.ToastType.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel XML (*.xml)|*.xml";
                sfd.Title = "Export to Excel";
                sfd.FileName = "selection_inspector";
                if (sfd.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
                sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
                sb.AppendLine(" <Styles>");
                sb.AppendLine("  <Style ss:ID=\"header\"><Font ss:Bold=\"1\" ss:Size=\"10\"/>");
                sb.AppendLine("   <Interior ss:Color=\"#D9E1F2\" ss:Pattern=\"Solid\"/></Style>");
                sb.AppendLine(" </Styles>");
                sb.AppendLine(" <Worksheet ss:Name=\"Selection Inspector\">");
                sb.AppendLine($"  <Table ss:ExpandedColumnCount=\"{_colunasVisiveis.Count}\" ss:ExpandedRowCount=\"{_dadosPorItem.Count + 1}\">");

                sb.Append("   <Row>");
                foreach (var col in _colunasVisiveis)
                    sb.Append($"<Cell ss:StyleID=\"header\"><Data ss:Type=\"String\">{EscaparXml(col.Replace("\n", " / "))}</Data></Cell>");
                sb.AppendLine("</Row>");

                for (int i = 0; i < _dadosPorItem.Count; i++)
                {
                    sb.Append("   <Row>");
                    var dados = _dadosPorItem[i];
                    foreach (var col in _colunasVisiveis)
                    {
                        string v;
                        var valor = dados.TryGetValue(col, out v) ? v : "";
                        sb.Append($"<Cell><Data ss:Type=\"String\">{EscaparXml(valor)}</Data></Cell>");
                    }
                    sb.AppendLine("</Row>");
                }

                sb.AppendLine("  </Table>");
                sb.AppendLine(" </Worksheet>");
                sb.AppendLine("</Workbook>");

                File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                ToastNotification.Show($"Excel exported successfully!", ToastNotification.ToastType.Success);
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────────────────

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

        private static string EscaparCsv(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            if (valor.Contains(",") || valor.Contains("\"") || valor.Contains("\n") || valor.Contains("\r"))
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            return valor;
        }

        private static string EscaparXml(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            return valor.Replace("&", "&amp;").Replace("<", "&lt;")
                        .Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
