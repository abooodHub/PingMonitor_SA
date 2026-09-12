using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingMonitor
{
    public partial class Form1 : Form
    {
        // ============== المتغيرات الخاصة بالنموذج ==============
        private bool isIPVisible = false;
        private int updateInterval = 5000;
        private string currentLanguage = "ar";
        private bool updating = true;
        private bool isClosing = false;

        private readonly Dictionary<string, ServerMetrics> serverMetrics = new Dictionary<string, ServerMetrics>(StringComparer.OrdinalIgnoreCase);
        private readonly MonitoringListStore monitoringListStore = new MonitoringListStore();
        private readonly WanIpService wanIpService = new WanIpService();
        private MonitoringListsState monitoringListsState;
        private List<ServerDefinition> serverDefinitions = new List<ServerDefinition>();
        private CancellationTokenSource scanCancellation;
        private bool scanInProgress;
        private int scanGeneration;
        private bool suppressProviderEvents;
        private bool suppressMonitorListEvents;
        private CancellationTokenSource wanIpCancellation;
        private int wanIpGeneration;
        private readonly Dictionary<string, float> metricPulseStrengths = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private System.Windows.Forms.Timer metricPulseTimer;
        private readonly Stopwatch metricPulseClock = Stopwatch.StartNew();
        private readonly Dictionary<string, long> metricPulseStartedAt = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        // متغيرات لحساب إحصائيات الخوادم
        private int activeServerCount = 0;
        private int offlineServerCount = 0;

        // خريطة أسماء المزودين إلى العربية
        private Dictionary<string, string> providerMapAr = new Dictionary<string, string>
        {
            {"Amazon", "أمازون"},
            {"Google Cloud", "جوجل السحابية"},
            {"STC", "الاتصالات السعودية"},
            {"Mobily", "موبايلي"},
            {"Zain", "زين"},
            {"GO Telecom", "قو اتصالات"},
            {"Salam", "سلام"},
            {"Etisalat", "اتصالات الإمارات"},
            {"du", "دو الإمارات"},
            {"STC Bahrain", "اتصالات البحرين"},
            {"Zain Bahrain", "زين البحرين"},
            {"Etisalcom", "اتصالكم البحرين"}
        };

        // خريطة أسماء الدول إلى العربية
        private Dictionary<string, string> countryMapAr = new Dictionary<string, string>
        {
            {"sa", "السعودية"},
            {"ae", "الإمارات"},
            {"bh", "البحرين"},
            {"i_n", "الهند"},
            {"de", "ألمانيا"},
            {"fr", "فرنسا"},
            {"gb", "المملكة المتحدة"},
            {"es", "إسبانيا"},
            {"ie", "أيرلندا"},
            {"it", "إيطاليا"},
            {"ch", "سويسرا"},
            {"se", "السويد"},
            {"qa", "قطر"},
            {"no", "النرويج"},
            {"pl", "بولندا"},
            {"nl", "هولندا"}
        };

        // خريطة أسماء الدول إلى الإنجليزية
        private Dictionary<string, string> countryMapEn = new Dictionary<string, string>
        {
            {"sa", "Saudi Arabia"},
            {"ae", "United Arab Emirates"},
            {"bh", "Bahrain"},
            {"i_n", "India"},
            {"de", "Germany"},
            {"fr", "France"},
            {"gb", "United Kingdom"},
            {"es", "Spain"},
            {"ie", "Ireland"},
            {"it", "Italy"},
            {"ch", "Switzerland"},
            {"se", "Sweden"},
            {"qa", "Qatar"},
            {"no", "Norway"},
            {"pl", "Poland"},
            {"nl", "Netherlands"}
        };

        // خريطة أسماء المدن من العربية إلى الإنجليزية
        private Dictionary<string, string> locationMapEn = new Dictionary<string, string>
        {
            {"الرياض", "Riyadh"},
            {"جدة", "Jeddah"},
            {"المدينة المنورة", "Madina"},
            {"تبوك", "Tabuk"},
            {"جازان", "Jazan"},
            {"نجران", "Najran"},
            {"الدمام", "Dammam"},
            {"مكة", "Makkah"},
            {"خميس مشيط", "Khamis Mushait"},
            {"أبو ظبي", "Abu Dhabi"},
            {"دبي", "Dubai"},
            {"الرفاع", "Riffa"},
            {"سيف", "Seef"},
            {"المنامة", "Manama"},
            {"المحرق", "Muharraq"},
            {"مومباي", "Mumbai"},
            {"فرانكفورت", "Frankfurt"},
            {"باريس", "Paris"},
            {"لندن", "London"},
            {"إسپانيا", "Spain"},
            {"أيرلندا", "Ireland"},
            {"ميلانو", "Milan"},
            {"زيورخ", "Zurich"},
            {"ستوكهولم", "Stockholm"},
            {"الدوحة", "Doha"},
            {"أوسلو", "Oslo"},
            {"وارسو", "Warsaw"},
            {"أمستردام", "Amsterdam"},
            {"ينبع", "Yanbu"},
            {"الحمراء", "Alhamra'a"},
            {"أبحر", "Obhur"},
            {"بحرة", "Bahrah"},
            {"أبيار علي", "Abyar 'Ali"},
            {"القصيم", "Qassim"},
            {"المشاعر", "Al Mashair"},
            {"الخبر", "Al-Khobar"},
            {"الهفوف", "Hofuf"},
            {"توبلي", "Tubli"}
        };

        // أسماء المزودين بالترتيب للّوحة الأفقية
        private string[] providersAr = { "أمازون", "شرق الأوسط", "الاتصالات السعودية", "موبايلي", "زين", "سلام", "قو اتصالات" };
        private string[] providersEn = { "Amazon", "Middle East", "STC", "Mobily", "Zain", "Salam", "GO Telecom" };
        private string[] providerIds = { "amazon", "middle-east", "stc", "mobily", "zain", "salam", "go-telecom" };

        // ============== Constructor ==============
        public Form1() : this(null) { }

        public Form1(MonitoringListStore customStore)
        {
            SuspendLayout();
            if (customStore != null) monitoringListStore = customStore;
            InitializeComponent();
            MinimumSize = new Size(640, 520);

            metricPulseTimer = new System.Windows.Forms.Timer(components) { Interval = 30 };
            metricPulseTimer.Tick += MetricPulseTimer_Tick;

            // تحميل الإعدادات المحفوظة
            LoadSettings();

            // ضبط اللغة الافتراضية إلى العربية
            serverDefinitions = ServerCatalog.Create();
            monitoringListsState = monitoringListStore.Load(serverDefinitions.Select(x => x.Id), ServerCatalog.GetLegacyIdMap());
            currentLanguage = monitoringListsState.Language == "en" ? "en" : "ar";
            UiEffects.ReduceMotion = monitoringListsState.ReduceMotion;
            UiEffects.PrepareFadeIn(this, 260);
            InitializeMonitoringUi();

            btnToggleIP.GradientColor1 = Color.FromArgb(0, 180, 240);
            btnToggleIP.GradientColor2 = Color.FromArgb(0, 140, 200);
            btnToggleTimer.GradientColor1 = Color.FromArgb(40, 167, 69);
            btnToggleTimer.GradientColor2 = Color.FromArgb(60, 200, 100);
            btnChangeInterval.GradientColor1 = Color.FromArgb(255, 152, 0);
            btnChangeInterval.GradientColor2 = Color.FromArgb(255, 180, 50);
            btnToggleLang.GradientColor1 = Color.FromArgb(156, 39, 176);
            btnToggleLang.GradientColor2 = Color.FromArgb(190, 80, 210);
            btnManageMonitorLists.GradientColor1 = Color.FromArgb(70, 80, 100);
            btnManageMonitorLists.GradientColor2 = Color.FromArgb(100, 120, 150);

            // تحميل صور الأعلام من الموارد
            LoadFlagImages_FromResources();

            // إعداد أعمدة الـ ListView بناءً على اللغة
            SetupListViewColumns();

            // تهيئة قائمة الخوادم


            // تفعيل الرسم اليدوي للألوان
            serverListView.OwnerDraw = true;
            serverListView.DrawColumnHeader += ServerListView_DrawColumnHeader;
            serverListView.DrawSubItem += ServerListView_DrawSubItem;

            // ربط حدث الـ PingTimer للتحديث الدوري
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Interval = updateInterval;
            updating = true;

            // ضبط نصوص الأزرار بالوضع العربي
            btnToggleTimer.Text = "⏸ إيقاف";
            btnChangeInterval.Text = "⏱ تغيير الفترة";
            btnToggleIP.Text = "👁️ إظهار IP";
            btnToggleLang.Text = "🌐 en";
            btnManageMonitorLists.Text = "⚙ إدارة القوائم";

            UpdateLanguageLabels();
            RestoreViewPreferences();

            // تعبئة FlowLayoutPanel بخانات اختيار المزودين (أفقياً)
            PopulateProviderCheckboxes();

            // استعادة حالة المزودين المحفوظة
            RestoreProviderCheckboxStates();

            // تعبئة قوائم المراقبة التي أنشأها المستخدم
            PopulateMonitorListsCombo();

            // ملء الـ ListView للمرة الأولى
            PopulateListView();

            // ربط فرز الأعمدة عند النقر على رأس العمود
            serverListView.ColumnClick += ServerListView_ColumnClick;

            // تحديث شريط الحالة
            UpdateStatusBar();

            InitializeResponsiveLayout();
            ResumeLayout(true);
            pingTimer.Start();
        }

        // ============== حدث تحميل النموذج ==============
        private void Form1_Load(object sender, EventArgs e)
        {
            lblWanIP.Visible = isIPVisible;
        }

        // ============== حدث إغلاق النموذج ==============
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
            updating = false;
            CancelCurrentScan();
            CancelWanIpRequest();
            try { pingTimer.Tick -= PingTimer_Tick; } catch {}
            try { pingTimer.Stop(); } catch {}
            try { pingTimer.Dispose(); } catch {}
            if (metricPulseTimer != null)
            {
                metricPulseTimer.Stop();
                metricPulseTimer.Tick -= MetricPulseTimer_Tick;
                metricPulseTimer = null;
            }
            settingsSaveTimer.Stop();
            SaveSettings();
            SaveMonitorState();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
        }

        // ============== حفظ الإعدادات ==============
        private void SaveSettings()
        {
            try
            {
                // حفظ الفترة الزمنية
                Properties.Settings.Default.UpdateInterval = updateInterval;

                // حفظ حالة المزودين المحددين
                List<string> selectedProviders = new List<string>();
                foreach (Control ctrl in pnlProviders.Controls)
                {
                    if (ctrl is CheckBox)
                    {
                        CheckBox cb = (CheckBox)ctrl;
                        if (cb.Checked)
                        {
                            selectedProviders.Add(Convert.ToString(cb.Tag));
                        }
                    }
                }
                Properties.Settings.Default.SelectedProviders = string.Join(",", selectedProviders);

                // حفظ الإعدادات
                Properties.Settings.Default.Save();
            }
            catch
            {
                ShowSaveError();
            }
        }

        // ============== تحميل الإعدادات ==============
        private void LoadSettings()
        {
            try
            {
                // تحميل الفترة الزمنية
                if (Properties.Settings.Default.UpdateInterval >= 1000 && Properties.Settings.Default.UpdateInterval <= 86400000)
                {
                    updateInterval = Properties.Settings.Default.UpdateInterval;
                }
            }
            catch
            {
                // استخدام القيم الافتراضية في حالة الخطأ
                updateInterval = 5000;
            }
        }

        // ============== استعادة حالة المزودين المحفوظة ==============
        private void RestoreProviderCheckboxStates()
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.SelectedProviders))
                {
                    string[] selectedProviders = Properties.Settings.Default.SelectedProviders.Split(',');
                    HashSet<string> selectedIds = new HashSet<string>(selectedProviders.Select(ConvertLegacyProviderSelection), StringComparer.OrdinalIgnoreCase);
                    if (selectedProviders.Any(x => x == "google-cloud" || x == "جوجل السحابية" || x.Equals("google cloud", StringComparison.OrdinalIgnoreCase)))
                    { selectedIds.Add("amazon"); selectedIds.Add("salam"); }
                    suppressProviderEvents = true;
                    foreach (Control ctrl in pnlProviders.Controls)
                    {
                        if (ctrl is CheckBox)
                        {
                            CheckBox cb = (CheckBox)ctrl;
                            cb.Checked = selectedIds.Contains(Convert.ToString(cb.Tag));
                        }
                    }
                    suppressProviderEvents = false;
                }
            }
            catch
            {
                // تجاهل أي أخطاء في الاستعادة
            }
        }

        // ============== دالة لإعادة تعبئة FlowLayoutPanel بخانات اختيار المزودين ==============
        private void PopulateProviderCheckboxes()
        {
            HashSet<string> checkedIds = new HashSet<string>(
                pnlProviders.Controls.OfType<CheckBox>().Where(x => x.Checked).Select(x => Convert.ToString(x.Tag)),
                StringComparer.OrdinalIgnoreCase);

            suppressProviderEvents = true;
            Control[] oldControls = pnlProviders.Controls.Cast<Control>().ToArray();
            pnlProviders.Controls.Clear();
            foreach (Control oldControl in oldControls) oldControl.Dispose();
            string[] listToUse = currentLanguage == "ar" ? providersAr : providersEn;

            for (int i = 0; i < listToUse.Length; i++)
            {
                string txt = listToUse[i];
                CheckBox cb = new MotionCheckBox
                {
                    Text = txt,
                    Tag = providerIds[i],
                    AutoSize = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Margin = new Padding(8, 5, 8, 5),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                cb.Checked = checkedIds.Contains(providerIds[i]);
                cb.CheckedChanged += ProviderCheckBox_CheckedChanged;
                pnlProviders.Controls.Add(cb);
            }
            suppressProviderEvents = false;
        }

        // ============== تحميل صور الأعلام من الموارد ==============
        private void LoadFlagImages_FromResources()
        {
            try
            {
                imageListFlags.Images.Add("sa", Properties.Resources.sa);
                imageListFlags.Images.Add("ae", Properties.Resources.ae);
                imageListFlags.Images.Add("bh", Properties.Resources.bh);
                imageListFlags.Images.Add("de", Properties.Resources.de);
                imageListFlags.Images.Add("fr", Properties.Resources.fr);
                imageListFlags.Images.Add("gb", Properties.Resources.gb);
                imageListFlags.Images.Add("es", Properties.Resources.es);
                imageListFlags.Images.Add("ie", Properties.Resources.ie);
                imageListFlags.Images.Add("it", Properties.Resources.it);
                imageListFlags.Images.Add("ch", Properties.Resources.ch);
                imageListFlags.Images.Add("se", Properties.Resources.se);
                imageListFlags.Images.Add("i_n", Properties.Resources.i_n);
                imageListFlags.Images.Add("nl", Properties.Resources.nl);
                imageListFlags.Images.Add("no", Properties.Resources.no);
                imageListFlags.Images.Add("pl", Properties.Resources.pl);
                imageListFlags.Images.Add("qa", Properties.Resources.qa);
            }
            catch
            {
                // إذا لم توجد بعض الصور في الموارد، نتجاهل الخطأ
            }
        }

        // ============== ضبط أعمدة الـ ListView بناءً على اللغة ==============
        private void SetupListViewColumns()
        {
            serverListView.Columns.Clear();

            if (currentLanguage == "ar")
            {
                serverListView.Columns.Add("العلم", 40, HorizontalAlignment.Center);
                serverListView.Columns.Add("الدولة", 120, HorizontalAlignment.Left);
                serverListView.Columns.Add("المدينة/المنطقة", 140, HorizontalAlignment.Left);
                serverListView.Columns.Add("مزود الخدمة", 150, HorizontalAlignment.Left);
                serverListView.Columns.Add("البنق", 80, HorizontalAlignment.Center);
                serverListView.Columns.Add("التذبذب", 100, HorizontalAlignment.Center);
                serverListView.Columns.Add("فقد البيانات", 100, HorizontalAlignment.Center);
            }
            else
            {
                serverListView.Columns.Add("Flag", 50, HorizontalAlignment.Center);
                serverListView.Columns.Add("Country", 120, HorizontalAlignment.Left);
                serverListView.Columns.Add("Location", 140, HorizontalAlignment.Left);
                serverListView.Columns.Add("ISP", 150, HorizontalAlignment.Left);
                serverListView.Columns.Add("Ping (ms)", 100, HorizontalAlignment.Center);
                serverListView.Columns.Add("Jitter (ms)", 100, HorizontalAlignment.Center);
                serverListView.Columns.Add("Loss (%)", 100, HorizontalAlignment.Center);
            }
            if (monitoringListsState.ColumnWidths == null || monitoringListsState.ColumnWidths.Count != 7)
            {
                int available = Math.Max(730, serverListView.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
                int[] widths = { 50, 140, 170, 185, 100, 115, Math.Max(100, available - 760) };
                for (int i = 0; i < widths.Length; i++) serverListView.Columns[i].Width = widths[i];
            }
        }

        private string GetCountryDisplay(ServerDefinition server)
        {
            Dictionary<string, string> map = currentLanguage == "ar" ? countryMapAr : countryMapEn;
            string value;
            return map.TryGetValue(server.CountryCode, out value) ? value : server.CountryCode;
        }

        private string GetLocationDisplay(ServerDefinition server)
        {
            if (currentLanguage == "ar") return server.LocationAr;
            string value;
            return locationMapEn.TryGetValue(server.LocationAr, out value) ? value : server.LocationAr;
        }

        private string GetProviderDisplay(ServerDefinition server)
        {
            if (currentLanguage == "en") return server.ProviderName;
            string value;
            return providerMapAr.TryGetValue(server.ProviderName, out value) ? value : server.ProviderName;
        }

        private string ConvertLegacyProviderSelection(string value)
        {
            string normalized = (value ?? string.Empty).Trim();
            for (int i = 0; i < providerIds.Length; i++)
            {
                if (normalized.Equals(providerIds[i], StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals(providersAr[i], StringComparison.CurrentCultureIgnoreCase) ||
                    normalized.Equals(providersEn[i], StringComparison.OrdinalIgnoreCase))
                    return providerIds[i];
            }
            return normalized.ToLowerInvariant();
        }

        private void PopulateMonitorListsCombo()
        {
            if (monitoringListsState == null) return;
            string activeId = monitoringListsState.ActiveListId;
            suppressMonitorListEvents = true;
            cmbMonitorList.BeginUpdate();
            cmbMonitorList.Items.Clear();
            cmbMonitorList.Items.Add(new MonitoringListComboItem
            {
                Id = MonitoringListStore.AllServersListId,
                Text = "📋 " + (currentLanguage == "ar" ? "كل الخوادم" : "All servers") + " (" + serverDefinitions.Count + ")"
            });
            foreach (MonitoringListDefinition list in monitoringListsState.Lists)
            {
                cmbMonitorList.Items.Add(new MonitoringListComboItem
                {
                    Id = list.Id,
                    Text = "📋 " + list.Name + " (" + (list.ServerIds == null ? 0 : list.ServerIds.Count) + ")"
                });
            }
            cmbMonitorList.EndUpdate();

            int index = 0;
            for (int i = 0; i < cmbMonitorList.Items.Count; i++)
            {
                MonitoringListComboItem item = (MonitoringListComboItem)cmbMonitorList.Items[i];
                if (item.Id.Equals(activeId, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            cmbMonitorList.SelectedIndex = index;
            monitoringListsState.ActiveListId = ((MonitoringListComboItem)cmbMonitorList.SelectedItem).Id;
            suppressMonitorListEvents = false;
        }

        private void cmbMonitorList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressMonitorListEvents || monitoringListsState == null) return;
            MonitoringListComboItem item = cmbMonitorList.SelectedItem as MonitoringListComboItem;
            if (item == null) return;
            monitoringListsState.ActiveListId = item.Id;
            CancelCurrentScan();
            PopulateListView();
            SaveMonitorState();
            RequestImmediateScan();
        }

        private void btnManageMonitorLists_Click(object sender, EventArgs e)
        {
            CancelCurrentScan();
            using (MonitoringListsForm dialog = new MonitoringListsForm(
                serverDefinitions,
                monitoringListsState.Lists,
                currentLanguage,
                GetCountryDisplay,
                GetLocationDisplay,
                GetProviderDisplay,
                imageListFlags,
                Icon))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                monitoringListsState.Lists = dialog.ResultLists;
                if (!monitoringListsState.ActiveListId.Equals(MonitoringListStore.AllServersListId, StringComparison.OrdinalIgnoreCase) &&
                    !monitoringListsState.Lists.Any(x => x.Id.Equals(monitoringListsState.ActiveListId, StringComparison.OrdinalIgnoreCase)))
                {
                    monitoringListsState.ActiveListId = MonitoringListStore.AllServersListId;
                }
                SaveMonitorState();
                PopulateMonitorListsCombo();
                PopulateListView();
                CancelCurrentScan();
                RequestImmediateScan();
            }
        }

        private void cmbMonitorList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = selected ? Color.FromArgb(65, 75, 95) : Color.FromArgb(40, 40, 43);
            using (SolidBrush backBrush = new SolidBrush(background))
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            string text = Convert.ToString(cmbMonitorList.Items[e.Index]);
            TextRenderer.DrawText(e.Graphics, text, cmbMonitorList.Font, e.Bounds, Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }

        // ============== إعادة ملء الـ ListView بناءً على المزودين المحددين واللغة ==============
        private void PopulateListView()
        {
            string selectedId = serverListView.SelectedItems.Count == 0 ? null : ((ServerRowTag)serverListView.SelectedItems[0].Tag).ServerId;
            serverListView.BeginUpdate();
            serverListView.Items.Clear();
            foreach (ServerDefinition server in GetMonitoredServers().Where(MatchesSearch))
            {
                ListViewItem item = new ListViewItem { ImageKey = server.CountryCode };
                item.SubItems.Add(GetCountryDisplay(server));
                item.SubItems.Add(GetLocationDisplay(server));
                item.SubItems.Add(GetProviderDisplay(server));
                item.SubItems.Add("-");
                item.SubItems.Add("-");
                item.SubItems.Add("-");
                item.Tag = new ServerRowTag(server.Id, server.Host);

                ServerMetrics metrics;
                if (serverMetrics.TryGetValue(server.Id, out metrics) && metrics.HasResult)
                    ApplyMetricsToItem(item, metrics);
                serverListView.Items.Add(item);
                item.Selected = server.Id == selectedId;
            }
            ApplySort();
            serverListView.EndUpdate();
            serverListView.BeginContentTransition();
            UpdateStatusBar();
        }

        // ============== حدث تغيير حالة أي CheckBox في FlowLayoutPanel ==============
        private void ProviderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressProviderEvents) return;
            CancelCurrentScan();
            PopulateListView();
            ScheduleSettingsSave();
            RequestImmediateScan();
        }

        // ============== جلب WAN IP بطريقة غير متزامنة ==============
        private async void ShowWANIP()
        {
            int generation = ++wanIpGeneration;
            CancellationTokenSource previous = wanIpCancellation;
            if (previous != null && !previous.IsCancellationRequested) previous.Cancel();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            wanIpCancellation = cancellation;
            try
            {
                lblWanIP.Text = currentLanguage == "ar" ? "🌐 WAN IP: جاري التحميل..." : "🌐 WAN IP: Loading...";
                WanIpInfo info = await wanIpService.GetAsync(cancellation.Token);
                if (isClosing || generation != wanIpGeneration || cancellation.IsCancellationRequested) return;
                lblWanIP.Text = "🌐 WAN IP: " + info.Ip + " " + BuildLocationInfo(info);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WAN IP lookup failed: " + ex);
                if (!isClosing && generation == wanIpGeneration)
                    lblWanIP.Text = currentLanguage == "ar" ? "🌐 WAN IP: فشل في الجلب" : "🌐 WAN IP: Failed to fetch";
            }
            finally
            {
                if (ReferenceEquals(wanIpCancellation, cancellation)) wanIpCancellation = null;
                cancellation.Dispose();
            }
        }

        private string BuildLocationInfo(WanIpInfo info)
        {
            string country = info.Country;
            string region = info.Region;
            string city = info.City;
            if (currentLanguage == "ar")
            {
                country = TranslateCountryToArabic(country, info.CountryCode);
                region = TranslateRegionToArabic(region);
                city = TranslateCityToArabic(city);
            }

            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country)) return "(" + city + ", " + country + ")";
            if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(country)) return "(" + region + ", " + country + ")";
            return string.IsNullOrEmpty(country) ? string.Empty : "(" + country + ")";
        }

        private void CancelWanIpRequest()
        {
            wanIpGeneration++;
            CancellationTokenSource cancellation = wanIpCancellation;
            if (cancellation != null && !cancellation.IsCancellationRequested)
            {
                try { cancellation.Cancel(); } catch {}
            }
        }

        // ============== ترجمة اسم الدولة إلى العربية ==============
        private string TranslateCountryToArabic(string country, string countryCode)
        {
            var countryCodeTranslations = new Dictionary<string, string>
            {
                {"SA", "السعودية"}, {"AE", "الإمارات"}, {"BH", "البحرين"}, {"KW", "الكويت"},
                {"QA", "قطر"}, {"OM", "عمان"}, {"JO", "الأردن"}, {"LB", "لبنان"},
                {"EG", "مصر"}, {"IQ", "العراق"}, {"SY", "سوريا"}, {"YE", "اليمن"},
                {"PS", "فلسطين"}, {"MA", "المغرب"}, {"DZ", "الجزائر"}, {"TN", "تونس"},
                {"LY", "ليبيا"}, {"SD", "السودان"}, {"US", "أمريكا"}, {"GB", "بريطانيا"},
                {"DE", "ألمانيا"}, {"FR", "فرنسا"}, {"ES", "إسبانيا"}, {"IT", "إيطاليا"},
                {"NL", "هولندا"}, {"CH", "سويسرا"}, {"SE", "السويد"}, {"NO", "النرويج"},
                {"IN", "الهند"}, {"CN", "الصين"}, {"JP", "اليابان"}, {"KR", "كوريا الجنوبية"},
                {"TR", "تركيا"}, {"PK", "باكستان"}, {"BD", "بنغلاديش"}, {"CA", "كندا"},
                {"AU", "أستراليا"}, {"BR", "البرازيل"}, {"MX", "المكسيك"}, {"RU", "روسيا"}
            };

            if (!string.IsNullOrEmpty(countryCode) && countryCodeTranslations.ContainsKey(countryCode.ToUpper()))
            {
                return countryCodeTranslations[countryCode.ToUpper()];
            }

            var countryTranslations = new Dictionary<string, string>
            {
                {"Saudi Arabia", "السعودية"}, {"United Arab Emirates", "الإمارات"}, {"Bahrain", "البحرين"},
                {"Kuwait", "الكويت"}, {"Qatar", "قطر"}, {"Oman", "عمان"}, {"Jordan", "الأردن"},
                {"Lebanon", "لبنان"}, {"Egypt", "مصر"}, {"Iraq", "العراق"}, {"Syria", "سوريا"},
                {"Yemen", "اليمن"}, {"Palestine", "فلسطين"}, {"Morocco", "المغرب"}, {"Algeria", "الجزائر"},
                {"Tunisia", "تونس"}, {"Libya", "ليبيا"}, {"Sudan", "السودان"}, {"United States", "أمريكا"},
                {"United Kingdom", "بريطانيا"}, {"Germany", "ألمانيا"}, {"France", "فرنسا"},
                {"Spain", "إسبانيا"}, {"Italy", "إيطاليا"}, {"Netherlands", "هولندا"},
                {"Switzerland", "سويسرا"}, {"Sweden", "السويد"}, {"Norway", "النرويج"},
                {"India", "الهند"}, {"China", "الصين"}, {"Japan", "اليابان"},
                {"South Korea", "كوريا الجنوبية"}, {"Turkey", "تركيا"}, {"Pakistan", "باكستان"},
                {"Bangladesh", "بنغلاديش"}
            };

            return countryTranslations.ContainsKey(country) ? countryTranslations[country] : country;
        }

        // ============== ترجمة اسم المنطقة إلى العربية ==============
        private string TranslateRegionToArabic(string region)
        {
            var regionTranslations = new Dictionary<string, string>
            {
                {"Riyadh", "الرياض"}, {"Makkah", "مكة المكرمة"}, {"Mecca", "مكة المكرمة"},
                {"Madinah", "المدينة المنورة"}, {"Medina", "المدينة المنورة"},
                {"Eastern Province", "المنطقة الشرقية"}, {"Asir", "عسير"}, {"Tabuk", "تبوك"},
                {"Qassim", "القصيم"}, {"Hail", "حائل"}, {"Jazan", "جازان"}, {"Najran", "نجران"},
                {"Al Bahah", "الباحة"}, {"Northern Borders", "الحدود الشمالية"}, {"Al Jawf", "الجوف"},
                {"Dubai", "دبي"}, {"Abu Dhabi", "أبو ظبي"}, {"Sharjah", "الشارقة"}, {"Ajman", "عجمان"},
                {"Ras Al Khaimah", "رأس الخيمة"}, {"Fujairah", "الفجيرة"}, {"Umm Al Quwain", "أم القيوين"},
                {"Capital Governorate", "محافظة العاصمة"}, {"Muharraq Governorate", "محافظة المحرق"},
                {"Northern Governorate", "المحافظة الشمالية"}, {"Southern Governorate", "المحافظة الجنوبية"}
            };

            return regionTranslations.ContainsKey(region) ? regionTranslations[region] : region;
        }

        // ============== ترجمة اسم المدينة إلى العربية ==============
        private string TranslateCityToArabic(string city)
        {
            var cityTranslations = new Dictionary<string, string>
            {
                {"Riyadh", "الرياض"}, {"Jeddah", "جدة"}, {"Mecca", "مكة"}, {"Medina", "المدينة"},
                {"Dammam", "الدمام"}, {"Khobar", "الخبر"}, {"Dhahran", "الظهران"}, {"Jubail", "الجبيل"},
                {"Hofuf", "الهفوف"}, {"Tabuk", "تبوك"}, {"Abha", "أبها"}, {"Khamis Mushait", "خميس مشيط"},
                {"Najran", "نجران"}, {"Jazan", "جازان"}, {"Hail", "حائل"}, {"Buraydah", "بريدة"},
                {"Unaizah", "عنيزة"}, {"Yanbu", "ينبع"}, {"Taif", "الطائف"}, {"Dubai", "دبي"},
                {"Abu Dhabi", "أبو ظبي"}, {"Sharjah", "الشارقة"}, {"Manama", "المنامة"},
                {"Muharraq", "المحرق"}, {"Riffa", "الرفاع"}, {"Doha", "الدوحة"},
                {"Kuwait City", "مدينة الكويت"}, {"Muscat", "مسقط"}, {"Amman", "عمّان"},
                {"Beirut", "بيروت"}, {"Cairo", "القاهرة"}, {"Baghdad", "بغداد"}, {"Damascus", "دمشق"},
                {"Al-Khobar", "الخبر"}, {"Qassim", "القصيم"}, {"Bahrah", "بحرة"}, {"Obhur", "أبحر"},
                {"Alhamra'a", "الحمراء"}, {"Abyar 'Ali", "أبيار علي"}, {"Al Mashair", "المشاعر"},
                {"Tubli", "توبلي"}
            };

            return cityTranslations.ContainsKey(city) ? cityTranslations[city] : city;
        }

        // ============== تحديث نص زر إظهار/إخفاء IP حسب اللغة والحالة ==============
        private void UpdateToggleIPButtonText()
        {
            btnToggleIP.Text = isIPVisible 
                ? (currentLanguage == "ar" ? "🙈 إخفاء IP" : "🙈 Hide IP")
                : (currentLanguage == "ar" ? "👁️ إظهار IP" : "👁️ Show IP");
        }

        // ============== حدث النقر على btnToggleIP ==============
        private void btnToggleIP_Click(object sender, EventArgs e)
        {
            isIPVisible = !isIPVisible;
            lblWanIP.Visible = isIPVisible;
            UpdateToggleIPButtonText();
            if (isIPVisible) ShowWANIP();
            else CancelWanIpRequest();
        }

        // ============== حدث النقر على btnToggleTimer ==============
        private void btnToggleTimer_Click(object sender, EventArgs e)
        {
            if (updating)
            {
                updating = false;
                pingTimer.Stop();
                CancelCurrentScan();
                btnToggleTimer.Text = currentLanguage == "ar" ? "▶️ تشغيل" : "▶️ Start";
            }
            else
            {
                updating = true;
                RequestImmediateScan();
                btnToggleTimer.Text = currentLanguage == "ar" ? "⏸ إيقاف" : "⏸ Pause";
            }
            UpdateScanStatus();
        }

        // ============== حدث النقر على btnChangeInterval ==============
        private void btnChangeInterval_Click(object sender, EventArgs e)
        {
            string prompt = currentLanguage == "ar" ? "الفاصل بين بدايات القياس بالثواني:" : "Seconds between scan starts:";
            string title = currentLanguage == "ar" ? "تغيير الفترة" : "Change Interval";

            using (var inputForm = new Form())
            {
                inputForm.Text = title;
                inputForm.ClientSize = new Size(350, 145);
                inputForm.BackColor = Color.FromArgb(25, 25, 25);
                inputForm.ForeColor = Color.White;
                inputForm.Font = Font;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var label = new Label() { Left = 20, Top = 20, Text = prompt, AutoSize = true };
                var textBox = new TextBox() { Left = 20, Top = 50, Width = 290, Text = (updateInterval / 1000).ToString() };
                var okButton = new ModernButton() { Text = "OK", Left = 150, Width = 75, Top = 80, DialogResult = DialogResult.OK };
                var cancelButton = new ModernButton() { Text = currentLanguage == "ar" ? "إلغاء" : "Cancel", Left = 235, Width = 75, Top = 80, DialogResult = DialogResult.Cancel };

                okButton.Click += (s, ev) => { inputForm.Close(); };
                cancelButton.Click += (s, ev) => { inputForm.Close(); };

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;
                UiEffects.PrepareFadeIn(inputForm);
                UiEffects.AttachFocusBorder(textBox);
                textBox.BackColor = Color.FromArgb(40, 40, 43);
                textBox.ForeColor = Color.White;
                okButton.Text = currentLanguage == "ar" ? "موافق" : "OK";
                foreach (Button button in new[] { okButton, cancelButton }) { button.FlatStyle = FlatStyle.Flat; button.BackColor = Color.FromArgb(60, 65, 75); button.ForeColor = Color.White; ((ModernButton)button).GradientColor1 = button.BackColor; ((ModernButton)button).GradientColor2 = Color.FromArgb(75, 80, 90); }

                if (inputForm.ShowDialog(this) == DialogResult.OK)
                {
                    int secs;
                    if (int.TryParse(textBox.Text, out secs) && secs >= 1 && secs <= 86400)
                    {
                        updateInterval = checked(secs * 1000);
                        pingTimer.Interval = updateInterval;
                        SaveSettings();
                    }
                    else
                    {
                        string msg = currentLanguage == "ar" ? "أدخل قيمة بين ثانية واحدة و86400 ثانية." : "Enter a value between 1 and 86400 seconds.";
                        string cap = currentLanguage == "ar" ? "خطأ" : "Error";
                        MessageBox.Show(msg, cap, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // ============== حدث المؤقّت PingTimer_Tick لتحديث البنق بشكل غير متزامن ==============
        private async void PingTimer_Tick(object sender, EventArgs e)
        {
            await RunPingCycleAsync();
        }

        private async Task RunPingCycleAsync()
        {
            if (isClosing || !updating || scanInProgress) return;
            scanInProgress = true;
            pingTimer.Stop();
            int generation = ++scanGeneration;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            scanCancellation = cancellation;

            Stopwatch cycleClock = Stopwatch.StartNew();
            List<PingTarget> targets = GetMonitoredServers()
                .Select(server => new PingTarget { ServerId = server.Id, Host = server.Host }).ToList();
            scanCompleted = 0;
            scanTotal = targets.Count;
            UpdateScanStatus();

            try
            {
                using (SemaphoreSlim gate = new SemaphoreSlim(8, 8))
                {
                    await Task.WhenAll(targets.Select(target => MeasureAndDisplayAsync(target, gate, cancellation.Token, generation)));
                }
                if (isClosing || cancellation.IsCancellationRequested || generation != scanGeneration) return;
                RefreshMeasuredRows();
                ApplySort();
                UpdateStatusBar();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ping cycle failed: " + ex);
            }
            finally
            {
                if (ReferenceEquals(scanCancellation, cancellation)) scanCancellation = null;
                cancellation.Dispose();
                scanInProgress = false;
                UpdateScanStatus();
                if (!isClosing && updating)
                {
                    pingTimer.Interval = immediateScanRequested ? 1 : Math.Max(100, updateInterval - (int)Math.Min(int.MaxValue, cycleClock.ElapsedMilliseconds));
                    immediateScanRequested = false;
                    pingTimer.Start();
                }
            }
        }

        private static Task<PingMeasurement> MeasureServerAsync(PingTarget target, SemaphoreSlim gate, CancellationToken cancellationToken)
        {
            return PingProbe.MeasureAsync(target, gate, cancellationToken);
        }

        private void CancelCurrentScan()
        {
            scanGeneration++;
            CancellationTokenSource cancellation = scanCancellation;
            if (cancellation != null && !cancellation.IsCancellationRequested)
            {
                try { cancellation.Cancel(); } catch {}
            }
        }

        private void ApplyMetricsToItem(ListViewItem item, ServerMetrics metrics, bool animate = false)
        {
            item.UseItemStyleForSubItems = false;
            if (metrics.IsOnline)
            {
                item.SubItems[4].Text = metrics.PingMilliseconds + " ms";
                item.SubItems[5].Text = metrics.JitterMilliseconds.ToString("F1") + " ms";
                item.SubItems[4].ForeColor = GetPingColor(metrics.PingMilliseconds);
                item.SubItems[5].ForeColor = GetJitterColor(metrics.JitterMilliseconds);
            }
            else
            {
                item.SubItems[4].Text = metrics.IsMeasurementError ? (currentLanguage == "ar" ? "خطأ" : "Error") : "—";
                item.SubItems[5].Text = "—";
                item.SubItems[4].ForeColor = Color.Gray;
                item.SubItems[5].ForeColor = Color.Gray;
            }
            item.SubItems[6].Text = metrics.SampleCount == 0 ? "—" : metrics.LossPercent.ToString("F1") + " %";
            item.ToolTipText = BuildMetricHint(metrics);
            item.SubItems[6].ForeColor = GetLossColor(metrics.LossPercent);

            if (animate)
                BeginMetricPulse(item);
            else
                SetMetricBackground(item, 0f);
        }

        private void BeginMetricPulse(ListViewItem item)
        {
            ServerRowTag tag = item == null ? null : item.Tag as ServerRowTag;
            if (tag == null || metricPulseTimer == null || !UiEffects.CanAnimate(serverListView)) return;

            metricPulseStrengths[tag.ServerId] = 1f;
            metricPulseStartedAt[tag.ServerId] = metricPulseClock.ElapsedMilliseconds;
            SetMetricBackground(item, 1f);
            if (!metricPulseTimer.Enabled) metricPulseTimer.Start();
        }

        private void MetricPulseTimer_Tick(object sender, EventArgs e)
        {
            if (!UiEffects.CanAnimate(serverListView)) metricPulseStrengths.Clear();
            foreach (string serverId in metricPulseStrengths.Keys.ToArray())
            {
                float nextStrength = Math.Max(0, 1 - (metricPulseClock.ElapsedMilliseconds - metricPulseStartedAt[serverId]) / 550f);
                if (nextStrength <= 0f)
                {
                    metricPulseStrengths.Remove(serverId);
                    metricPulseStartedAt.Remove(serverId);
                }
                else
                    metricPulseStrengths[serverId] = nextStrength;
            }

            foreach (ListViewItem item in serverListView.Items)
            {
                ServerRowTag tag = item.Tag as ServerRowTag;
                float strength;
                SetMetricBackground(
                    item,
                    tag != null && metricPulseStrengths.TryGetValue(tag.ServerId, out strength) ? strength * strength : 0f);
            }

            serverListView.Invalidate();
            if (metricPulseStrengths.Count == 0) { metricPulseTimer.Stop(); metricPulseStartedAt.Clear(); }
        }

        private void SetMetricBackground(ListViewItem item, float strength)
        {
            if (item == null || item.SubItems.Count < 7) return;
            Color background = UiEffects.Blend(
                serverListView.BackColor,
                Color.FromArgb(44, 76, 96),
                strength * 0.72f);
            item.SubItems[4].BackColor = background;
            item.SubItems[5].BackColor = background;
            item.SubItems[6].BackColor = background;
        }

        private static Color GetPingColor(long value)
        {
            if (value <= 20) return Color.FromArgb(50, 205, 50);
            if (value <= 50) return Color.FromArgb(34, 139, 34);
            if (value <= 80) return Color.FromArgb(154, 205, 50);
            if (value <= 120) return Color.FromArgb(255, 215, 0);
            if (value <= 180) return Color.FromArgb(255, 140, 0);
            if (value <= 250) return Color.FromArgb(255, 69, 0);
            return Color.FromArgb(220, 20, 60);
        }

        private static Color GetJitterColor(double value)
        {
            if (value <= 5) return Color.FromArgb(50, 205, 50);
            if (value <= 15) return Color.FromArgb(34, 139, 34);
            if (value <= 30) return Color.FromArgb(154, 205, 50);
            if (value <= 50) return Color.FromArgb(255, 215, 0);
            if (value <= 80) return Color.FromArgb(255, 140, 0);
            return Color.FromArgb(220, 20, 60);
        }

        private static Color GetLossColor(double value)
        {
            if (value <= 0) return Color.FromArgb(50, 205, 50);
            if (value <= 1) return Color.FromArgb(34, 139, 34);
            if (value <= 3) return Color.FromArgb(255, 215, 0);
            if (value <= 7) return Color.FromArgb(255, 140, 0);
            if (value <= 15) return Color.FromArgb(255, 69, 0);
            return Color.FromArgb(220, 20, 60);
        }

        // ============== تحديث شريط الحالة ==============
        private void UpdateStatusBar()
        {
            int totalServers = serverListView.Items.Count;
            activeServerCount = 0;
            offlineServerCount = 0;
            foreach (ListViewItem item in serverListView.Items)
            {
                ServerRowTag tag = item.Tag as ServerRowTag;
                ServerMetrics metrics;
                if (tag == null || !serverMetrics.TryGetValue(tag.ServerId, out metrics) || !metrics.HasResult) continue;
                if (metrics.IsOnline) activeServerCount++;
                else offlineServerCount++;
            }

            if (currentLanguage == "ar")
            {
                lblServerCount.Text = "📊 الخوادم: " + totalServers.ToString();
                lblActiveServers.Text = "✅ استجاب: " + activeServerCount.ToString();
                lblOfflineServers.Text = "⚠ لم يستجب/خطأ: " + offlineServerCount.ToString();
            }
            else
            {
                lblServerCount.Text = "📊 Servers: " + totalServers.ToString();
                lblActiveServers.Text = "✅ Replied: " + activeServerCount.ToString();
                lblOfflineServers.Text = "⚠ No reply/error: " + offlineServerCount.ToString();
            }
        }

        // ============== حدث فرز الأعمدة عند النقر على رأس العمود ==============
        private void ServerListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            sortDescending = sortColumn == e.Column && !sortDescending;
            sortColumn = e.Column;
            ApplySort();
            SaveMonitorState();
        }

        // ============== رسم رؤوس الأعمدة ==============
        private void ServerListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawBackground();
            TextRenderer.DrawText(e.Graphics, e.Header.Text, serverListView.Font, e.Bounds, SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        // ============== رسم العناصر الفرعية مع الألوان الخلفية ==============
        private void ServerListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            Color background = serverListView.GetRowBackground(e.Item, e.ColumnIndex >= 4 ? e.SubItem.BackColor : serverListView.BackColor);
            using (SolidBrush fill = new SolidBrush(background)) e.Graphics.FillRectangle(fill, e.Bounds);
            if (e.ColumnIndex == 0)
            {
                if (imageListFlags.Images.ContainsKey(e.Item.ImageKey))
                {
                    Image flag = imageListFlags.Images[e.Item.ImageKey];
                    int size = Math.Min(e.Bounds.Height - 4, 24);
                    e.Graphics.DrawImage(flag, e.Bounds.Left + (e.Bounds.Width - size) / 2, e.Bounds.Top + (e.Bounds.Height - size) / 2, size, size);
                }
                if (e.Item.Selected)
                    using (SolidBrush accent = new SolidBrush(Color.FromArgb(95, 205, 250)))
                        e.Graphics.FillRectangle(accent, e.Bounds.Left, e.Bounds.Top + 2, 3, Math.Max(1, e.Bounds.Height - 4));
            }
            else
            {
                Rectangle text = Rectangle.Inflate(e.Bounds, -5, 0);
                Color color = SystemInformation.HighContrast ? (e.Item.Selected ? SystemColors.HighlightText : SystemColors.WindowText) : e.SubItem.ForeColor;
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.SubItem.Font, text, color,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix |
                    (e.ColumnIndex >= 4 ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left));
            }
            using (Pen grid = new Pen(Color.FromArgb(52, 55, 60)))
            {
                e.Graphics.DrawLine(grid, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
                e.Graphics.DrawLine(grid, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }

        // ============== حدث تبديل اللغة ==============
        private void btnToggleLang_Click(object sender, EventArgs e)
        {
            pingTimer.Stop();
            CancelCurrentScan();

            if (currentLanguage == "ar")
            {
                currentLanguage = "en";
                btnToggleLang.Text = "🌐 ar";
                btnToggleTimer.Text = updating ? "⏸ Pause" : "▶️ Start";
                btnChangeInterval.Text = "⏱ Change Interval";
                btnToggleIP.Text = isIPVisible ? "🙈 Hide IP" : "👁️ Show IP";
                btnManageMonitorLists.Text = "⚙ Manage Lists";
            }
            else
            {
                currentLanguage = "ar";
                btnToggleLang.Text = "🌐 en";
                btnToggleTimer.Text = updating ? "⏸ إيقاف" : "▶️ تشغيل";
                btnChangeInterval.Text = "⏱ تغيير الفترة";
                btnToggleIP.Text = isIPVisible ? "🙈 إخفاء IP" : "👁️ إظهار IP";
                btnManageMonitorLists.Text = "⚙ إدارة القوائم";
            }

            CaptureColumnWidths();
            SetupListViewColumns();
            RestoreColumnWidths();
            UpdateLanguageLabels();
            PopulateProviderCheckboxes();
            PopulateMonitorListsCombo();
            PopulateListView();
            UpdateStatusBar();
            if (isIPVisible) ShowWANIP();
            SaveMonitorState();
            if (updating) RequestImmediateScan();
        }

    }
}
