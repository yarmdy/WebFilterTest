using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace WebFilterTest
{
    public class ColorListBox:ListBox
    {
        public KeywordList Keywords { get; }

        public ColorListBox() {
            DrawMode = DrawMode.OwnerDrawFixed;
            Keywords=new KeywordList();
            Keywords.OnChange += Keywords_OnChange;
        }

        private void Keywords_OnChange(object sender)
        {
            //this.Visible = this.Visible;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);
            if (e.Index < 0) return;
            var state = e.State;

            var bitmap = new Bitmap(e.Bounds.Width,e.Bounds.Height);
            var gg = Graphics.FromImage(bitmap);

            
            gg.FillRectangle(new SolidBrush(e.BackColor), new RectangleF(0,0,e.Bounds.Width,e.Bounds.Height));
            var str = Items[e.Index].ToString();

            Dictionary<int, object> dic = new Dictionary<int, object>();
            foreach (var s in Keywords) {
                //var index = str.IndexOf(s);
                var regstr = fixRegStr(s);
                var reg = new Regex(regstr, RegexOptions.IgnoreCase|RegexOptions.Singleline);
                var matches = reg.Matches(str);
                if (matches.Count<=0) continue;
                
                foreach (Match math in matches) {
                    for (int i = 0; i < s.Length; i++)
                    {
                        dic[math.Index + i] = null;
                    }
                }
            }
            var strarr = str.Select(a=>a+"").ToList();
            float w = 0;
            var strindex = 0;
            var sf = (StringFormat)StringFormat.GenericTypographic.Clone();
            strarr.ForEach(a => {
                var font = (Font)e.Font.Clone();
                var contains = dic.ContainsKey(strindex++);
                if (contains)
                {
                    font = new Font(font.FontFamily, font.SizeInPoints, FontStyle.Bold|FontStyle.Underline);
                }
                var m = gg.MeasureString(a,font,e.Bounds.Width,sf);
                
                gg.DrawString(a,font,new SolidBrush(contains ? Color.Red: e.ForeColor),new PointF(w,0));
                w += m.Width;
            });

            var g = e.Graphics;

            g.DrawImage(bitmap,e.Bounds);

            bitmap.Dispose();
        }

        Regex fixReg = new Regex(@"([\^\$\\\*\+\?\{\}\[\]\(\)\.\<\>\=\:\!\|\'])",RegexOptions.Singleline);
        Regex fixReg2 = new Regex(@"""",RegexOptions.Singleline);
        private string fixRegStr(string s) {
            return fixReg2.Replace(fixReg.Replace(s,@"\$1"),@"""$1");
        }
        public class KeywordList : IList<string> {
            private List<string> keywords = new List<string>();

            public string this[int index] { get => keywords[index]; set { keywords[index] = value; triggerChange(this); } }

            public int Count => keywords.Count;

            public bool IsReadOnly => false;

            public void Add(string item)
            {
                keywords.Add(item);
                triggerChange(this);
            }
            public void AddRange(IEnumerable<string> collection) {
                keywords.AddRange(collection);
                triggerChange(this);
            }

            public void Clear()
            {
                keywords.Clear();
                triggerChange(this);
            }

            public bool Contains(string item)
            {
                return keywords.Contains(item);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                keywords.CopyTo(array, arrayIndex);
            }

            public IEnumerator<string> GetEnumerator()
            {
                return keywords.GetEnumerator();
            }

            public int IndexOf(string item)
            {
                return keywords.IndexOf(item);
            }

            public void Insert(int index, string item)
            {
                keywords.Insert(index, item);
                triggerChange(this);
            }

            public bool Remove(string item)
            {
                var res=keywords.Remove(item);
                if (res)
                {
                    triggerChange(this);
                }
                return res;
            }

            public void RemoveAt(int index)
            {
                keywords.RemoveAt(index);
                triggerChange(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return keywords.GetEnumerator();
            }
            /// <summary>
            /// 修改事件
            /// </summary>
            /// <param name="sender">控件</param>
            public delegate void KeywordListChanged(object sender);
            public event KeywordListChanged OnChange;

            private void triggerChange(object sender) {
                if (OnChange == null)
                {
                    return;
                }
                OnChange(sender);
            }
            
        }
    }

    
}
