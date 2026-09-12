using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PingMonitor
{
    public sealed class MonitoringListsForm : Form
    {
        private readonly List<ServerDefinition> servers;
        private readonly List<MonitoringListDefinition> workingLists;
        private readonly string language;
        private readonly Func<ServerDefinition, string> countryText;
        private readonly Func<ServerDefinition, string> locationText;
        private readonly Func<ServerDefinition, string> providerText;
        private readonly ImageList flagImages;
        private readonly Icon windowIcon;

        private readonly ListBox listsBox = new ListBox();
        private readonly ListView serversView = new ListView();
        private readonly TextBox txtSearch = new TextBox();
        private readonly ComboBox cmbProvider = new ComboBox();
        private readonly ComboBox cmbRegion = new ComboBox();
        private readonly Label lblSelectedCount = new Label();
        private readonly Label lblEmptyHint = new Label();
        private readonly Button btnAdd = new ModernButton();
        private readonly Button btnRename = new ModernButton();
        private readonly Button btnDelete = new ModernButton();
        private readonly Button btnSelectVisible = new ModernButton();
        private readonly Button btnClearVisible = new ModernButton();
        private readonly Button btnSave = new ModernButton();
        private readonly Button btnCancel = new ModernButton();
        private readonly Button btnImport = new ModernButton();
        private readonly Button btnExport = new ModernButton();

        private MonitoringListDefinition currentList;
        private HashSet<string> selectedServerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool suppressEvents;
        private bool arranging;
        private float columnsScale = 1;

        public MonitoringListsForm(
            IEnumerable<ServerDefinition> serverCatalog,
            IEnumerable<MonitoringListDefinition> existingLists,
            string currentLanguage,
            Func<ServerDefinition, string> countryDisplay,
            Func<ServerDefinition, string> locationDisplay,
            Func<ServerDefinition, string> providerDisplay,
            ImageList countryFlags,
            Icon applicationIcon)
        {
            servers = (serverCatalog ?? Enumerable.Empty<ServerDefinition>()).ToList();
            workingLists = (existingLists ?? Enumerable.Empty<MonitoringListDefinition>()).Select(x => x.Clone()).ToList();
            language = currentLanguage == "en" ? "en" : "ar";
            countryText = countryDisplay;
            locationText = locationDisplay;
            providerText = providerDisplay;
            flagImages = countryFlags;
            windowIcon = applicationIcon;

            InitializeUi();
            UiEffects.PrepareFadeIn(this);
            PopulateFilters();
            RefreshLists(null);
            Layout += (s, e) => ArrangeListsLayout();
            ArrangeListsLayout();
        }

        public List<MonitoringListDefinition> ResultLists
        {
            get { return workingLists.Select(x => x.Clone()).ToList(); }
        }

        private bool IsArabic { get { return language == "ar"; } }

        private void ArrangeListsLayout()
        {
            if (arranging) return;
            arranging = true;
            try
            {
                float scale = Font.SizeInPoints / 9.5f * DeviceDpi / 96f;
                Func<int, int> px = value => (int)Math.Round(value * scale);
                if (Math.Abs(columnsScale - scale) > .01f)
                {
                    int[] widths = { 125, 150, 150, 235 };
                    for (int i = 0; i < serversView.Columns.Count; i++) serversView.Columns[i].Width = px(widths[i]);
                    columnsScale = scale;
                }
                int margin = px(15), gap = px(10), bottom = ClientSize.Height - margin - px(38);
                int leftWidth = px(220), rightX = margin + leftWidth + px(20), rightWidth = ClientSize.Width - rightX - margin;
                btnImport.SetBounds(margin, bottom, px(100), px(38)); btnExport.SetBounds(margin + px(110), bottom, px(120), px(38));
                btnCancel.SetBounds(ClientSize.Width - margin - px(90), bottom, px(90), px(38));
                btnSave.SetBounds(btnCancel.Left - gap - px(90), bottom, px(90), px(38));
                int selectionY = bottom - px(50);
                btnAdd.SetBounds(margin, selectionY, px(70), px(34)); btnRename.SetBounds(margin + px(75), selectionY, px(70), px(34));
                btnDelete.SetBounds(margin + px(150), selectionY, px(70), px(34));
                listsBox.SetBounds(margin, px(45), leftWidth, Math.Max(px(100), selectionY - px(45) - gap));
                int filterWidth = Math.Max(px(120), (rightWidth - px(30)) / 3);
                txtSearch.SetBounds(rightX, px(17), filterWidth, px(27));
                Controls["searchCaption"].SetBounds(rightX, 0, px(100), px(15));
                cmbProvider.SetBounds(txtSearch.Right + gap, px(17), filterWidth, px(28));
                cmbRegion.SetBounds(cmbProvider.Right + gap, px(17), Math.Max(px(100), ClientSize.Width - margin - cmbProvider.Right - gap), px(28));
                serversView.SetBounds(rightX, px(55), rightWidth, Math.Max(px(100), selectionY - px(55) - gap));
                btnSelectVisible.SetBounds(rightX, selectionY, px(115), px(34));
                btnClearVisible.SetBounds(rightX + px(120), selectionY, px(115), px(34));
                lblSelectedCount.SetBounds(rightX + px(250), selectionY, Math.Max(px(100), rightWidth - px(250)), px(34));
                lblEmptyHint.SetBounds(rightX + gap, serversView.Top + serversView.Height / 2 - px(25), rightWidth - 2 * gap, px(50));
            }
            finally { arranging = false; }
        }

        private void InitializeUi()
        {
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = IsArabic ? "إدارة قوائم المراقبة" : "Manage Monitor Lists";
            ClientSize = new Size(960, 600);
            MinimumSize = new Size(960, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(25, 25, 25);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            if (windowIcon != null) Icon = windowIcon;

            Label lblLists = CreateLabel(IsArabic ? "القوائم التي أنشأتها" : "Your lists", 15, 15, 220);
            Controls.Add(lblLists);

            listsBox.Location = new Point(15, 45);
            listsBox.Size = new Size(220, 440);
            listsBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listsBox.BackColor = Color.FromArgb(35, 35, 38);
            listsBox.ForeColor = Color.White;
            listsBox.BorderStyle = BorderStyle.FixedSingle;
            listsBox.DisplayMember = "Name";
            listsBox.SelectedIndexChanged += ListsBox_SelectedIndexChanged;
            Controls.Add(listsBox);

            ConfigureButton(btnAdd, IsArabic ? "+ إضافة" : "+ Add", new Point(15, 495), new Size(70, 34));
            ConfigureButton(btnRename, IsArabic ? "تسمية" : "Rename", new Point(90, 495), new Size(70, 34));
            ConfigureButton(btnDelete, IsArabic ? "حذف" : "Delete", new Point(165, 495), new Size(70, 34));
            btnAdd.Anchor = btnRename.Anchor = btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAdd.Click += BtnAdd_Click;
            btnRename.Click += BtnRename_Click;
            btnDelete.Click += BtnDelete_Click;
            Controls.AddRange(new Control[] { btnAdd, btnRename, btnDelete });

            txtSearch.Location = new Point(255, 17);
            txtSearch.Size = new Size(220, 27);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            txtSearch.BackColor = Color.FromArgb(40, 40, 43);
            txtSearch.ForeColor = Color.White;
            txtSearch.BorderStyle = BorderStyle.FixedSingle;
            txtSearch.TextChanged += FilterChanged;
            Controls.Add(txtSearch);

            Label lblSearch = CreateLabel(IsArabic ? "بحث" : "Search", 255, 0, 100);
            lblSearch.Name = "searchCaption";
            lblSearch.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            Controls.Add(lblSearch);

            ConfigureCombo(cmbProvider, new Point(490, 17), new Size(210, 28));
            ConfigureCombo(cmbRegion, new Point(715, 17), new Size(225, 28));
            cmbProvider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbRegion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbProvider.SelectedIndexChanged += FilterChanged;
            cmbRegion.SelectedIndexChanged += FilterChanged;
            Controls.Add(cmbProvider);
            Controls.Add(cmbRegion);

            serversView.Location = new Point(255, 55);
            serversView.Size = new Size(685, 430);
            serversView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            serversView.BackColor = Color.FromArgb(30, 30, 30);
            serversView.ForeColor = Color.White;
            serversView.BorderStyle = BorderStyle.FixedSingle;
            serversView.CheckBoxes = true;
            serversView.FullRowSelect = true;
            serversView.GridLines = true;
            serversView.HideSelection = false;
            serversView.View = View.Details;
            if (flagImages != null) serversView.SmallImageList = flagImages;
            serversView.Columns.Add(IsArabic ? "الدولة" : "Country", 125);
            serversView.Columns.Add(IsArabic ? "المدينة/المنطقة" : "Location", 150);
            serversView.Columns.Add(IsArabic ? "المزود" : "Provider", 150);
            serversView.Columns.Add(IsArabic ? "الخادم" : "Host", 235);
            serversView.ItemChecked += ServersView_ItemChecked;
            Controls.Add(serversView);

            lblEmptyHint.Location = new Point(275, 245);
            lblEmptyHint.Size = new Size(640, 50);
            lblEmptyHint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblEmptyHint.TextAlign = ContentAlignment.MiddleCenter;
            lblEmptyHint.ForeColor = Color.Silver;
            lblEmptyHint.Text = IsArabic ? "اضغط «+ إضافة» لإنشاء أول قائمة مراقبة." : "Click “+ Add” to create your first monitor list.";
            Controls.Add(lblEmptyHint);
            lblEmptyHint.BringToFront();

            ConfigureButton(btnSelectVisible, IsArabic ? "تحديد الظاهر" : "Select visible", new Point(255, 495), new Size(115, 34));
            ConfigureButton(btnClearVisible, IsArabic ? "إلغاء الظاهر" : "Clear visible", new Point(375, 495), new Size(115, 34));
            btnSelectVisible.Anchor = btnClearVisible.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSelectVisible.Click += BtnSelectVisible_Click;
            btnClearVisible.Click += BtnClearVisible_Click;
            Controls.Add(btnSelectVisible);
            Controls.Add(btnClearVisible);

            lblSelectedCount.Location = new Point(505, 500);
            lblSelectedCount.Size = new Size(230, 28);
            lblSelectedCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSelectedCount.ForeColor = Color.FromArgb(100, 200, 255);
            lblSelectedCount.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            Controls.Add(lblSelectedCount);

            ConfigureButton(btnSave, IsArabic ? "حفظ" : "Save", new Point(750, 545), new Size(90, 38));
            ConfigureButton(btnCancel, IsArabic ? "إلغاء" : "Cancel", new Point(850, 545), new Size(90, 38));
            btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.BackColor = Color.FromArgb(40, 167, 69);
            btnCancel.BackColor = Color.FromArgb(80, 80, 85);
            btnSave.Click += BtnSave_Click;
            btnCancel.DialogResult = DialogResult.Cancel;
            Controls.Add(btnSave);
            Controls.Add(btnCancel);

            ConfigureButton(btnImport, IsArabic ? "استيراد" : "Import", new Point(15, 545), new Size(100, 38));
            ConfigureButton(btnExport, IsArabic ? "تصدير القوائم" : "Export lists", new Point(125, 545), new Size(120, 38));
            btnImport.Anchor = btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnImport.Click += ImportLists;
            btnExport.Click += ExportLists;
            Controls.AddRange(new Control[] { btnImport, btnExport });

            foreach (Button button in new[]
            {
                btnAdd, btnRename, btnDelete, btnSelectVisible, btnClearVisible, btnSave, btnCancel, btnImport, btnExport
            })
            {
                ModernButton modern = (ModernButton)button;
                modern.GradientColor1 = button.BackColor;
                modern.GradientColor2 = UiEffects.Blend(button.BackColor, Color.White, .08f);
                UiEffects.AttachColorHover(
                    button,
                    button.BackColor,
                    UiEffects.Blend(button.BackColor, Color.White, 0.14f),
                    button.ForeColor,
                    Color.White);
            }

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void PopulateFilters()
        {
            suppressEvents = true;
            cmbProvider.Items.Clear();
            cmbProvider.Items.Add(new FilterItem { Id = "all", Text = IsArabic ? "كل المزودين" : "All providers" });
            foreach (ServerDefinition server in servers.GroupBy(x => x.ProviderId).Select(x => x.First()).OrderBy(providerText))
                cmbProvider.Items.Add(new FilterItem { Id = server.ProviderId, Text = providerText(server) });

            cmbRegion.Items.Clear();
            cmbRegion.Items.Add(new FilterItem { Id = "all", Text = IsArabic ? "كل المناطق" : "All regions" });
            cmbRegion.Items.Add(new FilterItem { Id = "saudi-arabia", Text = IsArabic ? "السعودية" : "Saudi Arabia" });
            cmbRegion.Items.Add(new FilterItem { Id = "gulf", Text = IsArabic ? "الخليج" : "Gulf" });
            cmbRegion.Items.Add(new FilterItem { Id = "europe", Text = IsArabic ? "أوروبا" : "Europe" });
            cmbRegion.Items.Add(new FilterItem { Id = "asia", Text = IsArabic ? "آسيا" : "Asia" });
            cmbProvider.SelectedIndex = 0;
            cmbRegion.SelectedIndex = 0;
            suppressEvents = false;
        }

        private void ImportLists(object sender, EventArgs e)
        {
            using (var file = new OpenFileDialog { Filter = "PingMonitor lists (*.xml)|*.xml", CheckFileExists = true })
            {
                if (file.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    ListImportPreview preview = MonitoringListTransfer.Read(file.FileName, servers, workingLists);
                    string text = string.Join(Environment.NewLine, preview.Lists.Select(x => x.Name + " — " + x.ServerIds.Count + (IsArabic ? " خادم" : " servers")));
                    text += Environment.NewLine + Environment.NewLine + (IsArabic ? "أسماء أُعيدت تسميتها: " : "Renamed lists: ") + preview.RenamedLists;
                    text += Environment.NewLine + (IsArabic ? "معرفات غير معروفة سيتم تجاهلها: " : "Unknown IDs to skip: ") + preview.UnknownServers;
                    text += Environment.NewLine + (IsArabic ? "ستُضاف كقوائم جديدة دون استبدال قوائمك. اضغط حفظ بعد الدمج للاحتفاظ بها." : "Adds new lists without replacing yours. Click Save after merging to keep them.");
                    using (var review = new TextReviewForm(IsArabic ? "معاينة الاستيراد" : "Import preview", text, IsArabic ? "إضافة القوائم" : "Add lists", IsArabic))
                    {
                        if (review.ShowDialog(this) != DialogResult.OK) return;
                        SaveCurrentSelection(); workingLists.AddRange(preview.Lists); RefreshLists(preview.Lists[0].Id);
                    }
                }
                catch (Exception ex) { MessageBox.Show(this, (IsArabic ? "تعذر الاستيراد: " : "Import failed: ") + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }

        private void ExportLists(object sender, EventArgs e)
        {
            SaveCurrentSelection();
            using (var file = new SaveFileDialog { Filter = "PingMonitor lists (*.xml)|*.xml", DefaultExt = "xml", AddExtension = true, FileName = "PingMonitor-lists.xml" })
            {
                if (file.ShowDialog(this) != DialogResult.OK) return;
                try { MonitoringListTransfer.Export(file.FileName, workingLists); }
                catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }

        private void RefreshLists(string selectId)
        {
            suppressEvents = true;
            listsBox.BeginUpdate();
            listsBox.Items.Clear();
            foreach (MonitoringListDefinition list in workingLists)
                listsBox.Items.Add(list);
            listsBox.EndUpdate();

            int selectedIndex = -1;
            if (!string.IsNullOrEmpty(selectId))
            {
                for (int i = 0; i < listsBox.Items.Count; i++)
                {
                    MonitoringListDefinition item = (MonitoringListDefinition)listsBox.Items[i];
                    if (item.Id.Equals(selectId, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            if (selectedIndex < 0 && listsBox.Items.Count > 0) selectedIndex = 0;
            listsBox.SelectedIndex = selectedIndex;
            suppressEvents = false;
            LoadSelectedList();
        }

        private void ListsBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressEvents) return;
            SaveCurrentSelection();
            LoadSelectedList();
        }

        private void LoadSelectedList()
        {
            currentList = listsBox.SelectedItem as MonitoringListDefinition;
            selectedServerIds = currentList == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(currentList.ServerIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            ApplyFilter(false);
            UpdateEnabledState();
        }

        private void SaveCurrentSelection()
        {
            CaptureVisibleChecks();
            if (currentList != null)
                currentList.ServerIds = selectedServerIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void CaptureVisibleChecks()
        {
            if (currentList == null) return;
            foreach (ListViewItem item in serversView.Items)
            {
                string id = item.Tag as string;
                if (string.IsNullOrEmpty(id)) continue;
                if (item.Checked) selectedServerIds.Add(id);
                else selectedServerIds.Remove(id);
            }
        }

        private void ApplyFilter(bool captureCurrent = true)
        {
            if (suppressEvents) return;
            if (captureCurrent) CaptureVisibleChecks();

            FilterItem provider = cmbProvider.SelectedItem as FilterItem;
            FilterItem region = cmbRegion.SelectedItem as FilterItem;
            string search = (txtSearch.Text ?? string.Empty).Trim();

            IEnumerable<ServerDefinition> filtered = servers;
            if (provider != null && provider.Id != "all") filtered = filtered.Where(x => x.ProviderId == provider.Id);
            if (region != null && region.Id != "all") filtered = filtered.Where(x => x.RegionId == region.Id);
            if (search.Length > 0)
            {
                filtered = filtered.Where(x =>
                    countryText(x).IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    locationText(x).IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    providerText(x).IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    x.Host.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            suppressEvents = true;
            serversView.BeginUpdate();
            serversView.Items.Clear();
            foreach (ServerDefinition server in filtered)
            {
                ListViewItem item = new ListViewItem(countryText(server)) { ImageKey = server.CountryCode };
                item.SubItems.Add(locationText(server));
                item.SubItems.Add(providerText(server));
                item.SubItems.Add(server.Host);
                item.Tag = server.Id;
                item.Checked = selectedServerIds.Contains(server.Id);
                serversView.Items.Add(item);
            }
            serversView.EndUpdate();
            suppressEvents = false;
            UpdateSelectedCount();
        }

        private void ServersView_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (suppressEvents || currentList == null) return;
            string id = e.Item.Tag as string;
            if (string.IsNullOrEmpty(id)) return;
            if (e.Item.Checked) selectedServerIds.Add(id);
            else selectedServerIds.Remove(id);
            BeginInvoke(new Action(UpdateSelectedCount));
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            if (!suppressEvents) ApplyFilter();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string name = PromptForName(IsArabic ? "اسم القائمة الجديدة:" : "New list name:", string.Empty);
            if (name == null) return;
            if (!ValidateName(name, null)) return;

            MonitoringListDefinition list = new MonitoringListDefinition
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name.Trim(),
                ServerIds = new List<string>()
            };
            SaveCurrentSelection();
            workingLists.Add(list);
            RefreshLists(list.Id);
        }

        private void BtnRename_Click(object sender, EventArgs e)
        {
            if (currentList == null) return;
            string name = PromptForName(IsArabic ? "الاسم الجديد:" : "New name:", currentList.Name);
            if (name == null || !ValidateName(name, currentList.Id)) return;
            SaveCurrentSelection();
            currentList.Name = name.Trim();
            RefreshLists(currentList.Id);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (currentList == null) return;
            string message = IsArabic ? "هل تريد حذف القائمة «" + currentList.Name + "»؟" : "Delete “" + currentList.Name + "”?";
            string title = IsArabic ? "تأكيد الحذف" : "Confirm delete";
            if (MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            string id = currentList.Id;
            workingLists.RemoveAll(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            currentList = null;
            RefreshLists(null);
        }

        private void BtnSelectVisible_Click(object sender, EventArgs e)
        {
            if (currentList == null) return;
            suppressEvents = true;
            foreach (ListViewItem item in serversView.Items)
            {
                item.Checked = true;
                selectedServerIds.Add((string)item.Tag);
            }
            suppressEvents = false;
            UpdateSelectedCount();
        }

        private void BtnClearVisible_Click(object sender, EventArgs e)
        {
            if (currentList == null) return;
            suppressEvents = true;
            foreach (ListViewItem item in serversView.Items)
            {
                item.Checked = false;
                selectedServerIds.Remove((string)item.Tag);
            }
            suppressEvents = false;
            UpdateSelectedCount();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveCurrentSelection();
            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ValidateName(string name, string currentId)
        {
            string trimmed = (name ?? string.Empty).Trim();
            string error = null;
            if (trimmed.Length == 0 || trimmed.Length > 60)
                error = IsArabic ? "يجب أن يكون الاسم بين 1 و60 حرفًا." : "The name must be between 1 and 60 characters.";
            else if (trimmed.Equals("كل الخوادم", StringComparison.CurrentCultureIgnoreCase) ||
                     trimmed.Equals("All servers", StringComparison.OrdinalIgnoreCase))
                error = IsArabic ? "هذا الاسم محجوز للقائمة الافتراضية." : "This name is reserved for the default list.";
            else if (workingLists.Any(x => !x.Id.Equals(currentId ?? string.Empty, StringComparison.OrdinalIgnoreCase) && x.Name.Equals(trimmed, StringComparison.CurrentCultureIgnoreCase)))
                error = IsArabic ? "يوجد قائمة أخرى بالاسم نفسه." : "Another list already uses this name.";

            if (error == null) return true;
            MessageBox.Show(this, error, IsArabic ? "اسم غير صالح" : "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private string PromptForName(string prompt, string initialValue)
        {
            using (Form dialog = new Form())
            {
                dialog.Text = IsArabic ? "اسم القائمة" : "List name";
                dialog.ClientSize = new Size(390, 145);
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.BackColor = Color.FromArgb(25, 25, 25);
                dialog.ForeColor = Color.White;
                dialog.Font = Font;

                Label label = CreateLabel(prompt, 18, 15, 350);
                TextBox input = new TextBox
                {
                    Location = new Point(18, 45),
                    Size = new Size(350, 27),
                    Text = initialValue ?? string.Empty,
                    BackColor = Color.FromArgb(40, 40, 43),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                Button ok = new ModernButton { Text = IsArabic ? "موافق" : "OK", Location = new Point(198, 92), Size = new Size(80, 34), DialogResult = DialogResult.OK };
                Button cancel = new ModernButton { Text = IsArabic ? "إلغاء" : "Cancel", Location = new Point(288, 92), Size = new Size(80, 34), DialogResult = DialogResult.Cancel };
                ConfigureFlatButton(ok);
                ConfigureFlatButton(cancel);
                UiEffects.AttachColorHover(ok, ok.BackColor, UiEffects.Blend(ok.BackColor, Color.White, 0.14f), ok.ForeColor, Color.White);
                UiEffects.AttachColorHover(cancel, cancel.BackColor, UiEffects.Blend(cancel.BackColor, Color.White, 0.14f), cancel.ForeColor, Color.White);
                UiEffects.PrepareFadeIn(dialog, 170);
                dialog.Controls.AddRange(new Control[] { label, input, ok, cancel });
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;
                dialog.Shown += (s, e) => { input.Focus(); input.SelectAll(); };
                return dialog.ShowDialog(this) == DialogResult.OK ? input.Text : null;
            }
        }

        private void UpdateEnabledState()
        {
            bool enabled = currentList != null;
            serversView.Enabled = enabled;
            txtSearch.Enabled = enabled;
            cmbProvider.Enabled = enabled;
            cmbRegion.Enabled = enabled;
            btnRename.Enabled = enabled;
            btnDelete.Enabled = enabled;
            btnSelectVisible.Enabled = enabled;
            btnClearVisible.Enabled = enabled;
            btnExport.Enabled = workingLists.Count > 0;
            lblEmptyHint.Visible = !enabled;
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            int count = currentList == null ? 0 : selectedServerIds.Count;
            lblSelectedCount.Text = IsArabic ? "المحدد: " + count + " خادم" : "Selected: " + count + " servers";
        }

        private static Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 24), ForeColor = Color.White };
        }

        private static void ConfigureCombo(ComboBox combo, Point location, Size size)
        {
            combo.Location = location;
            combo.Size = size;
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Color.FromArgb(40, 40, 43);
            combo.ForeColor = Color.White;
            combo.DisplayMember = "Text";
        }

        private static void ConfigureButton(Button button, string text, Point location, Size size)
        {
            button.Text = text;
            button.Location = location;
            button.Size = size;
            ConfigureFlatButton(button);
        }

        private static void ConfigureFlatButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Color.FromArgb(50, 50, 55);
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            ModernButton modern = button as ModernButton;
            if (modern != null) { modern.GradientColor1 = button.BackColor; modern.GradientColor2 = UiEffects.Blend(button.BackColor, Color.White, .08f); }
        }

        private sealed class FilterItem
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public override string ToString() { return Text; }
        }
    }
}
