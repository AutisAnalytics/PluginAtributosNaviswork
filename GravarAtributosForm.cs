using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace AutisAnalytics.NavisworksAtributos
{
    /// <summary>
    /// Formulário moderno para gravação de atributos customizados.
    ///
    /// Layout:
    ///   ┌──────────────────┬───────────────────────────────────┐
    ///   │  Header (info)   │                                   │
    ///   ├──────────────────┼───────────────────────────────────┤
    ///   │  Pesquisar...    │  Categoria: [___________]         │
    ///   │  ┌────────────┐  │  ┌───────────────────────────┐   │
    ///   │  │ Categorias │  │  │ Nome │ Valor │ Tipo │  ✕  │   │
    ///   │  │ existentes │  │  │      │       │      │     │   │
    ///   │  │ (clique p/ │  │  └───────────────────────────┘   │
    ///   │  │  importar) │  │  [+ Adicionar]  [- Remover]      │
    ///   │  └────────────┘  │                                   │
    ///   │                  │           [Cancelar] [Gravar]     │
    ///   └──────────────────┴───────────────────────────────────┘
    /// </summary>
    public class GravarAtributosForm : Form
    {
        private const string CATEGORIA_FIXA = AutisSchema.CategoriaPrincipal;

        public class SetResumo
        {
            public string Nome { get; }
            public int ItensSelecionados { get; }

            public SetResumo(string nome, int itensSelecionados)
            {
                Nome = nome;
                ItensSelecionados = itensSelecionados;
            }
        }

        // ── Cores do tema ──────────────────────────────────────────────
        private static readonly Color CorFundo       = Color.FromArgb(245, 246, 250);
        private static readonly Color CorPainel      = Color.White;
        private static readonly Color CorHeader      = Color.FromArgb(44, 62, 80);
        private static readonly Color CorAccent      = Color.FromArgb(0, 120, 215);
        private static readonly Color CorAccentHover = Color.FromArgb(0, 100, 190);
        private static readonly Color CorTexto       = Color.FromArgb(50, 50, 50);
        private static readonly Color CorTextoClaro  = Color.FromArgb(130, 130, 130);
        private static readonly Color CorBorda       = Color.FromArgb(220, 222, 228);
        private static readonly Color CorGridHeader  = Color.FromArgb(248, 249, 252);
        private static readonly Color CorGridAlt     = Color.FromArgb(250, 251, 254);
        private static readonly Color CorPerigo      = Color.FromArgb(220, 53, 69);
        private static readonly Color CorOk          = Color.FromArgb(0, 150, 70);

        private static readonly Font FonteNormal     = new Font("Segoe UI", 9.5f);
        private static readonly Font FonteSemibold   = new Font("Segoe UI Semibold", 9.5f);
        private static readonly Font FonteTitulo     = new Font("Segoe UI Semibold", 11f);
        private static readonly Font FonteHeader     = new Font("Segoe UI", 13f, FontStyle.Bold);
        private static readonly Font FontePequena    = new Font("Segoe UI", 8.5f);
        private static readonly Font FonteGrid       = new Font("Segoe UI", 9f);

        // ── Controles ──────────────────────────────────────────────────
        private Panel pnlHeader;
        private Panel pnlEsquerdo;
        private Panel pnlDireito;
        private TextBox txtPesquisa;
        private ListView lvCategorias;
        private TextBox txtCategoria;
        private DataGridView grid;
        private Label lblSubHeader;
        private Label lblResumoSets;
        private Label lblSelecaoSets;

        // ── Dados ──────────────────────────────────────────────────────
        private readonly List<SetResumo> _setsExistentes;
        private readonly HashSet<string> _setsSelecionados;
        private readonly int _qtdSelecionados;
        private bool _atualizandoListaSets;

        public string NomeCategoria { get; private set; }
        public List<AtributoCustom> Atributos { get; private set; }
        public List<string> NomesSetsSelecionados { get; private set; }
        public bool SolicitarExclusao { get; private set; }

        private string _nomeSetDetectado;

        public GravarAtributosForm(int qtdSelecionados, List<SetResumo> setsExistentes,
            string nomeSetDetectado = null,
            IEnumerable<string> setsInicialmenteSelecionados = null)
        {
            Atributos = new List<AtributoCustom>();
            _setsExistentes = setsExistentes ?? new List<SetResumo>();
            var selecaoInicial = (setsInicialmenteSelecionados ?? Enumerable.Empty<string>())
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Select(nome => nome.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _setsSelecionados = selecaoInicial.Count > 0
                ? new HashSet<string>(selecaoInicial, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(
                    _setsExistentes
                        .Where(s => !string.IsNullOrWhiteSpace(s?.Nome))
                        .Select(s => s.Nome),
                    StringComparer.OrdinalIgnoreCase);
            NomesSetsSelecionados = new List<string>();
            _qtdSelecionados = qtdSelecionados;
            _nomeSetDetectado = nomeSetDetectado;

            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            Montar(qtdSelecionados);
        }

        // ────────────────────────────────────────────────────────────────
        //  MONTAGEM DO LAYOUT
        // ────────────────────────────────────────────────────────────────

        private void Montar(int qtdSelecionados)
        {
            Text = "Autis Analytics";
            Size = new Size(920, 600);
            MinimumSize = new Size(780, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = CorFundo;
            Font = FonteNormal;

            // ── Header ─────────────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = CorHeader,
                Padding = new Padding(20, 0, 20, 0)
            };

            var lblTitulo = new Label
            {
                Text = "Write Properties",
                Font = FonteHeader,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 8)
            };
            pnlHeader.Controls.Add(lblTitulo);

            lblSubHeader = new Label
            {
                Font = FonteNormal,
                ForeColor = Color.FromArgb(180, 200, 220),
                AutoSize = true,
                Location = new Point(20, 38)
            };
            pnlHeader.Controls.Add(lblSubHeader);
            AtualizarResumoSetsUI();

            Controls.Add(pnlHeader);

            // ── Container principal ────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16, 12, 16, 12)
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            // ── Splitter esquerdo / direito ────────────────────────────
            var splitter = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 8,
                BackColor = CorFundo,
                BorderStyle = BorderStyle.None
            };
            splitter.Panel1.BackColor = CorFundo;
            splitter.Panel2.BackColor = CorFundo;
            pnlBody.Controls.Add(splitter);

            // Centralizar 50/50 após o layout estar pronto
            splitter.SplitterDistance = splitter.Width / 2;
            pnlBody.Resize += (s, e) => splitter.SplitterDistance = splitter.Width / 2;

            MontarPainelEsquerdo(splitter.Panel1);
            MontarPainelDireito(splitter.Panel2);

            // ── Footer ─────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = CorFundo,
                Padding = new Padding(16, 8, 16, 8)
            };

            var btnCancelar = CriarBotao("Cancel", CorPainel, CorTexto, CorBorda);
            btnCancelar.DialogResult = DialogResult.Cancel;
            btnCancelar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnCancelar.Location = new Point(pnlFooter.Width - 342, 10);
            pnlFooter.Controls.Add(btnCancelar);

            var btnExcluir = CriarBotao("Delete Created", CorPerigo, Color.White, CorPerigo);
            btnExcluir.Font = FonteSemibold;
            btnExcluir.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnExcluir.Location = new Point(pnlFooter.Width - 230, 10);
            btnExcluir.Click += BtnExcluir_Click;
            pnlFooter.Controls.Add(btnExcluir);

            var btnGravar = CriarBotao("Save", CorAccent, Color.White, CorAccent);
            btnGravar.Font = FonteSemibold;
            btnGravar.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnGravar.Location = new Point(pnlFooter.Width - 118, 10);
            btnGravar.Click += BtnGravar_Click;
            pnlFooter.Controls.Add(btnGravar);

            // Reposicionar ao redimensionar
            pnlFooter.Resize += (s, e) =>
            {
                btnGravar.Location = new Point(pnlFooter.Width - 118, 10);
                btnExcluir.Location = new Point(pnlFooter.Width - 230, 10);
                btnCancelar.Location = new Point(pnlFooter.Width - 342, 10);
            };

            Controls.Add(pnlFooter);
            pnlFooter.BringToFront();

            AcceptButton = btnGravar;
            CancelButton = btnCancelar;
        }

        // ────────────────────────────────────────────────────────────────
        //  PAINEL ESQUERDO — Categoria fixa + Sets detectados
        // ────────────────────────────────────────────────────────────────

        private TextBox txtNovaCategoria;

        private void MontarPainelEsquerdo(Control parent)
        {
            pnlEsquerdo = CriarPainelCard();
            pnlEsquerdo.Dock = DockStyle.Fill;
            parent.Padding = new Padding(0, 0, 4, 0);
            parent.Controls.Add(pnlEsquerdo);

            // ── Seção: Criar Nova Categoria ──────────────────────────────
            var lblCriar = new Label
            {
                Text = "Storage Category",
                Font = FonteTitulo,
                ForeColor = CorTexto,
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(0, 2, 0, 0)
            };
            pnlEsquerdo.Controls.Add(lblCriar);

            var lblDicaCriar = new Label
            {
                Text = "All created attributes are stored under this fixed category",
                Font = FontePequena,
                ForeColor = CorTextoClaro,
                Dock = DockStyle.Top,
                Height = 18
            };
            pnlEsquerdo.Controls.Add(lblDicaCriar);
            lblDicaCriar.BringToFront();

            // Campo + botão criar
            var pnlCriar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(0, 4, 0, 4)
            };

            var btnCriar = CriarBotaoPequeno("Reset", CorAccent);
            btnCriar.Size = new Size(70, 26);
            btnCriar.Dock = DockStyle.Right;
            btnCriar.Click += BtnCriarCategoria_Click;
            pnlCriar.Controls.Add(btnCriar);

            txtNovaCategoria = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = FonteNormal,
                ForeColor = CorTexto,
                Text = CATEGORIA_FIXA,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };
            pnlCriar.Controls.Add(txtNovaCategoria);

            pnlEsquerdo.Controls.Add(pnlCriar);
            pnlCriar.BringToFront();

            // Separador visual
            var separador = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = CorBorda,
                Margin = new Padding(0, 6, 0, 6)
            };
            pnlEsquerdo.Controls.Add(separador);
            separador.BringToFront();

            // Espaçador
            var espaco = new Panel { Dock = DockStyle.Top, Height = 8 };
            pnlEsquerdo.Controls.Add(espaco);
            espaco.BringToFront();

            // ── Seção: Sets Detectados ──────────────────────────────────
            var lblExistentes = new Label
            {
                Text = "Detected Sets",
                Font = FonteSemibold,
                ForeColor = CorTexto,
                Dock = DockStyle.Top,
                Height = 24
            };
            pnlEsquerdo.Controls.Add(lblExistentes);
            lblExistentes.BringToFront();

            var lblDica = new Label
            {
                Text = $"Check the set names you want to include in the fixed {AutisSchema.PropriedadeSets} field",
                Font = FontePequena,
                ForeColor = CorTextoClaro,
                Dock = DockStyle.Top,
                Height = 18
            };
            pnlEsquerdo.Controls.Add(lblDica);
            lblDica.BringToFront();

            var pnlResumoSets = new Panel
            {
                Dock = DockStyle.Top,
                Height = 54,
                BackColor = Color.FromArgb(238, 245, 255),
                Padding = new Padding(10, 6, 10, 6)
            };
            pnlResumoSets.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, pnlResumoSets.Width - 1, pnlResumoSets.Height - 1);
                using (var pen = new Pen(Color.FromArgb(206, 223, 248)))
                    e.Graphics.DrawRectangle(pen, rect);
            };

            lblResumoSets = new Label
            {
                Dock = DockStyle.Fill,
                Font = FontePequena,
                ForeColor = Color.FromArgb(36, 76, 126),
                Text = ObterTextoResumoSets()
            };
            pnlResumoSets.Controls.Add(lblResumoSets);
            pnlEsquerdo.Controls.Add(pnlResumoSets);
            pnlResumoSets.BringToFront();

            var pnlSelecaoSets = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(0, 4, 0, 2)
            };

            var btnLimparSets = CriarBotaoPequeno("Clear", CorTextoClaro);
            btnLimparSets.Size = new Size(66, 26);
            btnLimparSets.Dock = DockStyle.Right;
            btnLimparSets.Click += (s, e) => DefinirSelecaoSets(false);
            pnlSelecaoSets.Controls.Add(btnLimparSets);

            var btnSelecionarTodos = CriarBotaoPequeno("Select All", CorAccent);
            btnSelecionarTodos.Size = new Size(84, 26);
            btnSelecionarTodos.Dock = DockStyle.Right;
            btnSelecionarTodos.Margin = new Padding(0, 0, 8, 0);
            btnSelecionarTodos.Click += (s, e) => DefinirSelecaoSets(true);
            pnlSelecaoSets.Controls.Add(btnSelecionarTodos);

            lblSelecaoSets = new Label
            {
                Dock = DockStyle.Fill,
                Font = FontePequena,
                ForeColor = CorTextoClaro,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSelecaoSets.Controls.Add(lblSelecaoSets);

            pnlEsquerdo.Controls.Add(pnlSelecaoSets);
            pnlSelecaoSets.BringToFront();

            // Pesquisa
            var pnlPesquisa = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                Padding = new Padding(0, 4, 0, 4)
            };
            txtPesquisa = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = FonteNormal,
                ForeColor = CorTextoClaro,
                Text = "Search sets...",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtPesquisa.GotFocus += (s, e) =>
            {
                if (txtPesquisa.ForeColor == CorTextoClaro)
                {
                    txtPesquisa.Text = "";
                    txtPesquisa.ForeColor = CorTexto;
                }
            };
            txtPesquisa.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtPesquisa.Text))
                {
                    txtPesquisa.Text = "Search sets...";
                    txtPesquisa.ForeColor = CorTextoClaro;
                }
            };
            txtPesquisa.TextChanged += (s, e) => FiltrarSets();
            pnlPesquisa.Controls.Add(txtPesquisa);
            pnlEsquerdo.Controls.Add(pnlPesquisa);
            pnlPesquisa.BringToFront();

            // Lista de categorias
            lvCategorias = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                CheckBoxes = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                Font = FonteGrid,
                GridLines = false,
                MultiSelect = false,
                ShowItemToolTips = true,
                HotTracking = true,
                Activation = ItemActivation.OneClick
            };
            lvCategorias.Columns.Add("Set", 220);
            lvCategorias.Columns.Add("Items", 60, HorizontalAlignment.Center);
            lvCategorias.ItemChecked += LvCategorias_ItemChecked;

            pnlEsquerdo.Controls.Add(lvCategorias);
            lvCategorias.BringToFront();

            PreencherSets();
        }

        // ── Criar nova categoria ─────────────────────────────────────────

        private void BtnCriarCategoria_Click(object sender, EventArgs e)
        {
            // Setar categoria no painel direito e limpar grid para novas propriedades
            AplicarCategoriaFixa();
            SolicitarExclusao = false;
            grid.Rows.Clear();
            AdicionarLinhaGrid("", "", "string");

            // Focar no grid para o usuário começar a preencher
            grid.Focus();
            if (grid.Rows.Count > 0)
                grid.CurrentCell = grid.Rows[0].Cells["Nome"];
        }

        // ── Preencher / filtrar lista ────────────────────────────────────

        private void PreencherSets()
        {
            _atualizandoListaSets = true;
            lvCategorias.Items.Clear();
            var filtro = "";
            if (txtPesquisa != null && txtPesquisa.ForeColor != CorTextoClaro)
                filtro = txtPesquisa.Text.Trim().ToLower();

            foreach (var setInfo in _setsExistentes.OrderBy(s => s.Nome))
            {
                if (!string.IsNullOrEmpty(filtro) &&
                    !setInfo.Nome.ToLower().Contains(filtro))
                    continue;

                var item = new ListViewItem(setInfo.Nome);
                item.SubItems.Add(setInfo.ItensSelecionados.ToString());
                item.Tag = setInfo.Nome;
                item.ToolTipText = $"{setInfo.ItensSelecionados} selected item(s) will receive this set name.";
                item.Checked = _setsSelecionados.Contains(setInfo.Nome);

                lvCategorias.Items.Add(item);
            }

            // Auto-resize
            if (lvCategorias.Columns.Count >= 2)
            {
                lvCategorias.Columns[0].Width = -2;
                lvCategorias.Columns[1].Width = 56;
            }

            _atualizandoListaSets = false;
            AtualizarResumoSetsUI();
        }

        private void FiltrarSets()
        {
            if (txtPesquisa.ForeColor == CorTextoClaro) return;
            PreencherSets();
        }

        private void LvCategorias_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_atualizandoListaSets || !(e.Item.Tag is string nomeSet)) return;

            if (e.Item.Checked)
                _setsSelecionados.Add(nomeSet);
            else
                _setsSelecionados.Remove(nomeSet);

            AtualizarResumoSetsUI();
        }

        private void DefinirSelecaoSets(bool selecionarTodos)
        {
            if (selecionarTodos)
            {
                _setsSelecionados.Clear();
                foreach (var setInfo in _setsExistentes)
                {
                    if (!string.IsNullOrWhiteSpace(setInfo?.Nome))
                        _setsSelecionados.Add(setInfo.Nome);
                }
            }
            else
            {
                _setsSelecionados.Clear();
            }

            PreencherSets();
        }

        // ────────────────────────────────────────────────────────────────
        //  PAINEL DIREITO — Edição de atributos
        // ────────────────────────────────────────────────────────────────

        private void MontarPainelDireito(Control parent)
        {
            pnlDireito = CriarPainelCard();
            pnlDireito.Dock = DockStyle.Fill;
            parent.Padding = new Padding(4, 0, 0, 0);
            parent.Controls.Add(pnlDireito);

            // Título
            var lblTitulo = new Label
            {
                Text = "Custom Attributes",
                Font = FonteTitulo,
                ForeColor = CorTexto,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 2, 0, 0)
            };
            pnlDireito.Controls.Add(lblTitulo);

            // ── Linha da categoria ─────────────────────────────────────
            var pnlCat = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(0, 2, 0, 2)
            };

            var lblCat = new Label
            {
                Text = "Category:",
                Font = FonteSemibold,
                ForeColor = CorTexto,
                AutoSize = true,
                Location = new Point(0, 7)
            };
            pnlCat.Controls.Add(lblCat);

            int catLabelW = TextRenderer.MeasureText("Category:", FonteSemibold).Width + 4;

            txtCategoria = new TextBox
            {
                Text = CATEGORIA_FIXA,
                Font = FonteNormal,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                BackColor = CorGridAlt,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(catLabelW, 3),
                Size = new Size(pnlCat.Width - catLabelW - 2, 24)
            };
            pnlCat.Controls.Add(txtCategoria);
            pnlCat.Resize += (s, e) => txtCategoria.Width = pnlCat.Width - catLabelW - 2;

            pnlDireito.Controls.Add(pnlCat);
            pnlCat.BringToFront();

            var pnlAjuda = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(242, 247, 252),
                Padding = new Padding(12, 8, 12, 8)
            };
            pnlAjuda.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, pnlAjuda.Width - 1, pnlAjuda.Height - 1);
                using (var pen = new Pen(Color.FromArgb(220, 230, 240)))
                    e.Graphics.DrawRectangle(pen, rect);
            };

            var lblAjuda = new Label
            {
                Dock = DockStyle.Fill,
                Font = FontePequena,
                ForeColor = Color.FromArgb(86, 96, 106),
                Text = $"Checked set names are saved in the {AutisSchema.PropriedadeSets} field under {CATEGORIA_FIXA}. Use the table below only for extra custom attributes."
            };
            pnlAjuda.Controls.Add(lblAjuda);
            pnlDireito.Controls.Add(pnlAjuda);
            pnlAjuda.BringToFront();

            // ── Botões acima do grid ───────────────────────────────────
            var pnlBtns = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(0, 2, 0, 4)
            };

            var btnAdd = CriarBotaoPequeno("+ Add Row", CorAccent);
            btnAdd.Size = new Size(104, 28);
            btnAdd.Location = new Point(0, 3);
            btnAdd.Click += (s, e) => AdicionarLinhaGrid("", "", "string");
            pnlBtns.Controls.Add(btnAdd);

            var btnRemove = CriarBotaoPequeno("- Remove", CorPerigo);
            btnRemove.Size = new Size(92, 28);
            btnRemove.Location = new Point(110, 3);
            btnRemove.Click += (s, e) =>
            {
                if (grid.SelectedRows.Count > 0 && grid.Rows.Count > 0)
                    grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
            };
            pnlBtns.Controls.Add(btnRemove);

            var btnLimpar = CriarBotaoPequeno("Clear Grid", CorTextoClaro);
            btnLimpar.Size = new Size(82, 28);
            btnLimpar.Location = new Point(208, 3);
            btnLimpar.Click += (s, e) => grid.Rows.Clear();
            pnlBtns.Controls.Add(btnLimpar);

            var corExport = Color.FromArgb(108, 117, 125);
            var btnExportar = CriarBotaoPequeno("Export", corExport);
            btnExportar.Size = new Size(80, 28);
            btnExportar.Location = new Point(296, 3);
            btnExportar.Click += BtnExportar_Click;
            pnlBtns.Controls.Add(btnExportar);

            var btnImportar = CriarBotaoPequeno("Import", corExport);
            btnImportar.Size = new Size(80, 28);
            btnImportar.Location = new Point(382, 3);
            btnImportar.Click += BtnImportar_Click;
            pnlBtns.Controls.Add(btnImportar);

            pnlDireito.Controls.Add(pnlBtns);
            pnlBtns.BringToFront();

            // ── Grid ───────────────────────────────────────────────────
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                Font = FonteGrid,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = CorPainel,
                GridColor = CorBorda,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 32 },
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };

            // Estilo do header
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = CorGridHeader,
                ForeColor = CorTexto,
                Font = FonteSemibold,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = CorGridHeader,
                SelectionForeColor = CorTexto
            };

            // Estilo das células
            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = CorPainel,
                ForeColor = CorTexto,
                SelectionBackColor = Color.FromArgb(230, 240, 255),
                SelectionForeColor = CorTexto,
                Padding = new Padding(8, 0, 0, 0)
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = CorGridAlt,
                ForeColor = CorTexto,
                SelectionBackColor = Color.FromArgb(230, 240, 255),
                SelectionForeColor = CorTexto,
                Padding = new Padding(8, 0, 0, 0)
            };

            // Colunas
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Nome",
                HeaderText = "Name",
                FillWeight = 35
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Valor",
                HeaderText = "Value",
                FillWeight = 40
            });

            var colTipo = new DataGridViewComboBoxColumn
            {
                Name = "Tipo",
                HeaderText = "Type",
                FillWeight = 25,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            colTipo.Items.AddRange("string", "double", "int", "bool");
            grid.Columns.Add(colTipo);

            pnlDireito.Controls.Add(grid);
            grid.BringToFront();

            // Primeira linha inicia vazia para evitar preenchimento implícito
            AdicionarLinhaGrid("", "", "string");
        }

        // ────────────────────────────────────────────────────────────────
        //  HELPERS
        // ────────────────────────────────────────────────────────────────

        private void AdicionarLinhaGrid(string nome, string valor, string tipo)
        {
            int idx = grid.Rows.Add();
            grid.Rows[idx].Cells["Nome"].Value = nome;
            grid.Rows[idx].Cells["Valor"].Value = valor;
            grid.Rows[idx].Cells["Tipo"].Value = tipo;
        }

        private void AplicarCategoriaFixa()
        {
            if (txtCategoria != null)
                txtCategoria.Text = CATEGORIA_FIXA;
        }

        private string ObterTextoResumoSets()
        {
            if (_setsExistentes.Count == 0)
                return "No sets were detected for the current selection. You can still save extra custom attributes below.";

            return $"{_setsSelecionados.Count} of {_setsExistentes.Count} detected set name(s) selected for the fixed {AutisSchema.PropriedadeSets} field.";
        }

        private string ObterTextoSubHeader()
        {
            int totalSetsDetectados = _setsExistentes.Count;
            int totalSetsSelecionados = _setsSelecionados.Count;

            if (totalSetsDetectados > 0 && !string.IsNullOrWhiteSpace(_nomeSetDetectado))
                return $"{_qtdSelecionados} element(s) — {totalSetsSelecionados}/{totalSetsDetectados} set name(s) selected — First: {_nomeSetDetectado}";
            if (totalSetsDetectados > 0)
                return $"{_qtdSelecionados} element(s) — {totalSetsSelecionados}/{totalSetsDetectados} set name(s) selected";

            return $"{_qtdSelecionados} element(s) selected";
        }

        private void AtualizarResumoSetsUI()
        {
            if (lblSubHeader != null)
                lblSubHeader.Text = ObterTextoSubHeader();

            if (lblResumoSets != null)
                lblResumoSets.Text = ObterTextoResumoSets();

            if (lblSelecaoSets != null)
                lblSelecaoSets.Text = $"{_setsSelecionados.Count} selected";
        }

        private Panel CriarPainelCard()
        {
            var pnl = new Panel
            {
                BackColor = CorPainel,
                Padding = new Padding(14)
            };
            pnl.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
                using (var pen = new Pen(CorBorda))
                using (var path = CriarRoundedRect(rect, 6))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.DrawPath(pen, path);
                }
            };
            return pnl;
        }

        private Button CriarBotao(string texto, Color fundo, Color textoColor, Color borda)
        {
            var btn = new Button
            {
                Text = texto,
                Size = new Size(105, 36),
                Font = FonteNormal,
                BackColor = fundo,
                ForeColor = textoColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = borda;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = fundo == CorAccent
                ? CorAccentHover
                : fundo == CorPerigo
                    ? Color.FromArgb(190, 40, 55)
                    : Color.FromArgb(240, 240, 245);
            return btn;
        }

        private Button CriarBotaoPequeno(string texto, Color cor)
        {
            var btn = new Button
            {
                Text = texto,
                Size = new Size(92, 26),
                Font = FontePequena,
                ForeColor = cor,
                BackColor = CorPainel,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = cor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 250);
            return btn;
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

        // ────────────────────────────────────────────────────────────────
        //  EXPORTAR / IMPORTAR
        // ────────────────────────────────────────────────────────────────

        private void BtnExportar_Click(object sender, EventArgs e)
        {
            if (grid.Rows.Count == 0)
            {
                MessageBox.Show("No attributes to export.",
                    "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|XML (*.xml)|*.xml",
                Title = "Export Attributes",
                DefaultExt = "csv"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                if (dlg.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                    ExportarXml(dlg.FileName);
                else
                    ExportarCsv(dlg.FileName);

                MessageBox.Show("Exported successfully!",
                    "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnImportar_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|XML (*.xml)|*.xml",
                Title = "Import Attributes"
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    if (dlg.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        ImportarXml(dlg.FileName);
                    else
                        ImportarCsv(dlg.FileName);

                    MessageBox.Show($"Imported {grid.Rows.Count} attribute(s).",
                        "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao importar: {ex.Message}",
                        "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportarCsv(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Categoria,Nome,Valor,Tipo");

            var cat = CATEGORIA_FIXA;
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var nome = Escapar(grid.Rows[i].Cells["Nome"].Value?.ToString() ?? "");
                var valor = Escapar(grid.Rows[i].Cells["Valor"].Value?.ToString() ?? "");
                var tipo = grid.Rows[i].Cells["Tipo"].Value?.ToString() ?? "string";
                sb.AppendLine($"{Escapar(cat)},{nome},{valor},{tipo}");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void ExportarXml(string path)
        {
            var ds = new DataSet("AutisAtributos");
            var dt = new DataTable("Atributo");
            dt.Columns.Add("Categoria", typeof(string));
            dt.Columns.Add("Nome", typeof(string));
            dt.Columns.Add("Valor", typeof(string));
            dt.Columns.Add("Tipo", typeof(string));

            var cat = CATEGORIA_FIXA;
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var row = dt.NewRow();
                row["Categoria"] = cat;
                row["Nome"] = grid.Rows[i].Cells["Nome"].Value?.ToString() ?? "";
                row["Valor"] = grid.Rows[i].Cells["Valor"].Value?.ToString() ?? "";
                row["Tipo"] = grid.Rows[i].Cells["Tipo"].Value?.ToString() ?? "string";
                dt.Rows.Add(row);
            }

            ds.Tables.Add(dt);
            var settings = new XmlWriterSettings { Indent = true };
            using (var writer = XmlWriter.Create(path, settings))
                ds.WriteXml(writer);
        }

        private void ImportarCsv(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length < 2) return;

            grid.Rows.Clear();
            // Pular header
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = ParseCsvLine(lines[i]);
                if (parts.Count < 4) continue;

                AplicarCategoriaFixa();
                AdicionarLinhaGrid(parts[1], parts[2], parts[3]);
            }
        }

        private void ImportarXml(string path)
        {
            var ds = new DataSet();
            ds.ReadXml(path);
            var dt = ds.Tables[0];

            grid.Rows.Clear();
            foreach (DataRow row in dt.Rows)
            {
                var nome = row.Table.Columns.Contains("Nome") ? row["Nome"].ToString() : "";
                var valor = row.Table.Columns.Contains("Valor") ? row["Valor"].ToString() : "";
                var tipo = row.Table.Columns.Contains("Tipo") ? row["Tipo"].ToString() : "string";

                AplicarCategoriaFixa();
                AdicionarLinhaGrid(nome, valor, tipo);
            }
        }

        private static string Escapar(string val)
        {
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n"))
                return "\"" + val.Replace("\"", "\"\"") + "\"";
            return val;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    { current.Append('"'); i++; }
                    else if (c == '"')
                        inQuotes = false;
                    else
                        current.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { result.Add(current.ToString()); current.Clear(); }
                    else current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        // ────────────────────────────────────────────────────────────────
        //  GRAVAR
        // ────────────────────────────────────────────────────────────────

        private void BtnGravar_Click(object sender, EventArgs e)
        {
            NomeCategoria = CATEGORIA_FIXA;
            AplicarCategoriaFixa();
            SolicitarExclusao = false;
            NomesSetsSelecionados = _setsSelecionados
                .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Atributos.Clear();

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var nome = grid.Rows[i].Cells["Nome"].Value?.ToString()?.Trim();
                var valor = grid.Rows[i].Cells["Valor"].Value?.ToString()?.Trim() ?? "";
                var tipo = grid.Rows[i].Cells["Tipo"].Value?.ToString() ?? "string";

                if (string.IsNullOrEmpty(nome)) continue;

                Atributos.Add(new AtributoCustom(NomeCategoria, nome, valor, tipo));
            }

            if (Atributos.Count == 0 && NomesSetsSelecionados.Count == 0)
            {
                MessageBox.Show("Add at least one attribute or select at least one set to export.",
                    "Autis Analytics", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnExcluir_Click(object sender, EventArgs e)
        {
            var resposta = MessageBox.Show(
                $"Delete all attributes created by Autis from the selected elements?\n\nThis removes the entire {CATEGORIA_FIXA} category and legacy Autis categories.",
                "Autis Analytics",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (resposta != DialogResult.Yes)
                return;

            NomeCategoria = CATEGORIA_FIXA;
            AplicarCategoriaFixa();
            SolicitarExclusao = true;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
