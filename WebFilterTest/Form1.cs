using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Web;
using System.Threading;
using log4net;

namespace WebFilterTest
{
    public partial class Form1 : Form
    {
        ILog log=LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            var obj = (ListObj)listBox1.SelectedItem;
            Clipboard.SetText(obj.Url);
        }

        Regex regmain = new Regex(@"\<tbody[^\>]+?id\=""normalthread.+?\</tbody\>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Regex reg = new Regex(@"<em>\[\<a.+?\>(.+?)\<\/a\>.*?</em>.*?<a\s+href=""(.+?)"".*?>(.+?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Regex regtag = new Regex(@"\[.*?\]", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Regex regzh = new Regex(@"[\u4e00-\u9fa5]{4,}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        Regex regpage = new Regex(@"共\s.*?([0-9]+)\s.*?页", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        int pages = 0;
        int maxpage = 0;
        //var requrl = "https://ghjhgjytty.live/forum.php?mod=forumdisplay&fid=103&typeid=481&typeid=481&filter=typeid&page={0}";
        //string requrl = "https://ghjhgjytty.live/forum.php?mod=forumdisplay&fid=2&typeid=684&filter=typeid&typeid=684&page={0}";
        //string requrl = "https://ghjhgjytty.live/forum.php?mod=forumdisplay&fid=2&typeid=684&filter=typeid&typeid=684&page={0}";
        AutoResetEvent are = new AutoResetEvent(true);
        AutoResetEvent are2 = new AutoResetEvent(true);
        Dictionary<int, List<ListObj>> dicObj = new Dictionary<int, List<ListObj>>();

        DateTime dt = DateTime.Now;

        List<ListObj> LocalList = new List<ListObj>();

        int getpage() {
            var res = 0;
            are.WaitOne();
            if (pages < maxpage) {
                res = (++pages);
                //SetProssHandle(pages,maxpage);
            }
            are.Set();
            return res;
        }
        void addObj(int page, List<ListObj> obj) {
            are2.WaitOne();
            dicObj[page] = obj;
            SetProssHandle(dicObj.Count, maxpage);
            if (dicObj.Count == maxpage) {
                Invoke(new Action(() => {
                    var arr = dicObj.OrderBy(a => a.Key).SelectMany(a => a.Value).ToArray();
                    LocalList = arr.ToList();
                    listBox1.Items.AddRange(arr );
                    label1.Text = (DateTime.Now - dt).TotalSeconds+"";
                    label3.Text = arr.Length+"";
                }));
            }
            are2.Set();
        }
        void dofenxi(object thid) {
            int doCount = 0;
            var strId = thid.ToString().PadLeft(2,'0');
            log.Info($"【创建】线程{strId}");
            var localPage = 0;
            var errorCount = 0;
            while (true) {
                log.Info($"【开始】线程{strId}====START");
                var page = 0;
                if (localPage > 0) {
                    page = localPage;
                }
                else
                {
                    page = getpage();
                }
                log.Info($"【获得】线程{strId}========GET(page:{page})");
                if (page <= 0) break;

                var thislist = new List<ListObj>();

                try
                {
                    var res = HttpGet(String.Format(textBox1.Text, page));
                    log.Info($"【数据】线程{strId}============DATA(page:{page})");
                    var str = res.docstr;

                    var mathes = regmain.Matches(str);
                    foreach (Match math in mathes)
                    {
                        var mathres = reg.Match(math.Value);
                        if (!mathres.Success) continue;
                        var tag = mathres.Groups[1].Value;
                        var aurl = $"{res.req.ResponseUri.Scheme}://{res.req.ResponseUri.Host}{(!new[] { 80, 443 }.Contains(res.req.ResponseUri.Port) ? (res.req.ResponseUri.Port + "") : "")}/{HttpUtility.HtmlDecode(mathres.Groups[2].Value)}";
                        var atitle = mathres.Groups[3].Value;

                        if (!regzh.IsMatch(regtag.Replace(atitle, ""))) continue;

                        thislist.Add(new ListObj { Title = $"[{tag}]{atitle}", Url = aurl });

                    }
                }
                catch (Exception ex)
                {
                    log.Info($"【异常】线程{strId}================================EXCEPTION(page:{page})", ex);
                    localPage = page;
                    errorCount++;
                    if (errorCount >= 10) {
                        localPage = 0;
                        errorCount = 0;
                        addObj(page, new List<ListObj>());
                    }
                    continue;
                }
                localPage = 0;
                errorCount = 0;
                addObj(page,thislist);
                log.Info($"【附加】线程{strId}================APPEND({strId}数量：{++doCount}(page:{page}))");
            }
            log.Info($"【结束】线程{strId}====================END({strId}总数：{doCount})");
        }

        private void button1_Click(object sender, EventArgs e)
        {


            var res = HttpGet(String.Format(textBox1.Text, 1));
            var str = res.docstr;
            var pageres = regpage.Match(str);
            if (!pageres.Success) return;
            if (!int.TryParse(pageres.Groups[1].Value, out maxpage)) return;
            pages = 0;
            dicObj = new Dictionary<int, List<ListObj>>();
            listBox1.Items.Clear();
            //SetProssHandle(pages,maxpage);

            dt=DateTime.Now;

            int thid = 0;
            new int[(int)numericUpDown1.Value].Select(a => {
                var task = new Task(new Action<object>(dofenxi),++thid);
                task.Start();
                return task;
            }).ToList();

            //listBox1.Items.AddRange(dicObj.OrderBy(a => a.Key).Select(a => a.Value).ToArray());
        }

        (string docstr, HttpWebResponse req) HttpGet(string url)
        {
            var http = (HttpWebRequest)WebRequest.Create(url);
            http.Method = "GET";
            http.Accept = "*/*";
            http.AllowAutoRedirect = false;
            var req = (HttpWebResponse)http.GetResponse();
            var stream = req.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            var res = sr.ReadToEnd();
            stream.Dispose();
            sr.Dispose();
            //req.Dispose();
            return (res, req);
        }

        List<ListObj> listObjs = new List<ListObj>();
        class ListObj {
            public string Title { get; set; }
            public string Url { get; set; }

            public override string ToString()
            {
                return Title;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count <= 0) return;
            StringBuilder sb = new StringBuilder();
            foreach (ListObj item in listBox1.Items) {
                sb.Append($"{item.Title}||*||{item.Url}\r\n");
            }

            File.WriteAllText(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "data.txt"), sb.ToString(), Encoding.UTF8);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var datafile = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "data.txt");
            if (!File.Exists(datafile)) { return; }

            listBox1.Items.Clear();
            var txt = File.ReadAllText(datafile);

            var arr = txt.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            LocalList.Clear();
            foreach (var item in arr) {
                var cols = item.Split(new[] { "||*||" }, StringSplitOptions.None);
                var obj = new ListObj { Title = cols[0], Url = cols[1] };
                listBox1.Items.Add(obj);
                LocalList.Add(obj);
            }
            label3.Text = listBox1.Items.Count + "";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem == null) return;
            var obj = (ListObj)listBox1.SelectedItem;
            openWeb(obj.Title, obj.Url);
        }

        Form2 noOpen = null;
        private void openWeb(string title, string url) {
            if (noOpen == null) {
                var form2 = new Form2(title, url);
                form2.WindowState = FormWindowState.Maximized;
                form2.Show();
                return;
            }
            noOpen.WindowState = FormWindowState.Maximized;
            noOpen.Show();
            noOpen.Opacity = 1;
            noOpen = null;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (listBox1.SelectedItem == null) return;

            var obj = (ListObj)listBox1.SelectedItem;
            var nourl = noOpen?.Url;
            var isDisposed = noOpen?.IsDisposed ?? true;
            var noVisble = noOpen?.Visible??false;
            if (isDisposed || !noVisble) {
                noOpen?.Close();
                noOpen = new Form2(obj.Title, obj.Url);
            }
            noOpen.Opacity = 0;
            var noState = noOpen.WindowState;
            noOpen.WindowState = FormWindowState.Maximized;
            noOpen.Show();
            if (obj.Url == nourl)
            {
                noOpen.Opacity = 1;
                return;
            }


            if (!noVisble)
            {
                noOpen.Visible = false;
                return;
                
            }
            noOpen.TabRemoveIndex(0);
            noOpen.AddBrowser(obj.Url);
            noOpen.Url = obj.Url;
            
            noOpen.Opacity = 1;
            //noOpen.WindowState = noState;

            //if (obj.Url == noOpen?.Url) {
            //    noOpen.WindowState = FormWindowState.Maximized;
            //    noOpen.Show();
            //    noOpen.Opacity = 1;
            //    noOpen = null;
            //    return;
            //};
            //if (noOpen != null) {
            //    noOpen.Dispose();
            //}
            //noOpen = new Form2(obj.Title, obj.Url);
            //noOpen.Opacity = 0;
            //noOpen.WindowState = FormWindowState.Maximized;
            //noOpen.Show();
            //noOpen.Hide();
        }

        delegate void SetPross(int value, int max);

        void SetProssHandle(int value, int max) {
            if (InvokeRequired)
            {
                SetPross setp = new SetPross(SetProssHandle);
                this.Invoke(setp, value, max);
            }
            else {
                progressBar1.Maximum = max;
                progressBar1.Value = value;

                label2.Text = $"{value}/{max}";
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            search();
        }

        List<string> getKeys(string str) {
            var list = new List<string>();
            var arr = str.Select(a => a + "").ToArray();
            list.AddRange(arr);

            if (str.Length > 1) {
                list.Add(str);
            }
            if (str.Length <= 2) {
                goto res;
            }

            for (int i = 0; i < str.Length; i++) {
                for (int j = 2; j <= str.Length - i; j++) {
                    list.Add(str.Substring(i,j));
                }
            }

            res:
            return list.Distinct().ToList();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            search();
        }

        private void search() {
            if (LocalList.Count <= 0)
            {
                label3.Text = 0 + "";
                return;
            }

            var text = textBox2.Text.ToLower().Trim();
            if (text == "")
            {
                listBox1.Keywords.Clear();
                listBox1.Items.Clear();
                listBox1.Items.AddRange(LocalList.ToArray());
                label3.Text = LocalList.Count + "";
                return;
            }
            var keys = checkBox1.Checked?new List<string> { text} : getKeys(text);

            int index = 0;
            var tempList = LocalList.Select(a => {
                var fen = keys.Sum(b => a.Title.ToLower().Contains(b) ? b.Length : 0);
                return (index: index++, fen: fen, a);
            }).Where(a => a.fen > 0).OrderByDescending(a => a.fen).ThenBy(a => a.index).Select(a => a.a).ToArray();
            listBox1.Keywords.Clear();
            listBox1.Keywords.AddRange(keys);
            listBox1.Items.Clear();
            listBox1.Items.AddRange(tempList);
            label3.Text = tempList.Length + "";
        }
    }
}
