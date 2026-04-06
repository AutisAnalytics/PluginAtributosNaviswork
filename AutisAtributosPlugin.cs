using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Windows;

namespace AutisAnalytics.NavisworksAtributos
{
    /// <summary>
    /// EventWatcherPlugin que cria a aba "Autis" na Ribbon do Navisworks 2026.
    /// Usa a API programática Autodesk.Windows (AdWindows.dll) — mesma abordagem
    /// do plugin waabe_navi_mcp.
    /// </summary>
    [Plugin("AutisAnalytics.Atributos", "AUAN",
        DisplayName = "Autis Analytics - Properties",
        ToolTip = "Custom attribute tools")]
    public class AutisAtributosPlugin : EventWatcherPlugin
    {
        private bool _ribbonCreated;

        public override void OnLoaded()
        {
            // Timer para esperar a Ribbon estar disponível
            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (s, e) =>
            {
                if (ComponentManager.Ribbon != null)
                {
                    timer.Stop();
                    timer.Dispose();

                    CriarRibbon();
                }
            };
            timer.Start();
        }

        public override void OnUnloading()
        {
        }

        private void CriarRibbon()
        {
            if (_ribbonCreated) return;
            _ribbonCreated = true;

            var ribbon = ComponentManager.Ribbon;
            if (ribbon == null) return;

            // ── Criar aba "AWP Autis" ────────────────────────────────────────
            var tab = new RibbonTab
            {
                Title = "AWP Autis",
                Id    = "ID_AUTIS_TAB",
                Name  = "AWP Autis"
            };

            // ── Painel "Properties" ─────────────────────────────────────────
            var panelSource = new RibbonPanelSource
            {
                Title = "Properties",
                Name  = "Properties"
            };

            // Botão Ler
            var btnLer = new RibbonButton
            {
                Text        = "Read\nProperties",
                Id          = "ID_BTN_LER",
                Size        = RibbonItemSize.Large,
                ShowText    = true,
                ShowImage   = true,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                LargeImage  = CriarIcone("L", System.Drawing.Color.FromArgb(0, 120, 215)),
                Image       = CriarIcone16("L", System.Drawing.Color.FromArgb(0, 120, 215))
            };
            btnLer.CommandHandler = new AutisCommandHandler();

            // Botão Gravar
            var btnGravar = new RibbonButton
            {
                Text        = "Write\nProperties",
                Id          = "ID_BTN_GRAVAR",
                Size        = RibbonItemSize.Large,
                ShowText    = true,
                ShowImage   = true,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                LargeImage  = CriarIcone("G", System.Drawing.Color.FromArgb(40, 167, 69)),
                Image       = CriarIcone16("G", System.Drawing.Color.FromArgb(40, 167, 69))
            };
            btnGravar.CommandHandler = new AutisCommandHandler();

            // Botão Selection Inspector
            var btnInspector = new RibbonButton
            {
                Text        = "Selection\nInspector",
                Id          = "ID_BTN_INSPECTOR",
                Size        = RibbonItemSize.Large,
                ShowText    = true,
                ShowImage   = true,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                LargeImage  = CriarIcone("S", System.Drawing.Color.FromArgb(108, 92, 168)),
                Image       = CriarIcone16("S", System.Drawing.Color.FromArgb(108, 92, 168))
            };
            btnInspector.CommandHandler = new AutisCommandHandler();

            panelSource.Items.Add(btnLer);
            panelSource.Items.Add(btnGravar);
            panelSource.Items.Add(btnInspector);

            var panel = new RibbonPanel { Source = panelSource };
            tab.Panels.Add(panel);

            // ── Painel "Visualization" ────────────────────────────────────
            var panelVisSource = new RibbonPanelSource
            {
                Title = "Visualization",
                Name  = "Visualization"
            };

            var btnColorizer = new RibbonButton
            {
                Text        = "Colorizer",
                Id          = "ID_BTN_COLORIZER",
                Size        = RibbonItemSize.Large,
                ShowText    = true,
                ShowImage   = true,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                LargeImage  = CriarIcone("C", System.Drawing.Color.FromArgb(220, 53, 69)),
                Image       = CriarIcone16("C", System.Drawing.Color.FromArgb(220, 53, 69))
            };
            btnColorizer.CommandHandler = new AutisCommandHandler();
            panelVisSource.Items.Add(btnColorizer);

            var panelVis = new RibbonPanel { Source = panelVisSource };
            tab.Panels.Add(panelVis);

            ribbon.Tabs.Add(tab);
            tab.IsActive = false;
        }

        // ── Ícones gerados programaticamente (32x32 e 16x16) ────────
        private static BitmapImage CriarIcone(string letra, System.Drawing.Color cor)
        {
            return GerarIconeBitmap(32, letra, cor);
        }

        private static BitmapImage CriarIcone16(string letra, System.Drawing.Color cor)
        {
            return GerarIconeBitmap(16, letra, cor);
        }

        private static BitmapImage GerarIconeBitmap(int size, string letra, System.Drawing.Color cor)
        {
            using (var bmp = new Bitmap(size, size))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                // Fundo arredondado
                using (var brush = new SolidBrush(cor))
                {
                    int r = size / 5;
                    var rect = new Rectangle(0, 0, size - 1, size - 1);
                    using (var path = new GraphicsPath())
                    {
                        int d = r * 2;
                        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                        path.CloseFigure();
                        g.FillPath(brush, path);
                    }
                }

                // Letra centralizada
                float fontSize = size * 0.55f;
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(letra, font, Brushes.White,
                        new RectangleF(0, 0, size, size), sf);
                }

                // Converter para BitmapImage (WPF)
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
        }
    }

    /// <summary>
    /// Handler para os botões da Ribbon. Recebe o clique e despacha
    /// pela propriedade Id do RibbonButton.
    /// </summary>
    internal class AutisCommandHandler : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged { add { } remove { } }

        /// <summary>
        /// Disparado após gravar atributos com sucesso. Usado pelo Selection Inspector (Integrado).
        /// </summary>
        internal static event EventHandler AtributosGravados;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var btn = parameter as RibbonCommandItem;
            if (btn == null) return;

            switch (btn.Id)
            {
                case "ID_BTN_LER":
                    ExecutarLeitura();
                    break;
                case "ID_BTN_GRAVAR":
                    ExecutarGravacao();
                    break;
                case "ID_BTN_COLORIZER":
                    ExecutarColorizer();
                    break;
                case "ID_BTN_INSPECTOR":
                    ExecutarInspector();
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Ler Atributos
        // ─────────────────────────────────────────────────────────────────

        private void ExecutarLeitura()
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) return;

            var selecao = doc.CurrentSelection.SelectedItems;
            if (selecao.Count == 0)
            {
                MessageBox.Show("Select at least one element in the model.",
                    "Autis Analytics", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var primeiroItem = selecao[0];
            var propriedades = AtributoService.LerPropriedades(primeiroItem);

            using (var form = new LerAtributosForm(
                primeiroItem.DisplayName, selecao.Count, propriedades))
            {
                form.ShowDialog();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Selection Inspector
        // ─────────────────────────────────────────────────────────────────

        private void ExecutarInspector()
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) return;

            var selecao = doc.CurrentSelection.SelectedItems;
            if (selecao.Count == 0)
            {
                MessageBox.Show("Select at least one element in the model.",
                    "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new SelectionInspectorForm(selecao))
            {
                form.ShowDialog();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Colorizer
        // ─────────────────────────────────────────────────────────────────

        private void ExecutarColorizer()
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null || doc.IsClear)
            {
                MessageBox.Show("No document open.",
                    "Autis Analytics", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new ColorizerForm())
            {
                form.ShowDialog();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Gravar Atributos
        // ─────────────────────────────────────────────────────────────────

        private void ExecutarGravacao()
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) return;

            var selecao = doc.CurrentSelection.SelectedItems;
            if (selecao.Count == 0)
            {
                MessageBox.Show("Select at least one element in the model.",
                    "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var setsPorItem = MapearTodosSetsPorItem(doc, selecao);
            var setsDetectados = CriarResumoSets(setsPorItem);
            var setsSalvos = AtributoService.LerSetsSalvos(
                selecao[0],
                setsDetectados.Select(setInfo => setInfo.Nome),
                AutisSchema.CategoriaPrincipal);
            string nomeSetDetectado = setsPorItem.TryGetValue(selecao[0], out var setsPrimeiroItem) &&
                                      setsPrimeiroItem.Count > 0
                ? setsPrimeiroItem[0].Nome
                : null;

            using (var form = new GravarAtributosForm(
                selecao.Count,
                setsDetectados,
                nomeSetDetectado,
                setsSalvos))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                var colecao = new ModelItemCollection();
                foreach (var item in selecao)
                    colecao.Add(item);

                var setsSelecionadosPorItem = FiltrarSetsSelecionados(setsPorItem, form.NomesSetsSelecionados);

                var resultado = form.SolicitarExclusao
                    ? AtributoService.ExcluirAtributos(colecao, form.NomeCategoria)
                    : AtributoService.GravarAtributos(colecao, form.Atributos, form.NomeCategoria, setsSelecionadosPorItem);

                MessageBox.Show(resultado.mensagem,
                    form.SolicitarExclusao ? "AWP Autis - Delete" : "AWP Autis - Save",
                    MessageBoxButtons.OK,
                    resultado.erros > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                if (resultado.erros == 0)
                    AtributosGravados?.Invoke(null, EventArgs.Empty);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Mapear todos os Sets que contêm cada item da seleção
        // ─────────────────────────────────────────────────────────────────

        private Dictionary<ModelItem, List<SetAssignment>> MapearTodosSetsPorItem(
            Document doc, IEnumerable<ModelItem> selecao)
        {
            var mapa = new Dictionary<ModelItem, List<SetAssignment>>();

            try
            {
                foreach (var item in selecao)
                    mapa[item] = new List<SetAssignment>();

                foreach (var setInfo in SelectionSetCache.Collect(doc))
                {
                    foreach (ModelItem itemDoSet in setInfo.Itens)
                    {
                        if (mapa.TryGetValue(itemDoSet, out var setsDoItem))
                            setsDoItem.Add(new SetAssignment(setInfo.NomeGravacao));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Autis] Erro ao mapear sets da selecao: {ex.Message}");
            }
            return mapa;
        }

        private List<GravarAtributosForm.SetResumo> CriarResumoSets(
            Dictionary<ModelItem, List<SetAssignment>> setsPorItem)
        {
            var itensPorNome = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var setsDoItem in setsPorItem.Values)
            {
                foreach (var nome in (setsDoItem ?? new List<SetAssignment>())
                    .Where(setInfo => !string.IsNullOrWhiteSpace(setInfo?.Nome))
                    .Select(setInfo => setInfo.Nome.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (itensPorNome.TryGetValue(nome, out var quantidade))
                        itensPorNome[nome] = quantidade + 1;
                    else
                        itensPorNome[nome] = 1;
                }
            }

            return itensPorNome
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new GravarAtributosForm.SetResumo(kvp.Key, kvp.Value))
                .ToList();
        }

        private Dictionary<ModelItem, List<SetAssignment>> FiltrarSetsSelecionados(
            Dictionary<ModelItem, List<SetAssignment>> setsPorItem,
            IEnumerable<string> nomesSelecionados)
        {
            if (setsPorItem == null)
                return null;

            var nomes = new HashSet<string>(
                (nomesSelecionados ?? Enumerable.Empty<string>())
                    .Where(nome => !string.IsNullOrWhiteSpace(nome))
                    .Select(nome => nome.Trim()),
                StringComparer.OrdinalIgnoreCase);

            var resultado = new Dictionary<ModelItem, List<SetAssignment>>();
            foreach (var kvp in setsPorItem)
            {
                resultado[kvp.Key] = (kvp.Value ?? new List<SetAssignment>())
                    .Where(setInfo => !string.IsNullOrWhiteSpace(setInfo?.Nome) && nomes.Contains(setInfo.Nome.Trim()))
                    .GroupBy(setInfo => setInfo.Nome.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }

            return resultado;
        }
    }
}
