using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
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

        private Dictionary<string, List<long>> lastPingTimes = new Dictionary<string, List<long>>();
        private Dictionary<string, int> lostCount = new Dictionary<string, int>();
        private Dictionary<string, int> totalCount = new Dictionary<string, int>();

        // متغيرات لحساب إحصائيات الخوادم
        private int activeServerCount = 0;
        private int offlineServerCount = 0;

        // خريطة أسماء المزودين إلى العربية
        private Dictionary<string, string> providerMapAr = new Dictionary<string, string>
        {
            {"Amazon", "أمازون"},
            {"Google Cloud ", "جوجل السحابية"},
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

        // قائمة الخوادم
        private List<Tuple<string, string, string, string>> servers = new List<Tuple<string, string, string, string>>();

        // أسماء المزودين بالترتيب للّوحة الأفقية
        private string[] providersAr = { "أمازون", "جوجل السحابية", "شرق الأوسط", "الاتصالات السعودية", "موبايلي", "زين", "سلام", "قو اتصالات" };
        private string[] providersEn = { "Amazon", "google cloud", "Middle East", "STC", "Mobily", "Zain", "Salam", "GO Telecom" };

        // ============== Constructor ==============
        public Form1()
        {
            InitializeComponent();

            // تحميل الإعدادات المحفوظة
            LoadSettings();

            // ضبط اللغة الافتراضية إلى العربية
            currentLanguage = "ar";

            btnToggleIP.GradientColor1 = Color.FromArgb(0, 180, 240);
            btnToggleIP.GradientColor2 = Color.FromArgb(0, 140, 200);
            btnToggleTimer.GradientColor1 = Color.FromArgb(40, 167, 69);
            btnToggleTimer.GradientColor2 = Color.FromArgb(60, 200, 100);
            btnChangeInterval.GradientColor1 = Color.FromArgb(255, 152, 0);
            btnChangeInterval.GradientColor2 = Color.FromArgb(255, 180, 50);
            btnToggleLang.GradientColor1 = Color.FromArgb(156, 39, 176);
            btnToggleLang.GradientColor2 = Color.FromArgb(190, 80, 210);

            // تحميل صور الأعلام من الموارد
            LoadFlagImages_FromResources();

            // إعداد أعمدة الـ ListView بناءً على اللغة
            SetupListViewColumns();

            // تهيئة قائمة الخوادم
            InitializeServersList();

            // تفعيل الرسم اليدوي للألوان
            serverListView.OwnerDraw = true;
            serverListView.DrawColumnHeader += ServerListView_DrawColumnHeader;
            serverListView.DrawSubItem += ServerListView_DrawSubItem;

            // ربط حدث الـ PingTimer للتحديث الدوري
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Interval = updateInterval;
            pingTimer.Start();
            updating = true;

            // ضبط نصوص الأزرار بالوضع العربي
            btnToggleTimer.Text = "⏸ إيقاف";
            btnChangeInterval.Text = "⏱ تغيير الفترة";
            btnToggleIP.Text = "👁️ إظهار IP";
            btnToggleLang.Text = "🌐 en";

            // تعبئة FlowLayoutPanel بخانات اختيار المزودين (أفقياً)
            PopulateProviderCheckboxes();

            // استعادة حالة المزودين المحفوظة
            RestoreProviderCheckboxStates();

            // ملء الـ ListView للمرة الأولى
            PopulateListView();

            // ربط فرز الأعمدة عند النقر على رأس العمود
            serverListView.ColumnClick += ServerListView_ColumnClick;

            // تحديث شريط الحالة
            UpdateStatusBar();
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
            try { pingTimer.Tick -= PingTimer_Tick; } catch {}
            try { pingTimer.Stop(); } catch {}
            try { pingTimer.Dispose(); } catch {}
            SaveSettings();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            try { Application.ExitThread(); } catch {}
            try { Environment.Exit(0); } catch {}
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
                            selectedProviders.Add(cb.Text);
                        }
                    }
                }
                Properties.Settings.Default.SelectedProviders = string.Join(",", selectedProviders);

                // حفظ الإعدادات
                Properties.Settings.Default.Save();
            }
            catch
            {
                // تجاهل أي أخطاء في الحفظ
            }
        }

        // ============== تحميل الإعدادات ==============
        private void LoadSettings()
        {
            try
            {
                // تحميل الفترة الزمنية
                if (Properties.Settings.Default.UpdateInterval > 0)
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
                    foreach (Control ctrl in pnlProviders.Controls)
                    {
                        if (ctrl is CheckBox)
                        {
                            CheckBox cb = (CheckBox)ctrl;
                            cb.Checked = selectedProviders.Contains(cb.Text);
                        }
                    }
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
            pnlProviders.Controls.Clear();
            string[] listToUse = currentLanguage == "ar" ? providersAr : providersEn;

            foreach (string txt in listToUse)
            {
                CheckBox cb = new CheckBox
                {
                    Text = txt,
                    AutoSize = true,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Margin = new Padding(8, 5, 8, 5),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                cb.CheckedChanged += ProviderCheckBox_CheckedChanged;
                pnlProviders.Controls.Add(cb);
            }
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
                serverListView.Columns.Add("الجيتار", 100, HorizontalAlignment.Center);
                serverListView.Columns.Add("فقد البيانات", 100, HorizontalAlignment.Center);
            }
            else
            {
                serverListView.Columns.Add("Flag", 40, HorizontalAlignment.Center);
                serverListView.Columns.Add("Country", 120, HorizontalAlignment.Left);
                serverListView.Columns.Add("Location", 140, HorizontalAlignment.Left);
                serverListView.Columns.Add("ISP", 150, HorizontalAlignment.Left);
                serverListView.Columns.Add("Ping (ms)", 80, HorizontalAlignment.Center);
                serverListView.Columns.Add("Jitter (ms)", 100, HorizontalAlignment.Center);
                serverListView.Columns.Add("Loss (%)", 100, HorizontalAlignment.Center);
            }
        }

        // ============== تهيئة قائمة الخوادم ==============
        private void InitializeServersList()
        {
            servers.Clear();

            // STC السعودية
            servers.Add(Tuple.Create("sa", "الرياض", "STC", "speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "جدة", "STC", "jed-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "المدينة المنورة", "STC", "ab-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "تبوك", "STC", "tabuk-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "جازان", "STC", "jizan-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "نجران", "STC", "najran-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "الدمام", "STC", "dam-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "مكة", "STC", "makkah-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "ينبع", "STC", "yanbu-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "الحمراء", "STC", "alhamraa-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "أبحر", "STC", "obhur-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "بحرة", "STC", "bahrah-speedtest.saudi.net.sa"));
            servers.Add(Tuple.Create("sa", "أبيار علي", "STC", "abyarali-speedtest.saudi.net.sa"));

            // Zain السعودية
            servers.Add(Tuple.Create("sa", "الرياض", "Zain", "speedtest-riyadhnew.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "جدة", "Zain", "speedtest-jeddahnew.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "المدينة المنورة", "Zain", "speedtest-medina.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "تبوك", "Zain", "speedtest-tabuk.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "القصيم", "Zain", "speedtest-qassim.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "خميس مشيط", "Zain", "speedtest-khamismushait.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "الهفوف", "Zain", "speedtest-hofuf.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "الدمام", "Zain", "speedtest-dammamnew.sa.zain.com.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "مكة", "Zain", "speedtest-makkah.sa.zain.com.prod.hosts.ooklaserver.net"));

            // Mobily السعودية
            servers.Add(Tuple.Create("sa", "الرياض", "Mobily", "ryd.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "جدة", "Mobily", "jed.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "المدينة المنورة", "Mobily", "mdn.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "تبوك", "Mobily", "tbk.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "جازان", "Mobily", "jzn.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "نجران", "Mobily", "njr.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "الدمام", "Mobily", "dam.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "ينبع", "Mobily", "ynb.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "بحرة", "Mobily", "bhr.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "المشاعر", "Mobily", "msr.myspeed.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "مكة", "Mobily", "mkh.myspeed.net.sa.prod.hosts.ooklaserver.net"));

            // Salam السعودية
            servers.Add(Tuple.Create("sa", "الرياض", "Salam", "ftthspeed-ruh.salam.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "جدة", "Salam", "jed-speed.itc.net.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "الخبر", "Salam", "speedtest-kbr.salam.sa.prod.hosts.ooklaserver.net"));

            // GO Telecom السعودية
            servers.Add(Tuple.Create("sa", "الرياض", "GO Telecom", "speedtest.go.com.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("sa", "جدة", "GO Telecom", "hispeedtest.go.com.sa.prod.hosts.ooklaserver.net"));

            // Etisalat الإمارات
            servers.Add(Tuple.Create("ae", "أبو ظبي", "Etisalat", "speedtest2.etisalat.ae"));
            servers.Add(Tuple.Create("ae", "دبي", "Etisalat", "speedtest1.etisalat.ae"));

            // du الإمارات
            servers.Add(Tuple.Create("ae", "أبو ظبي", "du", "auh.speedtest.du.ae"));
            servers.Add(Tuple.Create("ae", "دبي", "du", "dxbsouth.speedtest.du.ae.prod.hosts.ooklaserver.net"));

            // STC البحرين
            servers.Add(Tuple.Create("bh", "الرفاع", "STC Bahrain", "speedtest4.stc.com.bh.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("bh", "سيف", "STC Bahrain", "speedtest3.stc.com.bh.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("bh", "المنامة", "STC Bahrain", "speedtest2.stc.com.bh.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("bh", "المحرق", "STC Bahrain", "speedtest1.stc.com.bh.prod.hosts.ooklaserver.net"));

            // Zain البحرين
            servers.Add(Tuple.Create("bh", "توبلي", "Zain Bahrain", "staging.bh.zain.com"));
            servers.Add(Tuple.Create("bh", "المنامة", "Zain Bahrain", "stest.bh.zain.com"));

            // Etisalcom البحرين
            servers.Add(Tuple.Create("bh", "المنامة", "Etisalcom", "sp1.etisalcom.com.prod.hosts.ooklaserver.net"));

            // Amazon العالمية
            servers.Add(Tuple.Create("i_n", "مومباي", "Amazon", "dynamodb.ap-south-1.amazonaws.com"));
            servers.Add(Tuple.Create("de", "فرانكفورت", "Amazon", "dynamodb.eu-central-1.amazonaws.com"));
            servers.Add(Tuple.Create("fr", "باريس", "Amazon", "dynamodb.eu-west-3.amazonaws.com"));
            servers.Add(Tuple.Create("gb", "لندن", "Amazon", "dynamodb.eu-west-2.amazonaws.com"));
            servers.Add(Tuple.Create("es", "إسپانيا", "Amazon", "dynamodb.eu-south-2.amazonaws.com"));
            servers.Add(Tuple.Create("ie", "أيرلندا", "Amazon", "dynamodb.eu-west-1.amazonaws.com"));
            servers.Add(Tuple.Create("it", "ميلانو", "Amazon", "dynamodb.eu-south-1.amazonaws.com"));
            servers.Add(Tuple.Create("ch", "زيورخ", "Amazon", "dynamodb.eu-central-2.amazonaws.com"));
            servers.Add(Tuple.Create("se", "ستوكهولم", "Amazon", "dynamodb.eu-north-1.amazonaws.com"));

            // Google Cloud
            servers.Add(Tuple.Create("sa", "الدمام", "Google Cloud ", "ftthspeed-ruh.salam.sa.prod.hosts.ooklaserver.net"));
            servers.Add(Tuple.Create("bh", "المنامة", "Google Cloud ", "dynamodb.me-south-1.amazonaws.com"));
            servers.Add(Tuple.Create("de", "فرانكفورت", "Google Cloud ", "dynamodb.eu-central-1.amazonaws.com"));
            servers.Add(Tuple.Create("fr", "باريس", "Google Cloud ", "dynamodb.eu-west-3.amazonaws.com"));
            servers.Add(Tuple.Create("gb", "لندن", "Google Cloud ", "dynamodb.eu-west-2.amazonaws.com"));
            servers.Add(Tuple.Create("es", "إسپانيا", "Google Cloud ", "dynamodb.eu-south-2.amazonaws.com"));
        }

        // ============== إعادة ملء الـ ListView بناءً على المزودين المحددين واللغة ==============
        private void PopulateListView()
        {
            serverListView.Items.Clear();

            // اجمع أسماء المزودين الذين وُضع عليهم علامة في خانات الاختيار
            List<string> checkedProviders = new List<string>();
            foreach (Control ctrl in pnlProviders.Controls)
            {
                if (ctrl is CheckBox)
                {
                    CheckBox cb = (CheckBox)ctrl;
                    if (cb.Checked)
                    {
                        checkedProviders.Add(cb.Text);
                    }
                }
            }

            // إذا لم يختَر المستخدم شيئاً → عرض كل الخوادم
            bool filterAll = checkedProviders.Count == 0;

            foreach (var srv in servers)
            {
                string countryCode = srv.Item1.ToLower();
                string locAr = srv.Item2;
                string providerNameEn = srv.Item3;
                string host = srv.Item4;

                // أوجد النص المعروض للمزود بناءً على اللغة
                string providerDisplay;
                if (currentLanguage == "ar")
                {
                    providerDisplay = providerMapAr.ContainsKey(providerNameEn) ? providerMapAr[providerNameEn] : providerNameEn;
                }
                else
                {
                    providerDisplay = providerNameEn;
                }

                // معالجة فلتر "شرق الأوسط" بشكل خاص
                bool isMiddleEastServer = false;
                if (countryCode == "bh")
                {
                    isMiddleEastServer = true;
                }
                else if (countryCode == "ae" && providerNameEn != "Amazon")
                {
                    isMiddleEastServer = true;
                }

                // نحدد ما إذا سنعرض هذا السطر
                bool showItem = false;

                if (filterAll)
                {
                    showItem = true;
                }
                else
                {
                    foreach (string sel in checkedProviders)
                    {
                        if (currentLanguage == "ar")
                        {
                            if (sel == "شرق الأوسط" && isMiddleEastServer)
                            {
                                showItem = true;
                                break;
                            }
                        }
                        else
                        {
                            if (sel == "Middle East" && isMiddleEastServer)
                            {
                                showItem = true;
                                break;
                            }
                        }

                        if (sel == providerDisplay)
                        {
                            showItem = true;
                            break;
                        }
                    }
                }

                if (showItem)
                {
                    ListViewItem item = new ListViewItem
                    {
                        ImageKey = countryCode
                    };

                    // العمود الثاني: اسم الدولة حسب اللغة
                    if (currentLanguage == "ar")
                    {
                        item.SubItems.Add(countryMapAr.ContainsKey(countryCode) ? countryMapAr[countryCode] : countryCode);
                    }
                    else
                    {
                        item.SubItems.Add(countryMapEn.ContainsKey(countryCode) ? countryMapEn[countryCode] : countryCode);
                    }

                    // العمود الثالث: اسم المدينة/الموقع حسب اللغة
                    if (currentLanguage == "ar")
                    {
                        item.SubItems.Add(locAr);
                    }
                    else
                    {
                        item.SubItems.Add(locationMapEn.ContainsKey(locAr) ? locationMapEn[locAr] : locAr);
                    }

                    // العمود الرابع: اسم المزود
                    item.SubItems.Add(providerDisplay);

                    // الأعمدة الثلاثة للبنق / الجيتار / فقد البيانات
                    item.SubItems.Add("-");
                    item.SubItems.Add("-");
                    item.SubItems.Add("-");
                    item.Tag = host;

                    serverListView.Items.Add(item);
                }
            }
        }

        // ============== حدث تغيير حالة أي CheckBox في FlowLayoutPanel ==============
        private void ProviderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            PopulateListView();
            SaveSettings();
        }

        // ============== جلب WAN IP بطريقة غير متزامنة ==============
        private async void ShowWANIP()
        {
            try
            {
                lblWanIP.Text = currentLanguage == "ar" ? "🌐 WAN IP: جاري التحميل..." : "🌐 WAN IP: Loading...";

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string ip = await client.GetStringAsync("https://api.ipify.org");
                    ip = ip.Trim();

                    // جلب معلومات الموقع الجغرافي للـ IP
                    string locationInfo = await GetIPLocationInfo(ip);

                    lblWanIP.Text = "🌐 WAN IP: " + ip + " " + locationInfo;
                }
            }
            catch
            {
                lblWanIP.Text = currentLanguage == "ar" ? "🌐 WAN IP: فشل في الجلب" : "🌐 WAN IP: Failed to fetch";
            }
        }

        // ============== جلب معلومات الموقع الجغرافي للـ IP ==============
        private async Task<string> GetIPLocationInfo(string ip)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string response = await client.GetStringAsync("http://ip-api.com/json/" + ip + "?fields=status,country,countryCode,regionName,city");

                    // تحليل الاستجابة JSON (للتبسيط نستخدم تقسيم النص)
                    string[] parts = response.Split(new char[] { ':', ',', '{', '}', '"' }, StringSplitOptions.RemoveEmptyEntries);

                    string countryCode = "";
                    string country = "";
                    string region = "";
                    string city = "";

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        string key = parts[i].Trim();
                        if (key == "countryCode")
                        {
                            countryCode = parts[i + 1].Trim();
                        }
                        else if (key == "country")
                        {
                            country = parts[i + 1].Trim();
                        }
                        else if (key == "regionName")
                        {
                            region = parts[i + 1].Trim();
                        }
                        else if (key == "city")
                        {
                            city = parts[i + 1].Trim();
                        }
                    }

                    // ترجمة الأسماء إلى العربية إذا كانت اللغة عربية
                    if (currentLanguage == "ar")
                    {
                        country = TranslateCountryToArabic(country, countryCode);
                        region = TranslateRegionToArabic(region);
                        city = TranslateCityToArabic(city);
                    }

                    // بناء النص النهائي
                    string result = "";
                    if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(country))
                    {
                        result = "(" + city + ", " + country + ")";
                    }
                    else if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(country))
                    {
                        result = "(" + region + ", " + country + ")";
                    }
                    else if (!string.IsNullOrEmpty(country))
                    {
                        result = "(" + country + ")";
                    }

                    return result;
                }
            }
            catch
            {
                return "";
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
        }

        // ============== حدث النقر على btnToggleTimer ==============
        private void btnToggleTimer_Click(object sender, EventArgs e)
        {
            if (updating)
            {
                pingTimer.Stop();
                btnToggleTimer.Text = currentLanguage == "ar" ? "▶️ تشغيل" : "▶️ Start";
                updating = false;
            }
            else
            {
                pingTimer.Start();
                btnToggleTimer.Text = currentLanguage == "ar" ? "⏸ إيقاف" : "⏸ Pause";
                updating = true;
            }
        }

        // ============== حدث النقر على btnChangeInterval ==============
        private void btnChangeInterval_Click(object sender, EventArgs e)
        {
            string prompt = currentLanguage == "ar" ? "أدخل وقت التحديث بالثواني:" : "Enter update interval in seconds:";
            string title = currentLanguage == "ar" ? "تغيير الفترة" : "Change Interval";

            using (var inputForm = new Form())
            {
                inputForm.Text = title;
                inputForm.Width = 350;
                inputForm.Height = 150;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var label = new Label() { Left = 20, Top = 20, Text = prompt, AutoSize = true };
                var textBox = new TextBox() { Left = 20, Top = 50, Width = 290, Text = (updateInterval / 1000).ToString() };
                var okButton = new Button() { Text = "OK", Left = 150, Width = 75, Top = 80, DialogResult = DialogResult.OK };
                var cancelButton = new Button() { Text = currentLanguage == "ar" ? "إلغاء" : "Cancel", Left = 235, Width = 75, Top = 80, DialogResult = DialogResult.Cancel };

                okButton.Click += (s, ev) => { inputForm.Close(); };
                cancelButton.Click += (s, ev) => { inputForm.Close(); };

                inputForm.Controls.Add(label);
                inputForm.Controls.Add(textBox);
                inputForm.Controls.Add(okButton);
                inputForm.Controls.Add(cancelButton);
                inputForm.AcceptButton = okButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    int secs;
                    if (int.TryParse(textBox.Text, out secs) && secs > 0)
                    {
                        updateInterval = secs * 1000;
                        pingTimer.Interval = updateInterval;
                        SaveSettings();
                    }
                    else
                    {
                        string msg = currentLanguage == "ar" ? "الرجاء إدخال قيمة صحيحة بالثواني." : "Please enter a valid number in seconds.";
                        string cap = currentLanguage == "ar" ? "خطأ" : "Error";
                        MessageBox.Show(msg, cap, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        // ============== حدث المؤقّت PingTimer_Tick لتحديث البنق بشكل غير متزامن ==============
        private async void PingTimer_Tick(object sender, EventArgs e)
        {
            if (isClosing) return;
            activeServerCount = 0;
            offlineServerCount = 0;

            foreach (ListViewItem item in serverListView.Items)
            {
                string host = (string)item.Tag;
                Ping pingSender = new Ping();
                try
                {
                    PingReply reply = await pingSender.SendPingAsync(host, 2000);
                    if (isClosing) return;
                    if (reply.Status == IPStatus.Success)
                    {
                        activeServerCount++;

                        long pingVal = reply.RoundtripTime;
                        item.SubItems[4].Text = pingVal.ToString() + " ms";

                        // نظام ألوان احترافي متدرج للنص فقط
                        if (pingVal <= 20)
                            item.SubItems[4].ForeColor = Color.FromArgb(50, 205, 50);
                        else if (pingVal <= 50)
                            item.SubItems[4].ForeColor = Color.FromArgb(34, 139, 34);
                        else if (pingVal <= 80)
                            item.SubItems[4].ForeColor = Color.FromArgb(154, 205, 50);
                        else if (pingVal <= 120)
                            item.SubItems[4].ForeColor = Color.FromArgb(255, 215, 0);
                        else if (pingVal <= 180)
                            item.SubItems[4].ForeColor = Color.FromArgb(255, 140, 0);
                        else if (pingVal <= 250)
                            item.SubItems[4].ForeColor = Color.FromArgb(255, 69, 0);
                        else
                            item.SubItems[4].ForeColor = Color.FromArgb(220, 20, 60);

                        if (!lastPingTimes.ContainsKey(host))
                            lastPingTimes[host] = new List<long>();
                        
                        lastPingTimes[host].Add(reply.RoundtripTime);
                        if (lastPingTimes[host].Count > 2)
                            lastPingTimes[host].RemoveAt(0);

                        // العمود السادس: Jitter (ms)
                        if (lastPingTimes[host].Count == 2)
                        {
                            long jitter = Math.Abs(lastPingTimes[host][1] - lastPingTimes[host][0]);
                            item.SubItems[5].Text = jitter.ToString() + " ms";

                            if (jitter <= 5)
                                item.SubItems[5].ForeColor = Color.FromArgb(50, 205, 50);
                            else if (jitter <= 15)
                                item.SubItems[5].ForeColor = Color.FromArgb(34, 139, 34);
                            else if (jitter <= 30)
                                item.SubItems[5].ForeColor = Color.FromArgb(154, 205, 50);
                            else if (jitter <= 50)
                                item.SubItems[5].ForeColor = Color.FromArgb(255, 215, 0);
                            else if (jitter <= 80)
                                item.SubItems[5].ForeColor = Color.FromArgb(255, 140, 0);
                            else
                                item.SubItems[5].ForeColor = Color.FromArgb(220, 20, 60);
                        }
                        else
                        {
                            item.SubItems[5].Text = "0 ms";
                            item.SubItems[5].ForeColor = Color.FromArgb(50, 205, 50);
                        }

                        // العمود السابع: Loss (%)
                        item.SubItems[6].Text = "0 %";
                        item.SubItems[6].ForeColor = Color.FromArgb(50, 205, 50);
                    }
                    else
                    {
                        HandleOfflineServer(item, host);
                    }
                }
                catch
                {
                    HandleOfflineServer(item, host);
                }
            }

            UpdateStatusBar();
        }

        // ============== معالجة الخوادم غير المتصلة ==============
        private void HandleOfflineServer(ListViewItem item, string host)
        {
            offlineServerCount++;

            item.SubItems[4].Text = "N/A";
            item.SubItems[5].Text = "N/A";
            item.SubItems[4].ForeColor = Color.Gray;
            item.SubItems[5].ForeColor = Color.Gray;

            if (!lostCount.ContainsKey(host))
            {
                lostCount[host] = 0;
                totalCount[host] = 0;
            }

            totalCount[host]++;
            lostCount[host]++;
            double lossPercent = (lostCount[host] / (double)totalCount[host]) * 100;
            item.SubItems[6].Text = lossPercent.ToString("F1") + " %";

            if (lossPercent == 0)
                item.SubItems[6].ForeColor = Color.FromArgb(50, 205, 50);
            else if (lossPercent <= 1)
                item.SubItems[6].ForeColor = Color.FromArgb(34, 139, 34);
            else if (lossPercent <= 3)
                item.SubItems[6].ForeColor = Color.FromArgb(255, 215, 0);
            else if (lossPercent <= 7)
                item.SubItems[6].ForeColor = Color.FromArgb(255, 140, 0);
            else if (lossPercent <= 15)
                item.SubItems[6].ForeColor = Color.FromArgb(255, 69, 0);
            else
                item.SubItems[6].ForeColor = Color.FromArgb(220, 20, 60);
        }

        // ============== تحديث شريط الحالة ==============
        private void UpdateStatusBar()
        {
            int totalServers = serverListView.Items.Count;

            if (currentLanguage == "ar")
            {
                lblServerCount.Text = "📊 الخوادم: " + totalServers.ToString();
                lblActiveServers.Text = "✅ متصل: " + activeServerCount.ToString();
                lblOfflineServers.Text = "❌ غير متصل: " + offlineServerCount.ToString();
            }
            else
            {
                lblServerCount.Text = "📊 Servers: " + totalServers.ToString();
                lblActiveServers.Text = "✅ Online: " + activeServerCount.ToString();
                lblOfflineServers.Text = "❌ Offline: " + offlineServerCount.ToString();
            }
        }

        // ============== حدث فرز الأعمدة عند النقر على رأس العمود ==============
        private void ServerListView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListView listView = (ListView)sender;
            ListViewItemComparer sorter = new ListViewItemComparer(e.Column);
            listView.ListViewItemSorter = sorter;
            listView.Sort();
        }

        // ============== رسم رؤوس الأعمدة ==============
        private void ServerListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawBackground();
            e.DrawText();
        }

        // ============== رسم العناصر الفرعية مع الألوان الخلفية ==============
        private void ServerListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawBackground();

            if (e.ColumnIndex == 0)
            {
                e.DrawDefault = true;
                return;
            }

            if (e.ColumnIndex == 4 || e.ColumnIndex == 5 || e.ColumnIndex == 6)
            {
                using (SolidBrush backBrush = new SolidBrush(e.SubItem.BackColor))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                }
            }

            StringFormat textFormat = new StringFormat();
            if (e.ColumnIndex == 4 || e.ColumnIndex == 5 || e.ColumnIndex == 6)
                textFormat.Alignment = StringAlignment.Center;
            else
                textFormat.Alignment = StringAlignment.Near;
            
            textFormat.LineAlignment = StringAlignment.Center;

            using (SolidBrush foreBrush = new SolidBrush(e.SubItem.ForeColor))
            {
                Rectangle textRect = e.Bounds;
                if (e.ColumnIndex != 4 && e.ColumnIndex != 5 && e.ColumnIndex != 6)
                    textRect.X += 5;
                
                e.Graphics.DrawString(e.SubItem.Text, e.SubItem.Font, foreBrush, textRect, textFormat);
            }

            using (Pen gridPen = new Pen(Color.FromArgb(50, 50, 50)))
            {
                e.Graphics.DrawRectangle(gridPen, e.Bounds);
            }
        }

        // ============== حدث تبديل اللغة ==============
        private void btnToggleLang_Click(object sender, EventArgs e)
        {
            pingTimer.Stop();

            if (currentLanguage == "ar")
            {
                currentLanguage = "en";
                btnToggleLang.Text = "🌐 ar";
                btnToggleTimer.Text = updating ? "⏸ Pause" : "▶️ Start";
                btnChangeInterval.Text = "⏱ Change Interval";
                btnToggleIP.Text = isIPVisible ? "🙈 Hide IP" : "👁️ Show IP";
            }
            else
            {
                currentLanguage = "ar";
                btnToggleLang.Text = "🌐 en";
                btnToggleTimer.Text = updating ? "⏸ إيقاف" : "▶️ تشغيل";
                btnChangeInterval.Text = "⏱ تغيير الفترة";
                btnToggleIP.Text = isIPVisible ? "🙈 إخفاء IP" : "👁️ إظهار IP";
            }

            SetupListViewColumns();
            PopulateProviderCheckboxes();
            PopulateListView();
            UpdateStatusBar();
            pingTimer.Start();
        }

        // ============== ListView Item Comparer ==============
        private class ListViewItemComparer : System.Collections.IComparer
        {
            private int colIndex;

            public ListViewItemComparer(int column)
            {
                colIndex = column;
            }

            public int Compare(object x, object y)
            {
                ListViewItem itemX = (ListViewItem)x;
                ListViewItem itemY = (ListViewItem)y;

                string textX = itemX.SubItems[colIndex].Text;
                string textY = itemY.SubItems[colIndex].Text;

                double numX;
                double numY;
                if (double.TryParse(textX.Replace(" ms", "").Replace("%", "").Trim(), out numX) &&
                    double.TryParse(textY.Replace(" ms", "").Replace("%", "").Trim(), out numY))
                {
                    return numX.CompareTo(numY);
                }

                return string.Compare(textX, textY);
            }
        }
    }
}
