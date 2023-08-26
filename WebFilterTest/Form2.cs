using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace WebFilterTest
{
    public partial class Form2 : Form
    {
        public string Url { get; set; }
        //ChromiumWebBrowser chromiumWebBrowser = null;
        public Form2(string title,string url)
        {
            InitializeComponent();
            initCef();
            Text = title;
            Url = url;

            textBox1.Text = url;
            AddBrowser(url);
        }

        public void ChromiumWebBrowser_TitleChanged(object sender, TitleChangedEventArgs e)
        {
            if (IsDisposed) { return; };
            //setText(e.Title, null);
            var tabpage = (TabPage)((ChromiumWebBrowser)sender).Parent;
            if (tabpage == null) return;
            setTabText(tabpage,e.Title,null);
            var tab = (TabControl)tabpage.Parent;
            if (textBox1.Invoke(new Func<TabPage>(() => tab.SelectedTab)) != tabpage) return;
            setText(e.Title, null);
        }

        public void ChromiumWebBrowser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            if(IsDisposed){ return; }
            //setText(null,e.Address);
            var tabpage = (TabPage)((ChromiumWebBrowser)sender).Parent;
            if (tabpage == null) return;
            setTabText(tabpage, null, e.Address);
            var tab = (TabControl)tabpage.Parent;
            if (textBox1.Invoke(new Func<TabPage>(()=> tab.SelectedTab)) != tabpage) return;
            setText(null, e.Address);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 13) return;
            var tabPage = tabControl1.SelectedTab;
            if (tabPage == null) {
                tabPage = AddBrowser(textBox1.Text);
            }
            var controls = tabPage.Controls.Find("browser",false);
            if (controls.Length <= 0) {
                return;
            }
            ((ChromiumWebBrowser)controls[0]).LoadUrl(textBox1.Text);
        }
        private void initCef() {
            //var settings = new CefSettings();
            //settings.CefCommandLineArgs.Add("enable-media-stream", "enable-media-stream");
            //settings.CefCommandLineArgs.Add("enable-system-flash", "1");
            //settings.CefCommandLineArgs.Add("ppapi_flash_version", "29.0.0.140");

            //settings.CefCommandLineArgs.Add("ppapi_flash_path", AppDomain.CurrentDomain.BaseDirectory+"pepflashplayer.dll");
            //Cef.Initialize(settings);
        }
        bool cookieSetted=false;
        public TabPage AddBrowser(string url) { 
            var tabPage = new TabPage(url);
            tabPage.Tag = url;
            var browser1 = new ChromiumWebBrowser(url);
            if (!cookieSetted)
            {
                var cookieM = Cef.GetGlobalCookieManager();
                cookieM.SetCookie(url, new Cookie { Domain = Conf.Domain, Name = Conf.CookieName, Value = Conf.CookieValue });
                cookieSetted = true;
            }
            browser1.Name = "browser";
            browser1.AddressChanged += ChromiumWebBrowser_AddressChanged;
            browser1.TitleChanged += ChromiumWebBrowser_TitleChanged;
            browser1.LifeSpanHandler = new MyLifeSpan();
            tabPage.Controls.Add(browser1);
            tabControl1.TabPages.Add(tabPage);

            return tabPage;
        }

        delegate void SetTextDelegate(string title,string url);
        void setText(string title, string url) {
            if (textBox1.InvokeRequired)
            {
                SetTextDelegate settxt = new SetTextDelegate(setText);
                Invoke(settxt, title,url);
            }
            else {
                textBox1.Text = url?? textBox1.Text;
                Text = title?? Text;
            }
        }
        delegate void SetTabTextDelegate(Control control,string title, string url);
        void setTabText(Control control, string title, string url)
        {
            if (textBox1.InvokeRequired)
            {
                SetTabTextDelegate settxt = new SetTabTextDelegate(setTabText);

                Invoke(settxt,control, title,url);
            }
            else
            {
                control.Text = title?? control.Text;
                control.Tag = url ?? control.Tag;
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tabpage = (TabPage)((TabControl)sender).SelectedTab;
            setText(tabpage?.Text??"空白页",tabpage?.Tag+"");
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var bitmap = new Bitmap(e.Bounds.Width, e.Bounds.Height);
            var gg = Graphics.FromImage(bitmap);


            gg.FillRectangle(new SolidBrush(Color.White), new RectangleF(0, 0, e.Bounds.Width, e.Bounds.Height));
            var tab = ((TabControl)sender);
            var str = tab.TabPages[e.Index].Text;
            var isActive = tab.SelectedIndex == e.Index;
            var drawRect = new RectangleF(isActive?tab.Padding.X: tab.Padding.X/2, isActive?tab.Padding.Y: tab.Padding.Y/2, isActive?e.Bounds.Width - tab.Padding.X * 2:e.Bounds.Width, isActive?e.Bounds.Height - tab.Padding.Y * 2:e.Bounds.Height);
            gg.DrawString(str,e.Font,new SolidBrush(Color.Black), drawRect);
            using (var epath = new GraphicsPath()) { 
                epath.AddEllipse((float)e.Bounds.Width-e.Bounds.Height,e.Bounds.Height/4f,e.Bounds.Height/2f, e.Bounds.Height / 2f);
                gg.FillPath(new SolidBrush(Color.Red),epath);
            }
            gg.DrawLine(new Pen(new SolidBrush(Color.White)), (float)e.Bounds.Width - e.Bounds.Height, e.Bounds.Height / 4f, (float)e.Bounds.Width - e.Bounds.Height + e.Bounds.Height / 2f, e.Bounds.Height / 4f + e.Bounds.Height / 2f);
            gg.DrawLine(new Pen(new SolidBrush(Color.White)), (float)e.Bounds.Width - e.Bounds.Height + e.Bounds.Height / 2f, e.Bounds.Height / 4f, (float)e.Bounds.Width - e.Bounds.Height, e.Bounds.Height / 4f + e.Bounds.Height / 2f);

            e.Graphics.DrawImage(bitmap, e.Bounds);

            bitmap.Dispose();
        }


        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            removeAll(this);
            Url = null;
        }
        private void removeAll(Control control) {
            
            if (control==null || control.IsDisposed) return;

            foreach (Control c in control.Controls) {
                removeAll(c);
            }
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menu = (ContextMenuStrip)((ToolStripItem)sender).Owner;
            var bounds = menu.Bounds;

            for (int i = 0; i < tabControl1.TabCount; i++) {
                var tbounds = tabControl1.GetTabRect(i);
                tbounds.Offset(tabControl1.PointToScreen(new Point(0,0)));
                if (!tbounds.Contains(bounds.X, bounds.Y)) continue;
                var item = tabControl1.TabPages[i];
                var isLocal = tabControl1.SelectedTab == item;
                removeAll(item);
                item.Dispose();
                if (isLocal)
                {
                    var index = Math.Min(i, tabControl1.TabCount - 1);
                    tabControl1.SelectedIndex = index;
                }
                break;
            }
        }

        private void tabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                var tbounds = tabControl1.GetTabRect(i);
                var tboundsf = new RectangleF((float)tbounds.Width - tbounds.Height+tbounds.X, tbounds.Height / 4f+tbounds.Y, tbounds.Height / 2f, tbounds.Height / 2f);
                if (!tboundsf.Contains(e.X, e.Y)) continue;
                var item = tabControl1.TabPages[i];
                var isLocal = tabControl1.SelectedTab==item;
                removeAll(item);
                item.Dispose();

                if (isLocal)
                {
                    var index = Math.Min(i,tabControl1.TabCount-1);
                    tabControl1.SelectedIndex = index;
                }
                
                break;
            }
        }

        public void TabRemoveIndex(int index) {
            if(tabControl1.TabPages.Count<=index) return;
            var item = tabControl1.TabPages[index];
            removeAll(item);
            item.Dispose();
        }
    }

    class MyLifeSpan : ILifeSpanHandler
    {
        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            return true;
        }

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            
        }

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            
        }

        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null;
            if (!userGesture) goto donothing;
            var tab = (TabControl)((Control)chromiumWebBrowser).Parent.Parent;
            tab.Invoke(new Action(()=>{
                var form2 = (Form2)tab.Parent;
                form2.AddBrowser(targetUrl);
            }));
            

        donothing:
            return true;
        }
    }
}
