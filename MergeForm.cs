using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Navisworks.Api;
using Color = System.Drawing.Color;
using NwColor = Autodesk.Navisworks.Api.Color;

namespace AutisAnalytics.NavisworksAtributos
{
    public class MergeForm : Form
    {
        // ── Config phase controls ──────────────────────────────────────
        private Panel pnlConfig;
        private Label lblOldModel;
        private Label lblNewModel;
        private CheckBox chkAttributes;
        private CheckBox chkSets;
        private RadioButton radioAuto;
        private RadioButton radioDeep;
        private RadioButton radioUltra;
        private ComboBox cmbPreferredId;
        private Label lblStatus;
        private Button btnAnalyze;
        private ProgressBar progressBar;
        private Label lblPercent;
        private Label lblProgressDetail;

        // ── Results phase controls ─────────────────────────────────────
        private Panel pnlResults;
        private Label lblSummaryMatched;
        private Label lblSummaryNew;
        private Label lblSummaryRemoved;
        private Label lblSummaryCandidates;
        private Label lblSummaryTransfer;
        private TabControl tabResults;
        private DataGridView gridMatched;
        private DataGridView gridNew;
        private DataGridView gridRemoved;
        private DataGridView gridCandidates;
        private Button btnExecuteMerge;
        private Button btnExportCsv;

        // ── Data ───────────────────────────────────────────────────────
        private readonly string _newNwdPath;
        private MergeReport _report;
        private bool _colorsApplied;
        private List<ModelItem> _oldRoots;
        private HashSet<Guid> _oldModelGuids = new HashSet<Guid>();
        private HashSet<Guid> _appendedModelGuids = new HashSet<Guid>();
        private bool _mergeExecuted;

        public MergeForm(string newNwdPath)
        {
            _newNwdPath = newNwdPath;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            Montar();
        }

        // ════════════════════════════════════════════════════════════════
        // BUILD UI
        // ════════════════════════════════════════════════════════════════

        private void Montar()
        {
            Text = "Autis Analytics";
            Size = new Size(950, 700);
            MinimumSize = new Size(800, 550);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UITheme.Color.Background;
            Font = UITheme.Typography.Body;

            // ── Header ─────────────────────────────────────────────────
            var pnlHeader = UITheme.CreateHeader("Merge NWD",
                "Transfer attributes between model revisions");
            Controls.Add(pnlHeader);

            // ── Footer ─────────────────────────────────────────────────
            var pnlFooter = UITheme.CreateFooter();

            lblStatus = new Label
            {
                Text = "",
                Font = UITheme.Typography.BodySmall,
                ForeColor = UITheme.Color.TextTertiary,
                AutoSize = true,
                Location = new Point(UITheme.Spacing.LG, 16)
            };
            pnlFooter.Controls.Add(lblStatus);

            var btnClose = UITheme.CreateSecondaryButton("Close");
            btnClose.Width = 100;
            btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnClose.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnClose);

            btnExportCsv = UITheme.CreateSecondaryButton("Export CSV");
            btnExportCsv.Width = 110;
            btnExportCsv.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnExportCsv.Visible = false;
            btnExportCsv.Click += BtnExportCsv_Click;
            pnlFooter.Controls.Add(btnExportCsv);

            btnExecuteMerge = UITheme.CreateSuccessButton("Execute Merge");
            btnExecuteMerge.Width = 130;
            btnExecuteMerge.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            btnExecuteMerge.Visible = false;
            btnExecuteMerge.Click += BtnExecuteMerge_Click;
            pnlFooter.Controls.Add(btnExecuteMerge);

            pnlFooter.Resize += (s, e) =>
            {
                btnClose.Location = new Point(pnlFooter.Width - 116, 8);
                btnExportCsv.Location = new Point(pnlFooter.Width - 242, 8);
                btnExecuteMerge.Location = new Point(pnlFooter.Width - 388, 8);
            };

            Controls.Add(pnlFooter);
            CancelButton = btnClose;

            // ── Body ───────────────────────────────────────────────────
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(UITheme.Spacing.LG, UITheme.Spacing.MD,
                                      UITheme.Spacing.LG, UITheme.Spacing.MD)
            };
            Controls.Add(pnlBody);
            pnlBody.BringToFront();

            MontarConfigPanel(pnlBody);
            MontarResultsPanel(pnlBody);

            // Start in config phase
            pnlConfig.Visible = true;
            pnlResults.Visible = false;
        }

        // ────────────────────────────────────────────────────────────────
        // CONFIG PANEL (Phase 1)
        // ────────────────────────────────────────────────────────────────

        private void MontarConfigPanel(Panel parent)
        {
            pnlConfig = new Panel { Dock = DockStyle.Fill };
            parent.Controls.Add(pnlConfig);

            var card = UITheme.CreateCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(UITheme.Spacing.XL);
            pnlConfig.Controls.Add(card);

            int y = UITheme.Spacing.XL;

            // ── Models info ────────────────────────────────────────────
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            var currentFile = doc?.FileName ?? doc?.Title ?? "(unknown)";

            var lblTitleModels = new Label
            {
                Text = "MODELS",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true
            };
            card.Controls.Add(lblTitleModels);
            y += 22;

            // Current model
            var lblOldTitle = new Label
            {
                Text = "Current Model:",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextPrimary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true
            };
            card.Controls.Add(lblOldTitle);

            lblOldModel = new Label
            {
                Text = Path.GetFileName(currentFile),
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(140, y),
                AutoSize = true
            };
            card.Controls.Add(lblOldModel);
            y += 24;

            // New model
            var lblNewTitle = new Label
            {
                Text = "New Model:",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextPrimary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true
            };
            card.Controls.Add(lblNewTitle);

            lblNewModel = new Label
            {
                Text = Path.GetFileName(_newNwdPath),
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.Success,
                Location = new Point(140, y),
                AutoSize = true
            };
            card.Controls.Add(lblNewModel);
            y += 36;

            // ── Divider ────────────────────────────────────────────────
            var div1 = UITheme.CreateDivider();
            div1.Location = new Point(UITheme.Spacing.XL, y);
            div1.Width = card.Width - UITheme.Spacing.XL * 2;
            div1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(div1);
            y += 16;

            // ── What to transfer ───────────────────────────────────────
            var lblTitleTransfer = new Label
            {
                Text = "WHAT TO TRANSFER",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true
            };
            card.Controls.Add(lblTitleTransfer);
            y += 22;

            chkAttributes = new CheckBox
            {
                Text = "Custom Attributes (Autis_Attributes)",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextPrimary,
                Checked = true,
                Location = new Point(UITheme.Spacing.XL + 4, y),
                AutoSize = true
            };
            card.Controls.Add(chkAttributes);
            y += 26;

            chkSets = new CheckBox
            {
                Text = "Set Associations (Autis_AWP)",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextPrimary,
                Checked = true,
                Location = new Point(UITheme.Spacing.XL + 4, y),
                AutoSize = true
            };
            card.Controls.Add(chkSets);
            y += 36;

            // ── Divider ────────────────────────────────────────────────
            var div2 = UITheme.CreateDivider();
            div2.Location = new Point(UITheme.Spacing.XL, y);
            div2.Width = card.Width - UITheme.Spacing.XL * 2;
            div2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(div2);
            y += 16;

            // ── Matching depth ─────────────────────────────────────────
            var lblTitleDepth = new Label
            {
                Text = "MATCHING DEPTH",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true
            };
            card.Controls.Add(lblTitleDepth);
            y += 22;

            radioAuto = new RadioButton
            {
                Text = "Automatic (Levels 1-3, fast)",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextPrimary,
                Checked = true,
                Location = new Point(UITheme.Spacing.XL + 4, y),
                AutoSize = true
            };
            card.Controls.Add(radioAuto);
            y += 24;

            radioDeep = new RadioButton
            {
                Text = "Deep (Levels 1-4, slower)  — coming soon",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextDisabled,
                Enabled = false,
                Location = new Point(UITheme.Spacing.XL + 4, y),
                AutoSize = true
            };
            card.Controls.Add(radioDeep);
            y += 24;

            radioUltra = new RadioButton
            {
                Text = "Ultra (Levels 1-5 with AI)  — coming soon",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextDisabled,
                Enabled = false,
                Location = new Point(UITheme.Spacing.XL + 4, y),
                AutoSize = true
            };
            card.Controls.Add(radioUltra);
            y += 36;

            // ── Divider ────────────────────────────────────────────────
            var div3 = UITheme.CreateDivider();
            div3.Location = new Point(UITheme.Spacing.XL, y);
            div3.Width = card.Width - UITheme.Spacing.XL * 2;
            div3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.Controls.Add(div3);
            y += 16;

            // ── Preferred ID ───────────────────────────────────────────
            var lblTitleId = new Label
            {
                Text = "PREFERRED ID PROPERTY",
                Font = UITheme.Typography.Label,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true
            };
            card.Controls.Add(lblTitleId);
            y += 22;

            cmbPreferredId = new ComboBox
            {
                Font = UITheme.Typography.Body,
                Location = new Point(UITheme.Spacing.XL + 4, y),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbPreferredId.Items.Add("(auto-detect)");
            cmbPreferredId.Items.AddRange(ElementFingerprint.IdFieldNames.Cast<object>().ToArray());
            cmbPreferredId.SelectedIndex = 0;
            card.Controls.Add(cmbPreferredId);

            var lblIdHint = new Label
            {
                Text = "Detected after analysis starts",
                Font = UITheme.Typography.BodySmall,
                ForeColor = UITheme.Color.TextTertiary,
                Location = new Point(280, y + 3),
                AutoSize = true
            };
            card.Controls.Add(lblIdHint);
            y += 40;

            // ── Analyze button ─────────────────────────────────────────
            btnAnalyze = UITheme.CreatePrimaryButton("Analyze Models");
            btnAnalyze.Width = 160;
            btnAnalyze.Location = new Point(UITheme.Spacing.XL, y);
            btnAnalyze.Click += BtnAnalyze_Click;
            card.Controls.Add(btnAnalyze);
            y += 50;

            // ── Progress section (hidden until analysis starts) ──────────
            // Percentage — big and bold, first line
            lblPercent = new Label
            {
                Text = "0%",
                Font = UITheme.Typography.H1,
                ForeColor = UITheme.Color.Primary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true,
                Visible = false
            };
            card.Controls.Add(lblPercent);
            y += 34;

            // Progress bar — full width below the percentage
            progressBar = new ProgressBar
            {
                Location = new Point(UITheme.Spacing.XL, y),
                Height = 18,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            card.Controls.Add(progressBar);
            // Set width after adding to card so anchor works from card width
            progressBar.Width = card.Width > 0 ? card.Width - UITheme.Spacing.XL * 2 : 600;
            y += 28;

            // Detail text below bar
            lblProgressDetail = new Label
            {
                Text = "",
                Font = UITheme.Typography.Body,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(UITheme.Spacing.XL, y),
                AutoSize = true,
                Visible = false
            };
            card.Controls.Add(lblProgressDetail);
        }

        // ────────────────────────────────────────────────────────────────
        // RESULTS PANEL (Phase 2)
        // ────────────────────────────────────────────────────────────────

        private void MontarResultsPanel(Panel parent)
        {
            pnlResults = new Panel { Dock = DockStyle.Fill };
            parent.Controls.Add(pnlResults);

            // ── Summary bar ────────────────────────────────────────────
            var pnlSummary = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = UITheme.Color.Surface,
                Padding = new Padding(UITheme.Spacing.LG)
            };
            pnlSummary.Paint += (s, e) =>
            {
                using (var pen = new Pen(UITheme.Color.BorderLight))
                    e.Graphics.DrawLine(pen, 0, pnlSummary.Height - 1,
                                              pnlSummary.Width, pnlSummary.Height - 1);
            };

            // Colored badges
            int bx = UITheme.Spacing.LG;
            lblSummaryMatched = CriarBadgeSummary(pnlSummary, ref bx,
                Color.FromArgb(66, 133, 244), "0 Matched");
            lblSummaryNew = CriarBadgeSummary(pnlSummary, ref bx,
                Color.FromArgb(46, 184, 70), "0 New");
            lblSummaryRemoved = CriarBadgeSummary(pnlSummary, ref bx,
                Color.FromArgb(211, 47, 47), "0 Removed");
            lblSummaryCandidates = CriarBadgeSummary(pnlSummary, ref bx,
                Color.FromArgb(245, 166, 35), "0 Candidates");

            lblSummaryTransfer = new Label
            {
                Font = UITheme.Typography.BodySmall,
                ForeColor = UITheme.Color.TextSecondary,
                Location = new Point(UITheme.Spacing.LG, 48),
                AutoSize = true,
                Text = ""
            };
            pnlSummary.Controls.Add(lblSummaryTransfer);

            pnlResults.Controls.Add(pnlSummary);

            // ── Tabs with grids ────────────────────────────────────────
            tabResults = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = UITheme.Typography.Body
            };

            // Tab Matched
            var tabMatched = new TabPage("Matched");
            gridMatched = CriarGridResultado();
            gridMatched.Columns.Add("OldName", "Old Element");
            gridMatched.Columns.Add("NewName", "New Element");
            gridMatched.Columns.Add("Level", "Level");
            gridMatched.Columns.Add("Score", "Score");
            gridMatched.Columns.Add("Attrs", "Attributes");
            gridMatched.Columns.Add("Detail", "Detail");
            gridMatched.Columns["Level"].Width = 50;
            gridMatched.Columns["Score"].Width = 55;
            gridMatched.Columns["Attrs"].Width = 70;
            tabMatched.Controls.Add(gridMatched);
            tabResults.TabPages.Add(tabMatched);

            // Tab New
            var tabNew = new TabPage("New");
            gridNew = CriarGridResultado();
            gridNew.Columns.Add("Name", "Element");
            gridNew.Columns.Add("Type", "Type");
            gridNew.Columns.Add("Hierarchy", "Hierarchy");
            tabNew.Controls.Add(gridNew);
            tabResults.TabPages.Add(tabNew);

            // Tab Removed
            var tabRemoved = new TabPage("Removed");
            gridRemoved = CriarGridResultado();
            gridRemoved.Columns.Add("Name", "Element");
            gridRemoved.Columns.Add("Type", "Type");
            gridRemoved.Columns.Add("Attrs", "Attributes");
            gridRemoved.Columns.Add("Sets", "Sets");
            gridRemoved.Columns["Attrs"].Width = 70;
            tabRemoved.Controls.Add(gridRemoved);
            tabResults.TabPages.Add(tabRemoved);

            // Tab Candidates
            var tabCandidates = new TabPage("Candidates");
            gridCandidates = CriarGridResultado();
            var colAccept = new DataGridViewCheckBoxColumn
            {
                Name = "Accept",
                HeaderText = "Accept",
                Width = 55
            };
            gridCandidates.Columns.Add(colAccept);
            gridCandidates.Columns.Add("OldName", "Old Element");
            gridCandidates.Columns.Add("NewName", "New Element");
            gridCandidates.Columns.Add("Score", "Score");
            gridCandidates.Columns.Add("Detail", "Justification");
            gridCandidates.Columns["Score"].Width = 55;
            tabCandidates.Controls.Add(gridCandidates);
            tabResults.TabPages.Add(tabCandidates);

            pnlResults.Controls.Add(tabResults);
            tabResults.BringToFront();
        }

        private Label CriarBadgeSummary(Panel parent, ref int x, Color cor, string text)
        {
            // Color dot
            var dot = new Panel
            {
                Size = new Size(12, 12),
                Location = new Point(x, 16),
                BackColor = cor
            };
            dot.Paint += (s, e) =>
            {
                using (var path = CriarRoundedRect(new Rectangle(0, 0, 12, 12), 3))
                using (var brush = new SolidBrush(cor))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                }
            };
            dot.BackColor = Color.Transparent;
            parent.Controls.Add(dot);
            x += 16;

            var lbl = new Label
            {
                Text = text,
                Font = UITheme.Typography.H3,
                ForeColor = UITheme.Color.TextPrimary,
                Location = new Point(x, 12),
                AutoSize = true
            };
            parent.Controls.Add(lbl);
            x += TextRenderer.MeasureText(text, UITheme.Typography.H3).Width + 20;

            return lbl;
        }

        private DataGridView CriarGridResultado()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            UITheme.StyleDataGridView(grid);
            return grid;
        }

        // ════════════════════════════════════════════════════════════════
        // ANALYZE (Phase 1 → Phase 2 transition)
        // ════════════════════════════════════════════════════════════════

        private void BtnAnalyze_Click(object sender, EventArgs e)
        {
            btnAnalyze.Enabled = false;
            btnAnalyze.Text = "Analyzing...";
            ShowProgress(true);

            // Progress allocation:
            //   0-5%   Appending model
            //   5-40%  Extracting old fingerprints
            //  40-75%  Extracting new fingerprints
            //  75-80%  Detecting preferred ID
            //  80-95%  Running matching
            //  95-100% Applying colors

            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                if (doc == null) return;

                // ── Step 1: Capture old roots ──────────────────────────
                UpdateProgress(0, "Preparing...");
                _mergeExecuted = false;
                _oldRoots = new List<ModelItem>();
                _oldModelGuids = new HashSet<Guid>();
                _appendedModelGuids = new HashSet<Guid>();

                foreach (Model model in doc.Models)
                    _oldModelGuids.Add(model.Guid);
                foreach (var root in doc.Models.RootItems)
                    _oldRoots.Add(root);

                // ── Step 2: Append new NWD ─────────────────────────────
                UpdateProgress(2, "Appending new model...");

                if (!doc.TryAppendFile(_newNwdPath))
                {
                    ShowProgress(false);
                    MessageBox.Show("Failed to append the new NWD file.\n\n" +
                        "Make sure the file exists and is a valid NWD.",
                        "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetAnalyzeButton();
                    return;
                }
                UpdateProgress(5, "Model appended.");

                // Identify new model roots
                var newRoots = new List<ModelItem>();
                foreach (Model model in doc.Models)
                {
                    if (_oldModelGuids.Contains(model.Guid))
                        continue;

                    _appendedModelGuids.Add(model.Guid);
                    if (model.RootItem != null)
                        newRoots.Add(model.RootItem);
                }

                if (newRoots.Count == 0)
                {
                    CleanupTemporaryAnalysisState(doc);
                    ShowProgress(false);
                    MessageBox.Show("Could not identify the appended model.",
                        "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ResetAnalyzeButton();
                    return;
                }

                // ── Step 3: Extract OLD fingerprints (5% → 40%) ────────
                var oldFPs = MergeService.ExtractFingerprints(
                    _oldRoots,
                    Path.GetFileName(doc.FileName ?? "Current Model"),
                    readAutisData: true,
                    progress: (text, pct) =>
                        UpdateProgress(5 + (int)(pct * 0.35), text));

                // ── Step 4: Extract NEW fingerprints (40% → 75%) ───────
                var newFPs = MergeService.ExtractFingerprints(
                    newRoots,
                    Path.GetFileName(_newNwdPath),
                    readAutisData: false,
                    progress: (text, pct) =>
                        UpdateProgress(40 + (int)(pct * 0.35), text));

                // ── Step 5: Detect preferred ID (75% → 80%) ───────────
                UpdateProgress(75, "Detecting best ID property...");
                string preferredId = null;
                if (cmbPreferredId.SelectedIndex == 0)
                {
                    preferredId = MergeService.DetectPreferredId(oldFPs);
                    UpdateProgress(80, $"Auto-detected ID: {preferredId ?? "none"}");
                }
                else
                {
                    preferredId = cmbPreferredId.SelectedItem?.ToString();
                    UpdateProgress(80, $"Using ID: {preferredId}");
                }

                // ── Step 6: Run matching (80% → 95%) ──────────────────
                var config = new MergeConfig
                {
                    NewNwdPath = _newNwdPath,
                    TransferAttributes = chkAttributes.Checked,
                    TransferSets = chkSets.Checked,
                    Depth = MergeDepth.Automatic,
                    PreferredIdProperty = preferredId
                };

                _report = MergeService.RunMatching(oldFPs, newFPs, config,
                    (text, pct) => UpdateProgress(80 + (int)(pct * 0.15), text));

                // ── Step 7: Apply colors (95% → 100%) ─────────────────
                UpdateProgress(95, "Applying color coding...");
                ApplyColorCoding(doc);
                UpdateProgress(100, "Complete!");

                // Transition to results
                ShowProgress(false);
                PopulateResults();
                pnlConfig.Visible = false;
                pnlResults.Visible = true;
                pnlResults.BringToFront();
                btnExecuteMerge.Visible = true;
                btnExportCsv.Visible = true;

                UpdateStatus($"Analysis complete in {_report.AnalysisDuration.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                CleanupTemporaryAnalysisState(Autodesk.Navisworks.Api.Application.ActiveDocument);
                ShowProgress(false);
                MessageBox.Show($"Error during analysis:\n\n{ex.Message}",
                    "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetAnalyzeButton();
            }
        }

        private void ResetAnalyzeButton()
        {
            btnAnalyze.Enabled = true;
            btnAnalyze.Text = "Analyze Models";
        }

        private void UpdateStatus(string text)
        {
            lblStatus.Text = text;
            if (lblProgressDetail.Visible)
                lblProgressDetail.Text = text;
            System.Windows.Forms.Application.DoEvents();
        }

        private void UpdateProgress(int percent, string text = null)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            progressBar.Value = percent;
            lblPercent.Text = $"{percent}%";
            if (text != null)
            {
                lblProgressDetail.Text = text;
                lblStatus.Text = text;
            }
            System.Windows.Forms.Application.DoEvents();
        }

        private void ShowProgress(bool visible)
        {
            progressBar.Visible = visible;
            lblPercent.Visible = visible;
            lblProgressDetail.Visible = visible;
            if (visible)
                progressBar.Value = 0;
        }

        // ════════════════════════════════════════════════════════════════
        // POPULATE RESULTS
        // ════════════════════════════════════════════════════════════════

        private void PopulateResults()
        {
            if (_report == null) return;

            // Summary
            lblSummaryMatched.Text = $"{_report.Matched.Count} Matched " +
                $"(L1:{_report.MatchedLevel1} L2:{_report.MatchedLevel2} L3:{_report.MatchedLevel3})";
            lblSummaryNew.Text = $"{_report.NewElements.Count} New";
            lblSummaryRemoved.Text = $"{_report.RemovedElements.Count} Removed";
            lblSummaryCandidates.Text = $"{_report.Candidates.Count} Candidates";
            lblSummaryTransfer.Text =
                $"{_report.TotalAttributesToTransfer} attributes to transfer  |  " +
                $"{_report.TotalSetsToReconstruct} sets to reconstruct  |  " +
                $"Analysis: {_report.AnalysisDuration.TotalSeconds:F1}s";

            // Grid: Matched
            gridMatched.Rows.Clear();
            foreach (var (old, newFP, result) in _report.Matched)
            {
                gridMatched.Rows.Add(
                    old.DisplayName,
                    newFP.DisplayName,
                    result.Level,
                    $"{result.Score:F0}%",
                    old.AutisAttributes?.Count ?? 0,
                    result.Justification);
            }

            // Grid: New
            gridNew.Rows.Clear();
            foreach (var fp in _report.NewElements)
            {
                gridNew.Rows.Add(
                    fp.DisplayName,
                    fp.TypeName ?? fp.ClassName,
                    fp.HierarchyPath);
            }

            // Grid: Removed
            gridRemoved.Rows.Clear();
            foreach (var fp in _report.RemovedElements)
            {
                gridRemoved.Rows.Add(
                    fp.DisplayName,
                    fp.TypeName ?? fp.ClassName,
                    fp.AutisAttributes?.Count ?? 0,
                    fp.AutisSets != null ? string.Join(", ", fp.AutisSets) : "");
            }

            // Grid: Candidates
            gridCandidates.Rows.Clear();
            foreach (var (old, newFP, result) in _report.Candidates)
            {
                gridCandidates.Rows.Add(
                    false, // Accept checkbox
                    old.DisplayName,
                    newFP.DisplayName,
                    $"{result.Score:F0}%",
                    result.Justification);
            }

            // Color code grid rows
            ColorCodeGrid(gridMatched, Color.FromArgb(232, 240, 254));    // light blue
            ColorCodeGrid(gridNew, Color.FromArgb(232, 250, 232));         // light green
            ColorCodeGrid(gridRemoved, Color.FromArgb(254, 232, 232));     // light red
            ColorCodeGrid(gridCandidates, Color.FromArgb(255, 243, 224));  // light orange
        }

        private void ColorCodeGrid(DataGridView grid, Color rowColor)
        {
            foreach (DataGridViewRow row in grid.Rows)
                row.DefaultCellStyle.BackColor = rowColor;
        }

        // ════════════════════════════════════════════════════════════════
        // COLOR CODING IN NAVISWORKS
        // ════════════════════════════════════════════════════════════════

        private void ApplyColorCoding(Document doc)
        {
            try
            {
                using (var trans = doc.BeginTransaction("Autis_Merge_Colors"))
                {
                    // BLUE — matched new elements
                    var matchedItems = new ModelItemCollection();
                    foreach (var (_, newFP, _) in _report.Matched)
                        matchedItems.Add(newFP.Item);

                    if (matchedItems.Count > 0)
                    {
                        doc.Models.OverridePermanentColor(
                            (IEnumerable<ModelItem>)matchedItems,
                            new NwColor(0.3, 0.5, 0.85));
                    }

                    // GREEN — new elements
                    var newItems = new ModelItemCollection();
                    foreach (var fp in _report.NewElements)
                        newItems.Add(fp.Item);

                    if (newItems.Count > 0)
                    {
                        doc.Models.OverridePermanentColor(
                            (IEnumerable<ModelItem>)newItems,
                            new NwColor(0.2, 0.75, 0.3));
                    }

                    // RED — removed elements (old model items)
                    var removedItems = new ModelItemCollection();
                    foreach (var fp in _report.RemovedElements)
                        removedItems.Add(fp.Item);

                    if (removedItems.Count > 0)
                    {
                        doc.Models.OverridePermanentColor(
                            (IEnumerable<ModelItem>)removedItems,
                            new NwColor(0.85, 0.2, 0.2));
                    }

                    // ORANGE — candidates
                    var candidateItems = new ModelItemCollection();
                    foreach (var (_, newFP, _) in _report.Candidates)
                        candidateItems.Add(newFP.Item);

                    if (candidateItems.Count > 0)
                    {
                        doc.Models.OverridePermanentColor(
                            (IEnumerable<ModelItem>)candidateItems,
                            new NwColor(0.95, 0.65, 0.15));
                    }

                    trans.Commit();
                    _colorsApplied = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Autis] Merge color error: {ex.Message}");
            }
        }

        private void ResetColors()
        {
            if (!_colorsApplied) return;
            try
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                if (doc == null) return;
                var colorizedItems = CollectColorizedItems();
                if (colorizedItems.Count > 0)
                    doc.Models.ResetPermanentMaterials((IEnumerable<ModelItem>)colorizedItems);
                _colorsApplied = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Autis] Merge reset color error: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // EXECUTE MERGE
        // ════════════════════════════════════════════════════════════════

        private void BtnExecuteMerge_Click(object sender, EventArgs e)
        {
            if (_report == null) return;

            if (HasDuplicateAcceptedCandidates())
            {
                MessageBox.Show(
                    "There are accepted candidates pointing to the same new element.\n\n" +
                    "Keep only one accepted candidate for each target element before executing the merge.",
                    "AWP Autis",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Collect accepted candidates
            int acceptedCount = 0;
            for (int i = 0; i < gridCandidates.Rows.Count; i++)
            {
                var accepted = gridCandidates.Rows[i].Cells["Accept"].Value as bool? ?? false;
                if (accepted && i < _report.Candidates.Count)
                {
                    _report.Candidates[i].Result.Status = MatchStatus.Matched;
                    acceptedCount++;
                }
            }

            int totalToTransfer = _report.TotalAttributesToTransfer +
                _report.Candidates.Where(c => c.Result.Status == MatchStatus.Matched)
                    .Sum(c => c.Old.AutisAttributes?.Count ?? 0);

            // Confirmation
            var confirmResult = MessageBox.Show(
                $"Transfer attributes to {_report.Matched.Count + acceptedCount} elements?\n\n" +
                $"  {totalToTransfer} attributes\n" +
                $"  {_report.TotalSetsToReconstruct} sets\n\n" +
                "This action cannot be undone.",
                "AWP Autis — Confirm Merge",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmResult != DialogResult.Yes) return;

            btnExecuteMerge.Enabled = false;
            btnExecuteMerge.Text = "Merging...";
            _mergeExecuted = true;

            try
            {
                var config = new MergeConfig
                {
                    TransferAttributes = chkAttributes.Checked,
                    TransferSets = chkSets.Checked
                };

                var result = MergeService.ExecuteMerge(_report, config,
                    (s, pct) => UpdateStatus(s));

                ToastNotification.Show(
                    result.errors == 0
                        ? $"Merge complete! {result.transferred} elements transferred."
                        : $"Merge done with {result.errors} errors. {result.transferred} transferred.",
                    result.errors == 0
                        ? ToastNotification.ToastType.Success
                        : ToastNotification.ToastType.Warning);

                MessageBox.Show(result.message, "AWP Autis — Merge Result",
                    MessageBoxButtons.OK,
                    result.errors > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _mergeExecuted = false;
                MessageBox.Show($"Error during merge:\n\n{ex.Message}",
                    "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnExecuteMerge.Enabled = true;
                btnExecuteMerge.Text = "Execute Merge";
            }
        }

        // ════════════════════════════════════════════════════════════════
        // EXPORT CSV
        // ════════════════════════════════════════════════════════════════

        private void BtnExportCsv_Click(object sender, EventArgs e)
        {
            if (_report == null) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Export Merge Report";
                sfd.Filter = "CSV Files (*.csv)|*.csv";
                sfd.FileName = $"MergeReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (sfd.ShowDialog() != DialogResult.OK) return;

                try
                {
                    using (var writer = new StreamWriter(sfd.FileName, false,
                        System.Text.Encoding.UTF8))
                    {
                        writer.WriteLine("Status,Level,Score,OldElement,NewElement,Detail,Attributes,Sets");

                        foreach (var (old, newFP, result) in _report.Matched)
                        {
                            writer.WriteLine($"Matched,{result.Level},{result.Score:F0}," +
                                $"\"{Esc(old.DisplayName)}\",\"{Esc(newFP.DisplayName)}\"," +
                                $"\"{Esc(result.Justification)}\"," +
                                $"{old.AutisAttributes?.Count ?? 0}," +
                                $"{old.AutisSets?.Count ?? 0}");
                        }

                        foreach (var fp in _report.NewElements)
                        {
                            writer.WriteLine($"New,0,0,,\"{Esc(fp.DisplayName)}\"," +
                                $"\"{Esc(fp.HierarchyPath)}\",0,0");
                        }

                        foreach (var fp in _report.RemovedElements)
                        {
                            writer.WriteLine($"Removed,0,0,\"{Esc(fp.DisplayName)}\",,," +
                                $"{fp.AutisAttributes?.Count ?? 0}," +
                                $"{fp.AutisSets?.Count ?? 0}");
                        }

                        foreach (var (old, newFP, result) in _report.Candidates)
                        {
                            writer.WriteLine($"Candidate,{result.Level},{result.Score:F0}," +
                                $"\"{Esc(old.DisplayName)}\",\"{Esc(newFP.DisplayName)}\"," +
                                $"\"{Esc(result.Justification)}\"," +
                                $"{old.AutisAttributes?.Count ?? 0}," +
                                $"{old.AutisSets?.Count ?? 0}");
                        }
                    }

                    ToastNotification.Show("Report exported!", ToastNotification.ToastType.Success);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting:\n{ex.Message}",
                        "AWP Autis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private static string Esc(string s) =>
            (s ?? "").Replace("\"", "\"\"");

        // ════════════════════════════════════════════════════════════════
        // CLEANUP
        // ════════════════════════════════════════════════════════════════

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ResetColors();
            if (!_mergeExecuted)
                RemoveAppendedModels(Autodesk.Navisworks.Api.Application.ActiveDocument);
            base.OnFormClosing(e);
        }

        private ModelItemCollection CollectColorizedItems()
        {
            var items = new ModelItemCollection();
            if (_report == null)
                return items;

            foreach (var (_, newFP, _) in _report.Matched)
                items.Add(newFP.Item);
            foreach (var fp in _report.NewElements)
                items.Add(fp.Item);
            foreach (var fp in _report.RemovedElements)
                items.Add(fp.Item);
            foreach (var (_, newFP, _) in _report.Candidates)
                items.Add(newFP.Item);

            return items;
        }

        private bool HasDuplicateAcceptedCandidates()
        {
            var acceptedTargets = new HashSet<ModelItem>();

            for (int i = 0; i < gridCandidates.Rows.Count; i++)
            {
                var accepted = gridCandidates.Rows[i].Cells["Accept"].Value as bool? ?? false;
                if (!accepted || i >= _report.Candidates.Count)
                    continue;

                var target = _report.Candidates[i].New?.Item;
                if (target == null)
                    continue;

                if (!acceptedTargets.Add(target))
                    return true;
            }

            return false;
        }

        private void CleanupTemporaryAnalysisState(Document doc)
        {
            ResetColors();
            RemoveAppendedModels(doc);
            _report = null;
            _appendedModelGuids.Clear();
        }

        private void RemoveAppendedModels(Document doc)
        {
            if (doc == null || _appendedModelGuids == null || _appendedModelGuids.Count == 0)
                return;

            for (int i = doc.Models.Count - 1; i >= 0; i--)
            {
                var model = doc.Models[i];
                if (!_appendedModelGuids.Contains(model.Guid))
                    continue;

                if (!doc.TryRemoveFile(i))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Autis] Could not remove appended model at index {i}: {model.SourceFileName}");
                }
            }

            _appendedModelGuids.Clear();
        }

        // ════════════════════════════════════════════════════════════════
        // HELPER: Rounded rect (same as ColorizerForm)
        // ════════════════════════════════════════════════════════════════

        private static GraphicsPath CriarRoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
