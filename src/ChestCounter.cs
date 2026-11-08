// Счётчик открытых сундуков Fortnite — оверлей с анимацией сундука, шансом жетона,
// настройками (сочетания клавиш, RU/EN) и просмотром карт с сундуками.
// Не читает память игры — просто окна поверх + глобальные клавиши.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("Fortnite Chest Counter")]
[assembly: AssemblyProduct("Fortnite Chest Counter")]
[assembly: AssemblyDescription("Overlay chest counter for Fortnite treasure events")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace ChestCounter
{
    // ---------- строки на двух языках ----------

    static class L
    {
        public static string Lang = "ru";
        static readonly Dictionary<string, string[]> d = new Dictionary<string, string[]>
        {
            {"settings",   new[]{"НАСТРОЙКИ", "SETTINGS"}},
            {"value",      new[]{"ЗНАЧЕНИЕ", "VALUE"}},
            {"apply",      new[]{"УСТАНОВИТЬ", "APPLY"}},
            {"maps",       new[]{"КАРТЫ", "MAPS"}},
            {"hotkeys",    new[]{"ГОРЯЧИЕ КЛАВИШИ", "HOTKEYS"}},
            {"opacity",    new[]{"ПРОЗРАЧНОСТЬ", "OPACITY"}},
            {"done",       new[]{"ГОТОВО", "DONE"}},
            {"session0",   new[]{"СЕССИЯ 0", "RESET"}},
            {"exit",       new[]{"ВЫХОД", "EXIT"}},
            {"plus1",      new[]{"+1", "+1"}},
            {"minus1",     new[]{"−1", "-1"}},
            {"panel",      new[]{"Панель", "Panel"}},
            {"pressKey",   new[]{"Нажми клавишу…", "Press a key…"}},
            {"hintCombo",  new[]{"жми сочетание для «{0}» (Esc — отмена)", "press combo for \"{0}\" (Esc to cancel)"}},
            {"perSession", new[]{"за сессию: ", "session: "}},
            {"token",      new[]{"ЖЕТОН", "TOKEN"}},
            {"mapsTitle",  new[]{"КАРТЫ С СУНДУКАМИ", "CHEST MAPS"}},
            {"addPhoto",   new[]{"Добавить фото…", "Add photo…"}},
            {"delete",     new[]{"Удалить", "Delete"}},
            {"showMap",    new[]{"Показать карту", "Show map"}},
            {"hideMap",    new[]{"Скрыть карту", "Hide map"}},
            {"noMaps",     new[]{"нет карт — добавь через «Карты»", "no maps — add via Maps"}},
            {"viewerOp",   new[]{"прозр", "opacity"}},
            {"selectFirst",new[]{"Сначала выбери карту — кликни по её миниатюре.", "Select a map first — click its thumbnail."}},
            {"delConfirm", new[]{"Удалить выбранную карту?", "Delete the selected map?"}},
            {"delTitle",   new[]{"Карта", "Map"}},
            {"dupCombo",   new[]{"Это сочетание уже занято другим действием.", "This combo is already used by another action."}},
            {"busyCombo",  new[]{"Сочетание занято системой или другой программой — попробуй другое.", "Combo is taken by the system or another app — try another."}},
            {"alreadyRun", new[]{"Счётчик уже запущен.", "Counter is already running."}},
            {"firstSetup", new[]{"ПЕРВАЯ НАСТРОЙКА", "FIRST-TIME SETUP"}},
            {"language",   new[]{"ЯЗЫК / LANGUAGE", "LANGUAGE"}},
            {"opened",     new[]{"СУНДУКОВ УЖЕ ОТКРЫТО", "CHESTS ALREADY OPENED"}},
            {"openedHint", new[]{"примерная цифра — можно изменить позже", "rough guess — change it later in settings"}},
            {"keysTitle",  new[]{"КЛАВИШИ +1 / −1", "+1 / −1 KEYS"}},
            {"keyHintIdle",new[]{"жми клавишу +1 после каждого сундука · −1 откатывает", "press +1 after every chest · −1 undoes"}},
            {"hintGen1",   new[]{"кнопка › на панели открывает настройки (или F10)", "the › button opens settings (or F10)"}},
            {"hintGen2",   new[]{"«Карты» — оверлей с картами, где лежат сундуки", "Maps opens the chest-location overlay"}},
            {"hintGen3",   new[]{"позиция и прозрачность меняются в настройках", "position and opacity are adjustable in settings"}},
            {"start",      new[]{"НАЧАТЬ СЧИТАТЬ", "START COUNTING"}}
        };

        public static string T(string key)
        {
            string[] v;
            return d.TryGetValue(key, out v) ? v[Lang == "ru" ? 0 : 1] : key;
        }

        public static string F(string key, object arg)
        {
            return string.Format(T(key), arg);
        }
    }

    // ---------- коды иконок Font Awesome (solid) ----------

    static class FA
    {
        public const string Gear = "\uF013";
        public const string ListUl = "\uF0CA";
        public const string Download = "\uF019";
        public const string Map = "\uF279";
        public const string Keyboard = "\uF11C";
        public const string ArrowUp = "\uF062";
        public const string ArrowDown = "\uF063";
        public const string Table = "\uF00A";
        public const string Check = "\uF00C";
        public const string RotateLeft = "\uF2EA";
        public const string SignOut = "\uF2F5";
        public const string ChevronRight = "\uF054";
        public const string Xmark = "\uF00D";
        public const string Sliders = "\uF1DE";
        public const string Trash = "\uF2ED";
        public const string Eye = "\uF06E";
        public const string EyeSlash = "\uF070";
    }

    // ---------- круглая кнопка с иконкой и надписью ----------

    public class IconButton : Control
    {
        string glyph = "";
        string label = "";
        public bool Primary;
        bool hover;

        public string Glyph
        {
            get { return glyph; }
            set { glyph = value; Invalidate(); }
        }

        public string Label
        {
            get { return label; }
            set { label = value; Invalidate(); }
        }

        static readonly Color GoldText = CounterForm.GoldText;
        static readonly Color GoldBorder = CounterForm.GoldBorder;

        public IconButton(string glyph, string label, bool primary)
        {
            Glyph = glyph;
            Label = label;
            Primary = primary;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(24, 24, 30);
            Cursor = Cursors.Hand;
            TabStop = false;
            MouseEnter += delegate { hover = true; Invalidate(); };
            MouseLeave += delegate { hover = false; Invalidate(); };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color fill = Primary ? Color.FromArgb(255, 200, 55)
                : (hover ? Color.FromArgb(56, 56, 66) : Color.FromArgb(38, 38, 46));
            Color line = Primary ? Color.FromArgb(255, 220, 100) : GoldBorder;
            Color txt = !Enabled ? Color.FromArgb(110, 110, 120)
                : (Primary ? Color.FromArgb(30, 26, 10) : GoldText);
            using (GraphicsPath p = CounterForm.RoundedPath(r, 12))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                using (Pen pen = new Pen(line, 1.4F)) g.DrawPath(pen, p);
            }

            // автоподбор размера шрифта, чтобы содержимое не вылезало за кнопку
            Font tf = new Font("Segoe UI", 9F, FontStyle.Bold);
            Size ts = TextRenderer.MeasureText(Label, tf);
            Font gf = Glyph.Length > 0 && CounterForm.FaFamily != null
                ? new Font(CounterForm.FaFamily, Height * 0.40F, FontStyle.Regular) : null;
            int glyphW = gf != null ? (int)g.MeasureString(Glyph, gf).Width : 0;
            int total = glyphW + (glyphW > 0 ? 7 : 0) + ts.Width;
            float[] trySizes = { 8F, 7F };
            int t = 0;
            while (total > Width - 8 && t < trySizes.Length)
            {
                tf.Dispose();
                tf = new Font("Segoe UI", trySizes[t], FontStyle.Bold);
                ts = TextRenderer.MeasureText(Label, tf);
                if (gf != null) gf.Dispose();
                gf = Glyph.Length > 0 && CounterForm.FaFamily != null
                    ? new Font(CounterForm.FaFamily, Height * (0.36F - 0.02F * (t + 1)), FontStyle.Regular) : null;
                glyphW = gf != null ? (int)g.MeasureString(Glyph, gf).Width : 0;
                total = glyphW + (glyphW > 0 ? 7 : 0) + ts.Width;
                t++;
            }
            int x = (Width - total) / 2;
            if (gf != null)
            {
                float gh = gf.GetHeight(g);
                using (SolidBrush b = new SolidBrush(txt))
                    g.DrawString(Glyph, gf, b, x, (Height - gh) / 2 + 1);
                x += glyphW + 7;
            }
            TextRenderer.DrawText(g, Label, tf, new Point(x, Height / 2 - ts.Height / 2), txt);
            tf.Dispose();
            if (gf != null) gf.Dispose();
        }
    }

    public class CounterForm : Form
    {
        // id горячих клавиш (внутренние)
        const int HK_INC = 1;
        const int HK_DEC = 2;
        const int HK_EDIT = 3;

        // модификаторы RegisterHotKey
        const int MOD_ALT = 0x1;
        const int MOD_CONTROL = 0x2;
        const int MOD_SHIFT = 0x4;

        // официальный шанс жетона — фиксированный, менять нельзя
        const int ODDS = 137500;

        const int WM_HOTKEY = 0x0312;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_NOACTIVATE = 0x08000000;
        const int WS_EX_TOOLWINDOW = 0x80;
        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_FRAMECHANGED = 0x0020;

        // геометрия главного окна
        const int FORM_W = 500;
        const int FORM_H = 124;
        const int EDIT_H = 302;
        const int CHEST = 92;
        const int CHEST_X = 14;
        const int CHEST_Y = 16;
        static readonly Rectangle TileRect = new Rectangle(10, 12, 100, 100);
        static Rectangle BtnRect() { return new Rectangle(FORM_W - 58, (FORM_H - 48) / 2, 46, 48); }

        // анимация сундука
        const int POP_MS = 200;
        const int HOLD_MS = 600;
        const int SETTLE_MS = 130;
        const int SHAKE_MS = 180;

        // искры на фоне
        const int SPN = 22;

        public static readonly Color GoldText = Color.FromArgb(255, 211, 77);
        public static readonly Color GoldBorder = Color.FromArgb(201, 162, 39);
        static readonly Color GrayText = Color.FromArgb(168, 168, 178);
        static readonly Color DarkText = Color.FromArgb(150, 150, 162);
        static readonly Color PanelBg = Color.FromArgb(24, 24, 30);

        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")] static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int w, int h);
        [DllImport("user32.dll")] static extern bool ShowCaret(IntPtr hWnd);
        [DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(uint color);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        public static FontFamily FaFamily;   // Font Awesome (иконки)

        Panel editor;
        TextBox numBox;
        IconButton btnKeyInc;
        IconButton btnKeyDec;
        IconButton btnKeyEdit;
        IconButton setBtn;
        IconButton mapsBtn;
        IconButton doneBtn;
        IconButton resetBtn;
        IconButton exitBtn;
        IconButton langBtn;
        System.Windows.Forms.Timer flashTimer;
        System.Windows.Forms.Timer animTimer;
        System.Windows.Forms.Timer sparkTimer;
        System.Windows.Forms.Timer topTimer;
        System.Windows.Forms.Timer cursorTimer;
        MapsManagerForm mapsForm;
        MapViewerForm mapViewer;
        bool openMapsFlag;

        Font numFont;
        Font pctFont;
        Font uiFont;
        Font smallFont;
        Font capFont;
        Font headerFont;

        Image imgClosed;
        Image imgOpen;
        Image curChest;
        double chestScale = 1.0;
        int chestOffX = 0;
        int animMode;      // 0 покой, 1 открытие, 3 закрытие, 4 тряска
        int animStart;
        int numFlash;      // 0 золотой, 1 зелёный, 2 красный

        // искры
        double[] spX = new double[SPN];
        double[] spY = new double[SPN];
        double[] spPhase = new double[SPN];
        double[] spSpd = new double[SPN];
        double[] spS = new double[SPN];
        Random rnd = new Random();
        bool uiSliderDrag;
        bool interactiveNow;   // курсор над кнопкой «›» — окно ловит клики

        string sessionText = "session: +0";
        string tokenPctText = "0,00%";
        string tokenCountText = "0 / 137 500";

        public int count;
        int session;
        // горячие клавиши упакованы: старший байт — модификаторы, младшее слово — VK
        public int hkInc = 0x77;   // F8
        public int hkDec = 0x78;   // F9
        int hkEdit = 0x79;  // F10
        int captureAction = -1;
        int posX = -1;
        int posY = -1;
        int uiOp = 92;         // прозрачность основного окна, % (40..100)
        bool editMode;
        bool loaded;
        bool firstRun;   // нет state.txt — показать мастер первой настройки
        string statePath;
        Point posBeforeEdit;
        bool movedInEdit;

        // настройки карт
        public string mapSel = "";
        public int mapOp = 70;
        public int mapScale = 45;
        public int mapVis = 0;
        public int mapX = -1;
        public int mapY = -1;

        static string mapsDir;
        public static string MapsDir()
        {
            if (mapsDir == null)
            {
                mapsDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FortniteChestCounter", "Maps");
                Directory.CreateDirectory(mapsDir);
            }
            return mapsDir;
        }

        public bool EditModeNow { get { return editMode; } }

        public CounterForm(bool openMaps)
        {
            openMapsFlag = openMaps;

            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FortniteChestCounter");
            Directory.CreateDirectory(dir);
            MapsDir();
            statePath = Path.Combine(dir, "state.txt");
            firstRun = !File.Exists(statePath);
            LoadState();

            // шрифт иконок: извлекаем из ресурсов и подключаем
            try
            {
                byte[] fa = null;
                using (Stream s = typeof(CounterForm).Assembly.GetManifestResourceStream("ChestCounter.fa.ttf"))
                {
                    if (s != null)
                    {
                        MemoryStream ms = new MemoryStream();
                        byte[] buf = new byte[8192];
                        int n;
                        while ((n = s.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
                        fa = ms.ToArray();
                        ms.Dispose();
                    }
                }
                if (fa != null)
                {
                    string faPath = Path.Combine(dir, "fa.ttf");
                    File.WriteAllBytes(faPath, fa);
                    using (PrivateFontCollection pfc = new PrivateFontCollection())
                    {
                        pfc.AddFontFile(faPath);
                        if (pfc.Families.Length > 0) FaFamily = pfc.Families[0];
                    }
                }
            }
            catch { }

            using (Image rawC = LoadRes("chest_closed.png"))
            using (Image rawO = LoadRes("chest_open.png"))
            {
                imgClosed = ScaleImg(rawC, (int)(CHEST * 1.25));
                imgOpen = ScaleImg(rawO, (int)(CHEST * 1.25));
            }
            curChest = imgClosed;

            numFont = new Font("Segoe UI Black", 28F, FontStyle.Regular, GraphicsUnit.Point);
            pctFont = new Font("Segoe UI Black", 16F, FontStyle.Regular, GraphicsUnit.Point);
            uiFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            smallFont = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
            capFont = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
            headerFont = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(18, 18, 22);
            DoubleBuffered = true;
            KeyPreview = true;

            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            // по умолчанию — нижний левый угол (как на фото пользователя)
            Location = new Point(posX >= 0 ? posX : wa.Left + 8, posY >= 0 ? posY : wa.Bottom - FORM_H - 8);
            Size = new Size(FORM_W, FORM_H);
            Opacity = Math.Max(0.4, (double)uiOp / 100.0);
            UpdateRegion();

            for (int i = 0; i < SPN; i++)
            {
                spX[i] = rnd.NextDouble();
                spY[i] = rnd.NextDouble();
                spPhase[i] = rnd.NextDouble() * Math.PI * 2;
                spSpd[i] = 0.002 + rnd.NextDouble() * 0.004;
                spS[i] = 1.2 + rnd.NextDouble() * 1.6;
            }

            flashTimer = new System.Windows.Forms.Timer();
            flashTimer.Interval = 220;
            flashTimer.Tick += new EventHandler(FlashEnd);

            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 27;
            animTimer.Tick += new EventHandler(AnimTick);

            sparkTimer = new System.Windows.Forms.Timer();
            sparkTimer.Interval = 60;
            sparkTimer.Tick += new EventHandler(SparkTick);
            sparkTimer.Start();

            topTimer = new System.Windows.Forms.Timer();
            topTimer.Interval = 500;
            topTimer.Tick += new EventHandler(delegate
            {
                if (IsHandleCreated)
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            });
            topTimer.Start();

            cursorTimer = new System.Windows.Forms.Timer();
            cursorTimer.Interval = 120;
            cursorTimer.Tick += new EventHandler(delegate
            {
                if (!IsHandleCreated || !Visible) return;
                bool need = editMode;
                if (!need)
                {
                    POINT p;
                    if (GetCursorPos(out p))
                    {
                        Point cl = new Point(p.X - Left, p.Y - Top);
                        need = BtnRect().Contains(cl);
                    }
                }
                if (need != interactiveNow)
                {
                    interactiveNow = need;
                    ApplyUiStyle();
                }
            });
            cursorTimer.Start();

            BuildEditor();
            UpdateKeyButtons();
            UpdateTexts();
            UpdateAll();

            KeyDown += new KeyEventHandler(OnFormKeyDown);
            MouseDown += new MouseEventHandler(FormMouseDown);
            editor.MouseDown += new MouseEventHandler(EditorMouseDown);
            editor.MouseMove += new MouseEventHandler(EditorMouseMove);
            editor.MouseUp += new MouseEventHandler(EditorMouseUp);
            LocationChanged += new EventHandler(delegate
            {
                if (loaded && editMode)
                {
                    movedInEdit = true;
                    SaveState();
                }
            });

            loaded = true;
        }

        Image LoadRes(string name)
        {
            MemoryStream ms = new MemoryStream();
            using (Stream s = typeof(CounterForm).Assembly.GetManifestResourceStream("ChestCounter." + name))
            {
                if (s == null) throw new InvalidOperationException("Resource not found: " + name);
                byte[] buf = new byte[8192];
                int n;
                while ((n = s.Read(buf, 0, buf.Length)) > 0) ms.Write(buf, 0, n);
            }
            return Image.FromStream(ms); // ms не закрываем — GDI+ требует живой поток
        }

        public static Image ScaleImg(Image src, int size)
        {
            int side = Math.Min(src.Width, src.Height);
            Rectangle srcRect = new Rectangle((src.Width - side) / 2, (src.Height - side) / 2, side, side);
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, new Rectangle(0, 0, size, size), srcRect, GraphicsUnit.Pixel);
            }
            return bmp;
        }

        public static GraphicsPath RoundedPath(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ---------- панель настроек ----------

        void BuildEditor()
        {
            editor = new Panel();
            editor.BackColor = PanelBg;
            editor.Bounds = new Rectangle(0, FORM_H, FORM_W, EDIT_H);
            editor.Visible = false;
            editor.Paint += new PaintEventHandler(EditorPaint);
            editor.VisibleChanged += new EventHandler(delegate
            {
                if (editor.Visible)
                    numBox.Location = new Point(110, 44 + (44 - numBox.Height) / 2);
            });

            numBox = MkInput(count.ToString());
            numBox.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            numBox.Bounds = new Rectangle(110, 56, 84, 22);

            setBtn = new IconButton(FA.Download, L.T("apply"), false);
            setBtn.Bounds = new Rectangle(208, 44, 114, 44);
            setBtn.Click += delegate { ApplyCount(); };

            mapsBtn = new IconButton(FA.Map, L.T("maps"), false);
            mapsBtn.Bounds = new Rectangle(330, 44, 88, 44);
            mapsBtn.Click += delegate { OpenMaps(); };

            langBtn = new IconButton("", "RU", false);
            langBtn.Bounds = new Rectangle(FORM_W - 72, 8, 60, 26);
            langBtn.Click += delegate
            {
                L.Lang = L.Lang == "ru" ? "en" : "ru";
                SaveState();
                UpdateTexts();
            };

            btnKeyInc = new IconButton(FA.ArrowUp, "+1 — F8", false);
            btnKeyInc.Bounds = new Rectangle(12, 134, 130, 44);
            btnKeyInc.Click += delegate { StartCapture(0); };

            btnKeyDec = new IconButton(FA.ArrowDown, "−1 — F9", false);
            btnKeyDec.Bounds = new Rectangle(150, 134, 130, 44);
            btnKeyDec.Click += delegate { StartCapture(1); };

            btnKeyEdit = new IconButton(FA.Table, "Панель — F10", false);
            btnKeyEdit.Bounds = new Rectangle(288, 134, 130, 44);
            btnKeyEdit.Click += delegate { StartCapture(2); };

            doneBtn = new IconButton(FA.Check, L.T("done"), true);
            doneBtn.Bounds = new Rectangle(12, 240, 130, 48);
            doneBtn.Click += delegate { ToggleEdit(); };

            resetBtn = new IconButton(FA.RotateLeft, L.T("session0"), false);
            resetBtn.Bounds = new Rectangle(150, 240, 130, 48);
            resetBtn.Click += delegate
            {
                session = 0;
                UpdateAll();
                SaveState();
            };

            exitBtn = new IconButton(FA.SignOut, L.T("exit"), false);
            exitBtn.Bounds = new Rectangle(288, 240, 130, 48);
            exitBtn.Click += delegate { Close(); };

            editor.Controls.AddRange(new Control[] {
                numBox, setBtn, mapsBtn, langBtn,
                btnKeyInc, btnKeyDec, btnKeyEdit,
                doneBtn, resetBtn, exitBtn });
            Controls.Add(editor);
        }

        TextBox MkInput(string text)
        {
            TextBox t = new TextBox();
            t.BorderStyle = BorderStyle.None;
            t.BackColor = PanelBg;
            t.ForeColor = GoldText;
            t.TextAlign = HorizontalAlignment.Center;
            t.Text = text;
            t.KeyPress += new KeyPressEventHandler(NumKeyPress);
            t.GotFocus += new EventHandler(GoldCaret);
            return t;
        }

        void NumKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        // золотой курсор вместо чёрного (на тёмном поле чёрный не видно)
        void GoldCaret(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            IntPtr brush = CreateSolidBrush(0x004DD3FF);
            CreateCaret(tb.Handle, brush, 2, tb.Height);
            ShowCaret(tb.Handle);
        }

        Rectangle UiSliderRect()
        {
            return new Rectangle(180, 204, 170, 8);
        }

        Rectangle UiSliderZone()
        {
            Rectangle sr = UiSliderRect();
            sr.Inflate(12, 14);
            return sr;
        }

        void SetUiOpFromX(int x)
        {
            Rectangle sr = UiSliderRect();
            int frac = Math.Max(0, Math.Min(sr.Width, x - sr.X));
            uiOp = 40 + frac * 60 / Math.Max(1, sr.Width);
            Opacity = Math.Max(0.4, (double)uiOp / 100.0);
            editor.Invalidate();
        }

        void EditorMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && UiSliderZone().Contains(e.Location))
            {
                uiSliderDrag = true;
                SetUiOpFromX(e.X);
            }
            else StartDrag(sender, e);
        }

        void EditorMouseMove(object sender, MouseEventArgs e)
        {
            if (uiSliderDrag) SetUiOpFromX(e.X);
        }

        void EditorMouseUp(object sender, MouseEventArgs e)
        {
            if (uiSliderDrag)
            {
                uiSliderDrag = false;
                SaveState();
            }
        }

        void DrawFaHeader(Graphics g, string glyph, string text, int y, bool withLine, int lineEnd)
        {
            int x = 12;
            if (CounterForm.FaFamily != null)
            {
                using (Font gf = new Font(CounterForm.FaFamily, 11F, FontStyle.Regular))
                {
                    float gh = gf.GetHeight(g);
                    using (SolidBrush b = new SolidBrush(GoldText))
                        g.DrawString(glyph, gf, b, x, y + (20 - gh) / 2);
                    x += (int)g.MeasureString(glyph, gf).Width + 7;
                }
            }
            TextRenderer.DrawText(g, text, headerFont, new Point(x, y + 1), GoldText);
            if (withLine)
            {
                int tw = TextRenderer.MeasureText(text, headerFont).Width;
                using (Pen p = new Pen(Color.FromArgb(90, GoldBorder), 1F))
                    g.DrawLine(p, x + tw + 10, y + 12, lineEnd, y + 12);
            }
        }

        void DrawFaRow(Graphics g, string glyph, string text, int x, int y)
        {
            if (CounterForm.FaFamily != null)
            {
                using (Font gf = new Font(CounterForm.FaFamily, 9F, FontStyle.Regular))
                {
                    float gh = gf.GetHeight(g);
                    using (SolidBrush b = new SolidBrush(GrayText))
                        g.DrawString(glyph, gf, b, x, y + (20 - gh) / 2);
                    x += (int)g.MeasureString(glyph, gf).Width + 6;
                }
            }
            TextRenderer.DrawText(g, text, uiFont, new Point(x, y), GrayText);
        }

        // рамка настроек: прямая верхняя кромка (линия стыка с главным окном),
        // скругление только на нижних углах
        GraphicsPath EditorFramePath(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddLine(r.Left, r.Top, r.Right, r.Top);
            p.AddLine(r.Right, r.Top, r.Right, r.Bottom - rad);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 270, 90);
            p.AddLine(r.Right - rad, r.Bottom, r.Left + rad, r.Bottom);
            p.AddArc(r.Left, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            p.AddLine(r.Left, r.Bottom - rad, r.Left, r.Top);
            p.CloseFigure();
            return p;
        }

        void EditorPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // рамка настроек с жёлтой нижней кромкой
            Rectangle frameRect = new Rectangle(0, 0, editor.Width - 1, editor.Height - 1);
            using (GraphicsPath frame = EditorFramePath(frameRect, 14))
            {
                g.SetClip(frame);
                Rectangle bottom = new Rectangle(0, editor.Height - 6, editor.Width, 6);
                using (LinearGradientBrush gb = new LinearGradientBrush(bottom, Color.FromArgb(255, 205, 60), Color.FromArgb(255, 150, 0), 90F))
                    g.FillRectangle(gb, bottom);
                g.ResetClip();
                using (Pen bp = new Pen(GoldBorder, 1.6F))
                    g.DrawPath(bp, frame);
            }

            DrawFaHeader(g, FA.Gear, L.T("settings"), 12, true, FORM_W - 84);
            DrawFaHeader(g, FA.Keyboard, L.T("hotkeys"), 104, false, 0);

            // рамка поля ввода
            using (Pen p = new Pen(Color.FromArgb(150, GoldBorder), 1F))
            using (GraphicsPath gp = RoundedPath(new Rectangle(104, 44, 96, 44), 10))
                g.DrawPath(p, gp);
            DrawFaRow(g, FA.ListUl, L.T("value"), 14, 56);

            // подсказка показывается только при назначении клавиши
            if (captureAction >= 0)
            {
                string[] acts = { L.T("plus1"), L.T("minus1"), L.T("panel") };
                TextRenderer.DrawText(g, L.F("hintCombo", acts[captureAction]), smallFont,
                    new Rectangle(150, 104, 268, 16), GoldText,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }

            // ползунок прозрачности основного окна
            Rectangle sr = UiSliderRect();
            g.SmoothingMode = SmoothingMode.None;
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(42, 42, 50)))
                g.FillRectangle(tb, sr);
            int fracW = (uiOp - 40) * sr.Width / 60;
            using (SolidBrush fb = new SolidBrush(GoldBorder))
                g.FillRectangle(fb, sr.X, sr.Y, Math.Max(3, fracW), sr.Height);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush kb = new SolidBrush(GoldText))
                g.FillEllipse(kb, sr.X + Math.Max(3, fracW) - 7, sr.Y + sr.Height / 2 - 7, 14, 14);
            DrawFaRow(g, FA.Sliders, L.T("opacity"), 14, 196);
            TextRenderer.DrawText(g, uiOp + "%", uiFont, new Rectangle(356, 196, 52, 20), GoldText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        void ApplyCount()
        {
            string t = numBox.Text.Trim().Replace(" ", "").Replace("\u00A0", "");
            int v;
            if (int.TryParse(t, out v) && v >= 0 && v <= 999999999)
                count = v;
            numBox.Text = count.ToString();
            UpdateAll();
            SaveState();
        }

        // ---------- карты ----------

        public void OpenMaps()
        {
            if (mapsForm == null || mapsForm.IsDisposed)
                mapsForm = new MapsManagerForm(this);
            mapsForm.Show();
            mapsForm.BringToFront();
        }

        public bool IsMapVisible() { return mapViewer != null && mapViewer.Visible; }

        public void ShowMapViewer()
        {
            if (mapViewer == null || mapViewer.IsDisposed)
                mapViewer = new MapViewerForm(this);
            mapViewer.LoadCurrent();
            mapViewer.Show();
            mapViewer.SetEditMode(editMode);
            mapViewer.BringToFront();
            mapVis = 1;
            PersistState();
        }

        public void HideMapViewer()
        {
            if (mapViewer != null) mapViewer.Hide();
            mapVis = 0;
            PersistState();
        }

        public void MapViewerLoadCurrent()
        {
            if (mapViewer != null && mapViewer.Visible) mapViewer.LoadCurrent();
        }

        public void MapViewerReleaseIfCurrent(string name)
        {
            if (mapViewer != null) mapViewer.ReleaseIfCurrent(name);
        }

        public void PersistState() { SaveState(); }

        // ---------- отрисовка основного окна ----------

        void SparkTick(object sender, EventArgs e)
        {
            for (int i = 0; i < SPN; i++)
            {
                spY[i] -= spSpd[i];
                spPhase[i] += 0.12;
                if (spY[i] < -0.02)
                {
                    spY[i] = 1.02;
                    spX[i] = rnd.NextDouble();
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = RoundedPath(card, 14))
            {
                using (LinearGradientBrush bg = new LinearGradientBrush(card, Color.FromArgb(32, 32, 40), Color.FromArgb(12, 12, 16), 90F))
                    g.FillPath(bg, p);
            }

            // искры на фоне
            for (int i = 0; i < SPN; i++)
            {
                int a = 50 + (int)(80 * (0.5 + 0.5 * Math.Sin(spPhase[i])));
                using (SolidBrush sb = new SolidBrush(Color.FromArgb(Math.Min(190, a + 30), 255, 205, 120)))
                    g.FillEllipse(sb, (float)(spX[i] * Width), (float)(spY[i] * Height), (float)spS[i], (float)spS[i]);
            }

            using (GraphicsPath p = RoundedPath(card, 14))
            using (Pen pen = new Pen(GoldBorder, 1.6F))
                g.DrawPath(pen, p);

            // плитка с сундуком (без рамки)
            using (GraphicsPath tp = RoundedPath(TileRect, 14))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(40, 40, 46)))
                g.FillPath(tb, tp);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(curChest, ChestRect());

            string num = count.ToString();
            Color nc = numFlash == 1 ? Color.FromArgb(120, 255, 110) : (numFlash == 2 ? Color.FromArgb(255, 90, 60) : GoldText);
            TextRenderer.DrawText(g, num, numFont, new Rectangle(126, 28, 150, 54), Color.FromArgb(0, 0, 0),
                TextFormatFlags.VerticalCenter | TextFormatFlags.GlyphOverhangPadding);
            g.SmoothingMode = SmoothingMode.None;
            TextRenderer.DrawText(g, num, numFont, new Rectangle(124, 26, 150, 54), nc,
                TextFormatFlags.VerticalCenter | TextFormatFlags.GlyphOverhangPadding);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            TextRenderer.DrawText(g, sessionText, uiFont, new Rectangle(126, 84, 150, 18), GrayText,
                TextFormatFlags.VerticalCenter);

            using (Pen dp = new Pen(Color.FromArgb(58, 58, 68), 1F))
                g.DrawLine(dp, 288, 16, 288, 108);

            // блок жетона: честная вероятность = 1 - (1 - 1/ODDS)^count
            TextRenderer.DrawText(g, L.T("token"), capFont, new Rectangle(300, 18, 120, 14), Color.FromArgb(160, 160, 172),
                TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, tokenPctText, pctFont, new Rectangle(300, 32, 120, 34), GoldText,
                TextFormatFlags.VerticalCenter);

            Rectangle track = new Rectangle(300, 72, 120, 10);
            using (GraphicsPath tp = RoundedPath(track, 5))
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(42, 42, 50)))
                g.FillPath(tb, tp);
            double frac = Math.Min(1.0, (double)count / ODDS);
            int w = (int)Math.Round(120.0 * frac);
            if (count > 0 && w < 3) w = 3;
            if (w > 0)
            {
                Rectangle fill = new Rectangle(300, 72, w, 10);
                using (GraphicsPath fp = RoundedPath(fill, 5))
                using (LinearGradientBrush gb = new LinearGradientBrush(fill, Color.FromArgb(255, 200, 55), Color.FromArgb(255, 157, 0), 0F))
                    g.FillPath(gb, fp);
            }
            TextRenderer.DrawText(g, tokenCountText, smallFont, new Rectangle(300, 86, 120, 16), DarkText,
                TextFormatFlags.VerticalCenter);

            // кнопка «›» (открыть настройки) / «×» (закрыть)
            Rectangle br = BtnRect();
            using (GraphicsPath bp = RoundedPath(br, 14))
            {
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(38, 38, 46)))
                    g.FillPath(bb, bp);
                using (Pen pen = new Pen(GoldBorder, 1.6F))
                    g.DrawPath(pen, bp);
            }
            if (CounterForm.FaFamily != null)
            {
                using (Font gf = new Font(CounterForm.FaFamily, 13F, FontStyle.Regular))
                using (SolidBrush gb = new SolidBrush(GoldText))
                {
                    SizeF gs = g.MeasureString(editMode ? FA.Xmark : FA.ChevronRight, gf);
                    g.DrawString(editMode ? FA.Xmark : FA.ChevronRight, gf, gb,
                        br.X + (br.Width - gs.Width) / 2, br.Y + (br.Height - gs.Height) / 2 + 1);
                }
            }
            else
            {
                using (Font gf = new Font("Segoe UI", 15F, FontStyle.Bold))
                    TextRenderer.DrawText(g, editMode ? "×" : "›", gf, br, GoldText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        Rectangle ChestRect()
        {
            int size = (int)Math.Round(CHEST * chestScale);
            int x = CHEST_X + (CHEST - size) / 2 + chestOffX;
            int y = CHEST_Y + (CHEST - size) / 2;
            return new Rectangle(x, y, size, size);
        }

        void UpdateRegion()
        {
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath gp = RoundedPath(r, 14))
                Region = new Region(gp);
        }

        // ---------- анимация ----------

        void AnimTick(object sender, EventArgs e)
        {
            int el = Environment.TickCount - animStart;
            if (animMode == 1)
            {
                if (el >= POP_MS + HOLD_MS)
                {
                    animMode = 3;
                    animStart = Environment.TickCount;
                }
                else if (el >= POP_MS)
                {
                    SetChest(imgOpen, 1.0, 0);
                }
                else
                {
                    double t = (double)el / POP_MS;
                    SetChest(imgOpen, 1.0 + 0.20 * Math.Sin(Math.PI * t), 0);
                }
            }
            else if (animMode == 3)
            {
                if (el >= SETTLE_MS)
                {
                    animMode = 0;
                    animTimer.Stop();
                    SetChest(imgClosed, 1.0, 0);
                }
                else
                {
                    double t = (double)el / SETTLE_MS;
                    SetChest(imgClosed, 1.0 - 0.08 * Math.Sin(Math.PI * t), 0);
                }
            }
            else if (animMode == 4)
            {
                if (el >= SHAKE_MS)
                {
                    animMode = 0;
                    animTimer.Stop();
                    SetChest(imgClosed, 1.0, 0);
                }
                else
                {
                    int off = (el / 30) % 2 == 0 ? 3 : -3;
                    SetChest(imgClosed, 1.0, off);
                }
            }
            Invalidate();
        }

        void SetChest(Image img, double scale, int offX)
        {
            curChest = img;
            chestScale = scale;
            chestOffX = offX;
        }

        void FlashEnd(object sender, EventArgs e)
        {
            flashTimer.Stop();
            numFlash = 0;
            Invalidate();
        }

        // ---------- счёт ----------

        void Change(int d)
        {
            count += d;
            session += d;
            if (count < 0) count = 0;
            UpdateAll();
            SaveState();
            numFlash = d > 0 ? 1 : 2;
            flashTimer.Stop();
            flashTimer.Start();
            if (d > 0)
            {
                animMode = 1;
                animStart = Environment.TickCount;
                animTimer.Stop();
                animTimer.Start();
            }
            else if (d < 0 && animMode == 0)
            {
                animMode = 4;
                animStart = Environment.TickCount;
                animTimer.Stop();
                animTimer.Start();
            }
        }

        void UpdateAll()
        {
            sessionText = L.T("perSession") + (session >= 0 ? "+" : "") + session;
            double prob = 1.0 - Math.Pow(1.0 - 1.0 / ODDS, count);
            CultureInfo ru = L.Lang == "ru" ? CultureInfo.GetCultureInfo("ru-RU") : CultureInfo.InvariantCulture;
            tokenPctText = (prob * 100.0).ToString(L.Lang == "ru" ? "N2" : "0.00", ru) + "%";
            tokenCountText = count.ToString("N0", ru) + " / " + ODDS.ToString("N0", ru);
            Invalidate();
        }

        // ---------- клавиши (переназначение, в т.ч. сочетания Ctrl/Alt/Shift) ----------

        void StartCapture(int action)
        {
            if (!editMode) return;
            captureAction = action;
            UnregisterAll();
            ActiveControl = null;
            UpdateKeyButtons();
            editor.Invalidate();
        }

        void EndCapture()
        {
            captureAction = -1;
            RegisterAll();
            UpdateKeyButtons();
            editor.Invalidate();
        }

        void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (captureAction < 0) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            Keys k = e.KeyCode;
            if (k == Keys.ShiftKey || k == Keys.ControlKey || k == Keys.Menu
                || k == Keys.LWin || k == Keys.RWin) return; // ждём основную клавишу
            if (k == Keys.Escape)
            {
                EndCapture();
                return;
            }
            int mod = 0;
            if (e.Control) mod |= MOD_CONTROL;
            if (e.Alt) mod |= MOD_ALT;
            if (e.Shift) mod |= MOD_SHIFT;
            int packed = (mod << 16) | (int)k;
            bool dup = (captureAction != 0 && packed == hkInc)
                    || (captureAction != 1 && packed == hkDec)
                    || (captureAction != 2 && packed == hkEdit);
            if (dup)
            {
                MessageBox.Show(L.T("dupCombo"));
                EndCapture();
                return;
            }
            int oldInc = hkInc, oldDec = hkDec, oldEdit = hkEdit;
            if (captureAction == 0) hkInc = packed;
            else if (captureAction == 1) hkDec = packed;
            else hkEdit = packed;
            if (!RegisterAll())
            {
                hkInc = oldInc; hkDec = oldDec; hkEdit = oldEdit;
                RegisterAll();
                MessageBox.Show(L.T("busyCombo"));
            }
            EndCapture();
            SaveState();
        }

        public bool RegisterAll()
        {
            if (!IsHandleCreated) return true;
            UnregisterHotKey(Handle, HK_INC);
            UnregisterHotKey(Handle, HK_DEC);
            UnregisterHotKey(Handle, HK_EDIT);
            bool a = RegisterHotKey(Handle, HK_INC, (uint)ModOf(hkInc), (uint)VkOf(hkInc));
            bool b = RegisterHotKey(Handle, HK_DEC, (uint)ModOf(hkDec), (uint)VkOf(hkDec));
            bool c = RegisterHotKey(Handle, HK_EDIT, (uint)ModOf(hkEdit), (uint)VkOf(hkEdit));
            return a && b && c;
        }

        public void UnregisterAll()
        {
            if (!IsHandleCreated) return;
            UnregisterHotKey(Handle, HK_INC);
            UnregisterHotKey(Handle, HK_DEC);
            UnregisterHotKey(Handle, HK_EDIT);
        }

        static int ModOf(int packed) { return (packed >> 16) & 0xFF; }
        static int VkOf(int packed) { return packed & 0xFFFF; }

        public string ComboName(int packed)
        {
            int m = ModOf(packed);
            string s = "";
            if ((m & MOD_CONTROL) != 0) s += "Ctrl+";
            if ((m & MOD_ALT) != 0) s += "Alt+";
            if ((m & MOD_SHIFT) != 0) s += "Shift+";
            return s + VkName((uint)VkOf(packed));
        }

        string VkName(uint vk)
        {
            Keys k = (Keys)vk;
            if (k >= Keys.D0 && k <= Keys.D9) return ((char)('0' + (k - Keys.D0))).ToString();
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return "Num" + (char)('0' + (k - Keys.NumPad0));
            switch (k)
            {
                case Keys.Escape: return "Esc";
                case Keys.Return: return "Enter";
                case Keys.Space: return "Space";
                case Keys.Back: return "Backspace";
                case Keys.Tab: return "Tab";
                case Keys.Capital: return "Caps Lock";
                case Keys.Prior: return "Page Up";
                case Keys.Next: return "Page Down";
                case Keys.End: return "End";
                case Keys.Home: return "Home";
                case Keys.Insert: return "Insert";
                case Keys.Delete: return "Delete";
                case Keys.OemMinus: return "-";
                case Keys.Oemplus: return "+";
                case Keys.OemOpenBrackets: return "[";
                case Keys.OemCloseBrackets: return "]";
                case Keys.OemSemicolon: return ";";
                case Keys.OemQuotes: return "'";
                case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return ".";
                case Keys.OemQuestion: return "/";
                case Keys.Oemtilde: return "`";
                case Keys.OemBackslash: return "\\";
                default: return k.ToString();
            }
        }

        void UpdateKeyButtons()
        {
            string[] glyphs = { FA.ArrowUp, FA.ArrowDown, FA.Table };
            string[] acts = { L.T("plus1"), L.T("minus1"), L.T("panel") };
            IconButton[] btns = { btnKeyInc, btnKeyDec, btnKeyEdit };
            int[] hks = { hkInc, hkDec, hkEdit };
            for (int i = 0; i < 3; i++)
            {
                if (captureAction == i)
                {
                    btns[i].Glyph = "";
                    btns[i].Label = L.T("pressKey");
                }
                else
                {
                    btns[i].Glyph = glyphs[i];
                    btns[i].Label = acts[i] + " — " + ComboName(hks[i]);
                    btns[i].Enabled = captureAction < 0;
                }
            }
        }

        public void UpdateTexts()
        {
            setBtn.Label = L.T("apply");
            mapsBtn.Label = L.T("maps");
            doneBtn.Label = L.T("done");
            resetBtn.Label = L.T("session0");
            exitBtn.Label = L.T("exit");
            langBtn.Label = L.Lang == "ru" ? "RU" : "EN";
            UpdateKeyButtons();
            UpdateAll();
            editor.Invalidate();
            if (mapsForm != null && !mapsForm.IsDisposed) mapsForm.RefreshTexts();
        }

        // ---------- панель ----------

        void ToggleEdit()
        {
            if (captureAction >= 0) EndCapture();
            editMode = !editMode;
            if (editMode) posBeforeEdit = Location; // запоминаем, где стояла панель
            editor.Visible = editMode;
            if (editMode) numBox.Text = count.ToString();
            Height = editMode ? FORM_H + EDIT_H : FORM_H;
            if (!editMode) ActiveControl = null;
            ApplyUiStyle();
            UpdateRegion();
            if (editMode)
            {
                Location = ClampToScreen(Location);
                movedInEdit = false; // сдвиг из-за clamp — не «перенос пользователем»
            }
            else
            {
                // закрытие: вернуть панель туда, где она была до открытия,
                // если пользователь сам её не перетащил во время настроек
                if (!movedInEdit) Location = ClampToScreen(posBeforeEdit);
                else Location = ClampToScreen(Location);
                movedInEdit = false;
            }
            if (mapViewer != null) mapViewer.SetEditMode(editMode);
            SaveState();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterAll();
            ApplyUiStyle();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            UnregisterAll();
            topTimer.Stop();
            animTimer.Stop();
            sparkTimer.Stop();
            cursorTimer.Stop();
            flashTimer.Stop();
            SaveState();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Location = ClampToScreen(Location);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (firstRun)
            {
                // мастер первой настройки: язык, стартовый счёт, клавиши
                using (SetupWizardForm wiz = new SetupWizardForm(this))
                    wiz.ShowDialog(this);
                firstRun = false;
                RegisterAll();
                UpdateTexts();
                PersistState();
            }
            if (mapVis == 1 && (mapSel.Length > 0 || Directory.GetFiles(MapsDir()).Length > 0))
                ShowMapViewer();
            if (openMapsFlag)
            {
                ToggleEdit();
                OpenMaps();
                ShowMapViewer();
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HK_INC) Change(1);
                else if (id == HK_DEC) Change(-1);
                else if (id == HK_EDIT) ToggleEdit();
                return;
            }
            base.WndProc(ref m);
        }

        // в настройках окно интерактивно целиком; в игре — только когда курсор над «›»
        void ApplyUiStyle()
        {
            if (!IsHandleCreated) return;
            int ex = GetWindowLong(Handle, GWL_EXSTYLE);
            if (editMode)
            {
                ex &= ~WS_EX_TRANSPARENT;
                ex &= ~WS_EX_NOACTIVATE;
            }
            else if (interactiveNow)
            {
                ex &= ~WS_EX_TRANSPARENT;
                ex |= WS_EX_NOACTIVATE;
            }
            else
            {
                ex |= WS_EX_TRANSPARENT;
                ex |= WS_EX_NOACTIVATE;
            }
            SetWindowLong(Handle, GWL_EXSTYLE, ex);
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        void FormMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (BtnRect().Contains(e.Location))
            {
                ToggleEdit();
                return;
            }
            StartDrag(sender, e);
        }

        void StartDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        Point ClampToScreen(Point p)
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (p.X < wa.Left) p.X = wa.Left;
            if (p.Y < wa.Top) p.Y = wa.Top;
            if (p.X > wa.Right - Width) p.X = wa.Right - Width;
            if (p.Y > wa.Bottom - Height) p.Y = wa.Bottom - Height;
            return p;
        }

        // ---------- сохранение ----------

        void LoadState()
        {
            if (!File.Exists(statePath)) return;
            try
            {
                string[] lines = File.ReadAllLines(statePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(new char[] { '=' }, 2);
                    if (parts.Length < 2) continue;
                    int v;
                    if (parts[0] == "count" && int.TryParse(parts[1], out v)) count = v;
                    else if (parts[0] == "session" && int.TryParse(parts[1], out v)) session = v;
                    else if (parts[0] == "x" && int.TryParse(parts[1], out v)) posX = v;
                    else if (parts[0] == "y" && int.TryParse(parts[1], out v)) posY = v;
                    else if (parts[0] == "keyInc" && int.TryParse(parts[1], out v) && v > 0) hkInc = v;
                    else if (parts[0] == "keyDec" && int.TryParse(parts[1], out v) && v > 0) hkDec = v;
                    else if (parts[0] == "keyEdit" && int.TryParse(parts[1], out v) && v > 0) hkEdit = v;
                    else if (parts[0] == "map") mapSel = parts[1].Trim();
                    else if (parts[0] == "mapop" && int.TryParse(parts[1], out v)) mapOp = Math.Max(15, Math.Min(100, v));
                    else if (parts[0] == "mapscale" && int.TryParse(parts[1], out v)) mapScale = Math.Max(20, Math.Min(90, v));
                    else if (parts[0] == "mapvis" && int.TryParse(parts[1], out v)) mapVis = v == 1 ? 1 : 0;
                    else if (parts[0] == "mapx" && int.TryParse(parts[1], out v)) mapX = v;
                    else if (parts[0] == "mapy" && int.TryParse(parts[1], out v)) mapY = v;
                    else if (parts[0] == "uiop" && int.TryParse(parts[1], out v)) uiOp = Math.Max(40, Math.Min(100, v));
                    else if (parts[0] == "lang") L.Lang = parts[1].Trim() == "en" ? "en" : "ru";
                    // старые строки (sound, odds и т.п.) просто игнорируются
                }
            }
            catch { }
        }

        void SaveState()
        {
            try
            {
                string s = "count=" + count + "\r\n"
                    + "session=" + session + "\r\n"
                    + "keyInc=" + hkInc + "\r\n"
                    + "keyDec=" + hkDec + "\r\n"
                    + "keyEdit=" + hkEdit + "\r\n"
                    + "map=" + mapSel + "\r\n"
                    + "mapop=" + mapOp + "\r\n"
                    + "mapscale=" + mapScale + "\r\n"
                    + "mapvis=" + mapVis + "\r\n"
                    + "mapx=" + mapX + "\r\n"
                    + "mapy=" + mapY + "\r\n"
                    + "uiop=" + uiOp + "\r\n"
                    + "lang=" + L.Lang + "\r\n"
                    + "x=" + Location.X + "\r\n"
                    + "y=" + Location.Y + "\r\n";
                File.WriteAllText(statePath, s);
            }
            catch { }
        }
    }

    // ---------- окно просмотра карты (оверлей поверх игры) ----------

    public class MapViewerForm : Form
    {
        const int STRIP = 34;
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WM_SIZING = 0x214;
        CounterForm owner;
        Image img;
        string imgPath;
        bool positioned;
        bool editNow;      // настройки счётчика открыты — рамка, ползунок и края видны
        bool sliderDrag;
        bool ready;
        IconButton bHide;
        Font smallFont;
        System.Windows.Forms.Timer topTimer;

        static readonly Color GoldText = CounterForm.GoldText;
        static readonly Color GoldBorder = CounterForm.GoldBorder;
        static readonly Color DarkText = Color.FromArgb(150, 150, 162);

        const int WM_NCHITTEST = 0x84;
        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_FRAMECHANGED = 0x0020;

        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        public MapViewerForm(CounterForm owner)
        {
            this.owner = owner;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(18, 18, 22);
            DoubleBuffered = true;
            Opacity = Math.Max(0.15, (double)owner.mapOp / 100.0);
            Size = new Size(340, 80);
            smallFont = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);

            bHide = new IconButton(FA.Xmark, "", false);
            bHide.Bounds = new Rectangle(Width - 54, 7, 46, 20);
            bHide.Visible = false;
            bHide.Click += delegate { owner.HideMapViewer(); };
            Controls.Add(bHide);

            topTimer = new System.Windows.Forms.Timer();
            topTimer.Interval = 500;
            topTimer.Tick += new EventHandler(delegate
            {
                if (IsHandleCreated && Visible)
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            });
            topTimer.Start();

            SizeChanged += new EventHandler(delegate
            {
                SaveScale();
                LayoutButtons();
                UpdateRegion();
                Invalidate();
            });

            ready = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80000 | 0x08000000 | 0x80; // layered | noactivate | toolwindow
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyStyle();
        }

        public void LoadCurrent()
        {
            string dir = CounterForm.MapsDir();
            string path = Path.Combine(dir, owner.mapSel);
            if (owner.mapSel.Length == 0 || !File.Exists(path))
            {
                string[] files = Directory.GetFiles(dir);
                Array.Sort(files);
                if (files.Length > 0)
                {
                    path = files[0];
                    owner.mapSel = Path.GetFileName(path);
                }
                else path = null;
            }
            Image old = img;
            img = null;
            imgPath = null;
            if (path != null)
            {
                try { img = Image.FromFile(path); imgPath = owner.mapSel; }
                catch { img = null; }
            }
            if (old != null) old.Dispose();
            // при первой загрузке берём сохранённый масштаб, а не высоту пустого окна
            ResizeTo(editNow ? Height - STRIP : ScreenH() * owner.mapScale / 100, true);
        }

        public void ReleaseIfCurrent(string name)
        {
            if (img != null && imgPath == name)
            {
                Image old = img;
                img = null;
                imgPath = null;
                old.Dispose();
                UpdateRegion();
                Invalidate();
            }
        }

        // переход между режимом настроек (рамка, ползунок, края) и игровым (чистое фото)

        public void SetEditMode(bool on)
        {
            if (editNow == on) return;
            editNow = on;
            int bodyH = on ? Height : Height - STRIP;
            int w = Width;
            if (!on && img != null)
            {
                // ширина под пропорции карты, чтобы скруглённые углы легли ровно на фото
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                double ar = (double)img.Width / img.Height;
                w = (int)Math.Round(bodyH * ar);
                if (w > (int)(wa.Width * 0.9))
                {
                    w = (int)(wa.Width * 0.9);
                    bodyH = (int)Math.Round(w / ar);
                }
            }
            Size = new Size(Math.Max(w, on ? 340 : 160), Math.Max(bodyH, 60) + (on ? STRIP : 0));
            bHide.Visible = on;
            ApplyStyle();
            EnsurePosition();
            LayoutButtons();
            UpdateRegion();
            Invalidate();
        }

        int ScreenH() { return Screen.PrimaryScreen.WorkingArea.Height; }

        void ResizeTo(int bodyH, bool snap)
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int w = Width;
            if (img != null && snap)
            {
                double ar = (double)img.Width / img.Height;
                w = (int)Math.Round(bodyH * ar);
                if (w > (int)(wa.Width * 0.9))
                {
                    w = (int)(wa.Width * 0.9);
                    bodyH = (int)Math.Round(w / ar);
                }
            }
            Size = new Size(Math.Max(w, editNow ? 340 : 160), Math.Max(bodyH, 60) + (editNow ? STRIP : 0));
            EnsurePosition();
            SaveScale();
            LayoutButtons();
            UpdateRegion();
            Invalidate();
        }

        void EnsurePosition()
        {
            if (!positioned)
            {
                if (owner.mapX >= 0)
                    Location = Clamp(new Point(owner.mapX, owner.mapY));
                else
                {
                    Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                    Location = new Point(wa.Right - Width - 24, wa.Top + 90);
                }
                positioned = true;
            }
            else Location = Clamp(Location);
        }

        void SaveScale()
        {
            if (!ready) return;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int bodyH = editNow ? Height - STRIP : Height;
            owner.mapScale = Math.Max(20, Math.Min(90, (int)Math.Round(bodyH * 100.0 / wa.Height)));
        }

        void LayoutButtons()
        {
            bHide.Bounds = new Rectangle(Width - 54, Height - STRIP + 7, 46, 20);
        }

        Rectangle SliderRect()
        {
            int sy = Height - STRIP + 16;
            return new Rectangle(62, sy - 3, Math.Max(60, Width - 134), 6);
        }

        Rectangle SliderZone()
        {
            Rectangle sr = SliderRect();
            sr.Inflate(10, 12);
            return sr;
        }

        void SetOpFromX(int x)
        {
            Rectangle sr = SliderRect();
            int frac = Math.Max(0, Math.Min(sr.Width, x - sr.X));
            owner.mapOp = 15 + frac * 85 / Math.Max(1, sr.Width);
            Opacity = Math.Max(0.15, (double)owner.mapOp / 100.0);
            Invalidate();
        }

        void UpdateRegion()
        {
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath gp = CounterForm.RoundedPath(r, 10))
                Region = new Region(gp);
        }

        Point Clamp(Point p)
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (p.X < wa.Left) p.X = wa.Left;
            if (p.Y < wa.Top) p.Y = wa.Top;
            if (p.X > wa.Right - Width) p.X = wa.Right - Width;
            if (p.Y > wa.Bottom - Height) p.Y = wa.Bottom - Height;
            return p;
        }

        void StartDragNow()
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (editNow && SliderZone().Contains(e.Location))
            {
                sliderDrag = true;
                SetOpFromX(e.X);
            }
            else StartDragNow();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (sliderDrag) SetOpFromX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (sliderDrag)
            {
                sliderDrag = false;
                owner.PersistState();
            }
        }

        // края окна — изменение размера мышкой, пропорции фото сохраняются (только в настройках)
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST && editNow)
            {
                base.WndProc(ref m);
                Point pt = new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF));
                Point pos = PointToClient(pt);
                const int edge = 7;
                bool l = pos.X <= edge, r = pos.X >= Width - edge;
                bool t = pos.Y <= edge, b = pos.Y >= Height - edge;
                if (l && t) m.Result = (IntPtr)13;       // HTTOPLEFT
                else if (r && t) m.Result = (IntPtr)14;  // HTTOPRIGHT
                else if (l && b) m.Result = (IntPtr)16;  // HTBOTTOMLEFT
                else if (r && b) m.Result = (IntPtr)17;  // HTBOTTOMRIGHT
                else if (l) m.Result = (IntPtr)10;       // HTLEFT
                else if (r) m.Result = (IntPtr)11;       // HTRIGHT
                else if (t) m.Result = (IntPtr)12;       // HTTOP
                else if (b) m.Result = (IntPtr)15;       // HTBOTTOM
                return;
            }
            if (m.Msg == WM_SIZING && editNow && img != null)
            {
                RECT r = (RECT)Marshal.PtrToStructure(m.LParam, typeof(RECT));
                double ar = (double)img.Width / img.Height;
                int wp = m.WParam.ToInt32();
                bool top = (wp == 3 || wp == 4 || wp == 5);
                int w = Math.Max(160, r.Right - r.Left);
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                int bodyH = (int)Math.Round(w / ar);
                bodyH = Math.Max(60, Math.Min(bodyH, (int)(wa.Height * 0.9)));
                int th = bodyH + STRIP;
                if (top) r.Top = r.Bottom - th; else r.Bottom = r.Top + th;
                Marshal.StructureToPtr(r, m.LParam, false);
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        void ApplyStyle()
        {
            if (!IsHandleCreated) return;
            int ex = GetWindowLong(Handle, GWL_EXSTYLE);
            if (editNow) ex &= ~WS_EX_TRANSPARENT;   // в настройках окно интерактивно
            else ex |= WS_EX_TRANSPARENT;            // в игре клики проходят насквозь
            SetWindowLong(Handle, GWL_EXSTYLE, ex);
            SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle card = new Rectangle(0, 0, Width - 1, Height - 1);
            if (editNow)
            {
                using (GraphicsPath p = CounterForm.RoundedPath(card, 10))
                {
                    using (LinearGradientBrush bg = new LinearGradientBrush(card, Color.FromArgb(30, 30, 38), Color.FromArgb(14, 14, 18), 90F))
                        g.FillPath(bg, p);
                    using (Pen pen = new Pen(GoldBorder, 1.6F))
                        g.DrawPath(pen, p);
                }
            }
            else
            {
                // игровой режим: без рамок, только скруглённое фото
                using (SolidBrush b = new SolidBrush(BackColor))
                    g.FillRectangle(b, card);
            }

            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            Rectangle area = editNow
                ? new Rectangle(6, 6, Width - 12, Height - STRIP - 12)
                : new Rectangle(0, 0, Width, Height);
            if (img != null)
            {
                double ar = (double)img.Width / img.Height;
                int dh = area.Height, dw = (int)Math.Round(dh * ar);
                if (dw > area.Width)
                {
                    dw = area.Width;
                    dh = (int)Math.Round(dw / ar);
                }
                g.DrawImage(img, new Rectangle(area.X + (area.Width - dw) / 2, area.Y + (area.Height - dh) / 2, dw, dh));
            }
            else
            {
                TextRenderer.DrawText(g, L.T("noMaps"), smallFont,
                    area, DarkText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            if (editNow)
            {
                // ползунок прозрачности
                Rectangle sr = SliderRect();
                g.SmoothingMode = SmoothingMode.None;
                using (SolidBrush tb = new SolidBrush(Color.FromArgb(42, 42, 50)))
                    g.FillRectangle(tb, sr);
                int fracW = (owner.mapOp - 15) * sr.Width / 85;
                using (SolidBrush fb = new SolidBrush(GoldBorder))
                    g.FillRectangle(fb, sr.X, sr.Y, Math.Max(2, fracW), sr.Height);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush kb = new SolidBrush(GoldText))
                    g.FillEllipse(kb, sr.X + Math.Max(2, fracW) - 7, sr.Y + sr.Height / 2 - 7, 14, 14);
                TextRenderer.DrawText(g, L.T("viewerOp"), smallFont, new Point(8, sr.Y - 6), DarkText);
            }
        }
    }

    // ---------- менеджер карт: список, добавление, удаление ----------

    public class MapsManagerForm : Form
    {
        CounterForm owner;
        FlowLayoutPanel flow;
        List<Image> thumbs = new List<Image>();
        string selected;
        IconButton addBtn, delBtn, toggleBtn, closeBtn;
        Label title;
        System.Windows.Forms.Timer topTimer;

        static readonly Color GoldText = CounterForm.GoldText;
        static readonly Color GoldBorder = CounterForm.GoldBorder;

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;
        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public MapsManagerForm(CounterForm owner)
        {
            this.owner = owner;
            selected = owner.mapSel;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(24, 24, 30);
            DoubleBuffered = true;
            Size = new Size(520, 430);
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + 80);
            UpdateRegion();

            Font uiFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);

            title = new Label();
            title.Text = L.T("mapsTitle");
            title.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            title.ForeColor = GoldText;
            title.BackColor = Color.Transparent;
            title.Bounds = new Rectangle(46, 8, 360, 18);
            title.MouseDown += new MouseEventHandler(StartDrag);
            Controls.Add(title);

            closeBtn = new IconButton(FA.Xmark, "", false);
            closeBtn.Bounds = new Rectangle(Width - 46, 5, 38, 26);
            closeBtn.Click += delegate { Close(); };
            Controls.Add(closeBtn);

            flow = new FlowLayoutPanel();
            flow.BackColor = Color.FromArgb(16, 16, 20);
            flow.Bounds = new Rectangle(10, 34, Width - 20, Height - 34 - 52);
            flow.AutoScroll = true;
            Controls.Add(flow);

            addBtn = new IconButton(FA.Download, L.T("addPhoto"), false);
            addBtn.Bounds = new Rectangle(12, Height - 42, 126, 30);
            addBtn.Click += delegate { AddPhoto(); };
            Controls.Add(addBtn);

            delBtn = new IconButton(FA.Trash, L.T("delete"), false);
            delBtn.Bounds = new Rectangle(146, Height - 42, 84, 30);
            delBtn.Click += delegate { DeleteSelected(); };
            Controls.Add(delBtn);

            toggleBtn = new IconButton(FA.Eye, L.T("showMap"), false);
            toggleBtn.Bounds = new Rectangle(238, Height - 42, 126, 30);
            toggleBtn.Click += delegate
            {
                if (owner.IsMapVisible()) owner.HideMapViewer();
                else owner.ShowMapViewer();
                UpdateToggle();
            };
            Controls.Add(toggleBtn);

            MouseDown += new MouseEventHandler(StartDrag);

            topTimer = new System.Windows.Forms.Timer();
            topTimer.Interval = 500;
            topTimer.Tick += new EventHandler(delegate
            {
                if (IsHandleCreated)
                    SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            });
            topTimer.Start();

            Paint += new PaintEventHandler(ManagerPaint);
            BuildThumbs();
            UpdateToggle();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x80; // toolwindow
                return cp;
            }
        }

        void ManagerPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle frameRect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath frame = CounterForm.RoundedPath(frameRect, 12))
            {
                g.SetClip(frame);
                Rectangle bottom = new Rectangle(0, Height - 6, Width, 6);
                using (LinearGradientBrush gb = new LinearGradientBrush(bottom, Color.FromArgb(255, 205, 60), Color.FromArgb(255, 150, 0), 90F))
                    g.FillRectangle(gb, bottom);
                g.ResetClip();
                using (Pen bp = new Pen(GoldBorder, 1.6F))
                    g.DrawPath(bp, frame);
            }
            // иконка карты у заголовка
            if (CounterForm.FaFamily != null)
            {
                using (Font gf = new Font(CounterForm.FaFamily, 11F, FontStyle.Regular))
                using (SolidBrush b = new SolidBrush(GoldText))
                    g.DrawString(FA.Map, gf, b, 16, 8);
            }
            using (Pen p = new Pen(Color.FromArgb(90, GoldBorder), 1F))
                g.DrawLine(p, 14, 30, Width - 14, 30);
        }

        public void RefreshTexts()
        {
            title.Text = L.T("mapsTitle");
            addBtn.Label = L.T("addPhoto");
            delBtn.Label = L.T("delete");
            UpdateToggle();
            Invalidate();
        }

        void UpdateToggle()
        {
            bool vis = owner.IsMapVisible();
            toggleBtn.Glyph = vis ? FA.EyeSlash : FA.Eye;
            toggleBtn.Label = vis ? L.T("hideMap") : L.T("showMap");
        }

        void UpdateRegion()
        {
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath gp = CounterForm.RoundedPath(r, 12))
                Region = new Region(gp);
        }

        void StartDrag(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        void BuildThumbs()
        {
            foreach (Image t in thumbs) t.Dispose();
            thumbs.Clear();
            flow.Controls.Clear();
            string[] files = Directory.GetFiles(CounterForm.MapsDir());
            Array.Sort(files);
            foreach (string f in files)
            {
                string name = Path.GetFileName(f);
                Image full = null, thumb = null;
                try
                {
                    full = Image.FromFile(f);
                    thumb = CounterForm.ScaleImg(full, 104);
                }
                catch { }
                finally { if (full != null) full.Dispose(); }
                if (thumb == null) continue;
                thumbs.Add(thumb);

                Panel p = new Panel();
                p.Size = new Size(116, 116);
                p.Margin = new Padding(5);
                p.BackColor = Color.FromArgb(28, 28, 34);
                p.Tag = name;
                p.Paint += new PaintEventHandler(delegate(object s, PaintEventArgs e)
                {
                    Graphics gg = e.Graphics;
                    gg.SmoothingMode = SmoothingMode.AntiAlias;
                    gg.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    bool sel = (string)((Panel)s).Tag == selected;
                    gg.DrawImage(thumb, 6, 6, 104, 104);
                    using (Pen pen = new Pen(sel ? GoldText : Color.FromArgb(70, 70, 80), sel ? 2F : 1F))
                        gg.DrawRectangle(pen, 1, 1, 113, 113);
                });
                p.Click += new EventHandler(delegate(object s, EventArgs e)
                {
                    selected = (string)((Panel)s).Tag;
                    owner.mapSel = selected;
                    owner.PersistState();
                    if (owner.IsMapVisible()) owner.MapViewerLoadCurrent();
                    flow.Invalidate(true);
                });
                flow.Controls.Add(p);
            }
        }

        void AddPhoto()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = L.T("addPhoto");
                d.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp";
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string name = Path.GetFileName(d.FileName);
                string dst = Path.Combine(CounterForm.MapsDir(), name);
                int n = 2;
                while (File.Exists(dst))
                {
                    dst = Path.Combine(CounterForm.MapsDir(),
                        Path.GetFileNameWithoutExtension(name) + " (" + n + ")" + Path.GetExtension(name));
                    n++;
                }
                File.Copy(d.FileName, dst);
                selected = Path.GetFileName(dst);
                owner.mapSel = selected;
                BuildThumbs();
                if (owner.IsMapVisible()) owner.MapViewerLoadCurrent();
                UpdateToggle();
                owner.PersistState();
            }
        }

        void DeleteSelected()
        {
            if (selected == null || selected.Length == 0 || !File.Exists(Path.Combine(CounterForm.MapsDir(), selected)))
            {
                MessageBox.Show(L.T("selectFirst"));
                return;
            }
            if (MessageBox.Show(L.T("delConfirm"), L.T("delTitle"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            owner.MapViewerReleaseIfCurrent(selected);
            try { File.Delete(Path.Combine(CounterForm.MapsDir(), selected)); }
            catch { }
            selected = null;
            owner.mapSel = "";
            BuildThumbs();
            if (owner.IsMapVisible()) owner.MapViewerLoadCurrent();
            UpdateToggle();
            owner.PersistState();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            topTimer.Stop();
            foreach (Image t in thumbs) t.Dispose();
            thumbs.Clear();
        }
    }

    // ---------- мастер первой настройки ----------

    public class SetupWizardForm : Form
    {
        CounterForm owner;
        TextBox numBox;
        IconButton btnRu, btnEn, btnWizInc, btnWizDec, btnStart;
        int captureAction = -1;   // 0 → +1, 1 → −1
        Font headerFont;
        Font smallFont;

        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        [DllImport("user32.dll")] static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int w, int h);
        [DllImport("user32.dll")] static extern bool ShowCaret(IntPtr hWnd);
        [DllImport("gdi32.dll")] static extern IntPtr CreateSolidBrush(uint color);

        public SetupWizardForm(CounterForm owner)
        {
            this.owner = owner;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(24, 24, 30);
            DoubleBuffered = true;
            KeyPreview = true;
            Size = new Size(540, 534);
            UpdateRegion();

            headerFont = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            smallFont = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);

            btnRu = new IconButton("", "RU", false);
            btnRu.Bounds = new Rectangle(14, 82, 120, 44);
            btnRu.Click += delegate { SetLang("ru"); };

            btnEn = new IconButton("", "EN", false);
            btnEn.Bounds = new Rectangle(142, 82, 120, 44);
            btnEn.Click += delegate { SetLang("en"); };

            numBox = new TextBox();
            numBox.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            numBox.BorderStyle = BorderStyle.None;
            numBox.BackColor = Color.FromArgb(24, 24, 30);
            numBox.ForeColor = CounterForm.GoldText;
            numBox.TextAlign = HorizontalAlignment.Center;
            numBox.Text = "0";
            numBox.Bounds = new Rectangle(22, 174, 164, 26);
            numBox.KeyPress += new KeyPressEventHandler(NumKeyPress);
            numBox.GotFocus += new EventHandler(delegate
            {
                IntPtr brush = CreateSolidBrush(0x004DD3FF);
                CreateCaret(numBox.Handle, brush, 2, numBox.Height);
                ShowCaret(numBox.Handle);
            });

            btnWizInc = new IconButton("", "+1 — F8", false);
            btnWizInc.Bounds = new Rectangle(14, 262, 160, 44);
            btnWizInc.Click += delegate { StartCapture(0); };

            btnWizDec = new IconButton("", "−1 — F9", false);
            btnWizDec.Bounds = new Rectangle(182, 262, 160, 44);
            btnWizDec.Click += delegate { StartCapture(1); };

            btnStart = new IconButton(FA.Check, L.T("start"), true);
            btnStart.Bounds = new Rectangle(14, 470, 512, 48);
            btnStart.Click += delegate { Finish(); };

            Controls.AddRange(new Control[] { numBox, btnRu, btnEn, btnWizInc, btnWizDec, btnStart });

            Paint += new PaintEventHandler(WizardPaint);
            KeyDown += new KeyEventHandler(WizardKeyDown);
            Shown += new EventHandler(delegate
            {
                numBox.Location = new Point(22, 172 + (44 - numBox.Height) / 2);
                numBox.Focus();
            });
            MouseDown += new MouseEventHandler(delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            });

            // пока мастер открыт — глобальные клавиши счётчика спят
            owner.UnregisterAll();
            FormClosed += new FormClosedEventHandler(delegate { owner.RegisterAll(); });
            UpdateWizardTexts();
        }

        void NumKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
        }

        void SetLang(string lang)
        {
            L.Lang = lang;
            UpdateWizardTexts();
        }

        void UpdateWizardTexts()
        {
            btnRu.Primary = L.Lang == "ru";
            btnEn.Primary = L.Lang == "en";
            btnStart.Label = L.T("start");
            UpdateWizardButtons();
            Invalidate();
        }

        void UpdateWizardButtons()
        {
            if (captureAction == 0)
            {
                btnWizInc.Glyph = "";
                btnWizInc.Label = L.T("pressKey");
            }
            else
            {
                btnWizInc.Glyph = FA.ArrowUp;
                btnWizInc.Label = "+1 — " + owner.ComboName(owner.hkInc);
            }
            if (captureAction == 1)
            {
                btnWizDec.Glyph = "";
                btnWizDec.Label = L.T("pressKey");
            }
            else
            {
                btnWizDec.Glyph = FA.ArrowDown;
                btnWizDec.Label = "−1 — " + owner.ComboName(owner.hkDec);
            }
            btnWizInc.Invalidate();
            btnWizDec.Invalidate();
        }

        void StartCapture(int action)
        {
            captureAction = action;
            owner.UnregisterAll();
            UpdateWizardButtons();
        }

        void EndCapture()
        {
            captureAction = -1;
            owner.RegisterAll();
            UpdateWizardButtons();
        }

        void Finish()
        {
            string t = numBox.Text.Trim().Replace(" ", "").Replace("\u00A0", "");
            int v;
            if (int.TryParse(t, out v) && v >= 0)
                owner.count = v;
            DialogResult = DialogResult.OK;
            Close();
        }

        void WizardKeyDown(object sender, KeyEventArgs e)
        {
            if (captureAction < 0) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            Keys k = e.KeyCode;
            if (k == Keys.ShiftKey || k == Keys.ControlKey || k == Keys.Menu
                || k == Keys.LWin || k == Keys.RWin) return;
            if (k == Keys.Escape)
            {
                EndCapture();
                return;
            }
            int mod = 0;
            if (e.Control) mod |= 0x2;
            if (e.Alt) mod |= 0x1;
            if (e.Shift) mod |= 0x4;
            int packed = (mod << 16) | (int)k;
            bool dup = (captureAction != 0 && packed == owner.hkInc)
                    || (captureAction != 1 && packed == owner.hkDec);
            if (dup)
            {
                EndCapture();
                return;
            }
            if (captureAction == 0) owner.hkInc = packed;
            else owner.hkDec = packed;
            EndCapture();
        }

        void DrawHeader(Graphics g, string glyph, string text, int y, bool withLine, int lineEnd)
        {
            int x = 14;
            if (CounterForm.FaFamily != null)
            {
                using (Font gf = new Font(CounterForm.FaFamily, 11F, FontStyle.Regular))
                {
                    float gh = gf.GetHeight(g);
                    using (SolidBrush b = new SolidBrush(CounterForm.GoldText))
                        g.DrawString(glyph, gf, b, x, y + (20 - gh) / 2);
                    x += (int)g.MeasureString(glyph, gf).Width + 7;
                }
            }
            TextRenderer.DrawText(g, text, headerFont, new Point(x, y + 1), CounterForm.GoldText);
            if (withLine)
            {
                int tw = TextRenderer.MeasureText(text, headerFont).Width;
                using (Pen p = new Pen(Color.FromArgb(90, CounterForm.GoldBorder), 1F))
                    g.DrawLine(p, x + tw + 10, y + 12, lineEnd, y + 12);
            }
        }

        void WizardPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle card = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath frame = CounterForm.RoundedPath(card, 14))
            {
                g.SetClip(frame);
                Rectangle bottom = new Rectangle(0, Height - 6, Width, 6);
                using (LinearGradientBrush gb = new LinearGradientBrush(bottom, Color.FromArgb(255, 205, 60), Color.FromArgb(255, 150, 0), 90F))
                    g.FillRectangle(gb, bottom);
                g.ResetClip();
                using (LinearGradientBrush bg = new LinearGradientBrush(card, Color.FromArgb(32, 32, 40), Color.FromArgb(12, 12, 16), 90F))
                    g.FillPath(bg, frame);
                using (Pen bp = new Pen(CounterForm.GoldBorder, 1.6F))
                    g.DrawPath(bp, frame);
            }

            DrawHeader(g, FA.Gear, L.T("firstSetup"), 14, true, Width - 28);

            // 1. язык
            DrawHeader(g, "\uF1AB", L.T("language"), 56, true, Width - 28);
            // 2. сколько открыто
            DrawHeader(g, "\uF466", L.T("opened"), 146, true, Width - 28);
            using (Pen p = new Pen(Color.FromArgb(150, CounterForm.GoldBorder), 1F))
            using (GraphicsPath gp = CounterForm.RoundedPath(new Rectangle(14, 172, 180, 44), 10))
                g.DrawPath(p, gp);
            TextRenderer.DrawText(g, L.T("openedHint"), smallFont,
                new Rectangle(210, 172, 316, 44), Color.FromArgb(150, 150, 162),
                TextFormatFlags.VerticalCenter);
            // 3. клавиши
            DrawHeader(g, FA.Keyboard, L.T("keysTitle"), 236, true, Width - 28);
            TextRenderer.DrawText(g,
                captureAction >= 0
                    ? L.F("hintCombo", captureAction == 0 ? "+1" : "-1")
                    : L.T("keyHintIdle"),
                smallFont, new Rectangle(14, 314, 512, 18),
                captureAction >= 0 ? CounterForm.GoldText : Color.FromArgb(150, 150, 162),
                TextFormatFlags.VerticalCenter);

            using (Pen dp = new Pen(Color.FromArgb(58, 58, 68), 1F))
                g.DrawLine(dp, 14, 334, Width - 14, 334);

            // подсказки
            TextRenderer.DrawText(g, L.T("hintGen1"), smallFont, new Rectangle(14, 344, 512, 18), Color.FromArgb(150, 150, 162),
                TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, L.T("hintGen2"), smallFont, new Rectangle(14, 366, 512, 18), Color.FromArgb(150, 150, 162),
                TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(g, L.T("hintGen3"), smallFont, new Rectangle(14, 388, 512, 18), Color.FromArgb(150, 150, 162),
                TextFormatFlags.VerticalCenter);
        }

        void UpdateRegion()
        {
            Rectangle r = new Rectangle(0, 0, Width, Height);
            using (GraphicsPath gp = CounterForm.RoundedPath(r, 14))
                Region = new Region(gp);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84) // WM_NCHITTEST — всё клиентское, таскание через MouseDown
                m.Result = (IntPtr)1;
            base.WndProc(ref m);
        }
    }

    static class Program
    {
        static Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            mutex = new Mutex(true, "FortniteChestCounter_SingleInstance", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show(L.T("alreadyRun"));
                return;
            }
            bool openMaps = false;
            string[] a = Environment.GetCommandLineArgs();
            if (a.Length > 1 && a[1].TrimStart('/', '-').ToLowerInvariant() == "maps") openMaps = true;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CounterForm(openMaps));
        }
    }
}
