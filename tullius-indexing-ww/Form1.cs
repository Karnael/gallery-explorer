using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tulius_archive_ww;

namespace tullius_indexing_ww
{
    public partial class Form1 : Form
    {
        protected ChromeDriverService _driverService = null;
        protected ChromeOptions _options = null;
        protected ChromeDriver _driver = null;

        public Form1()
        {
            InitializeComponent();

            _driverService = ChromeDriverService.CreateDefaultService();
            _driverService.HideCommandPromptWindow = true;

            _options = new ChromeOptions();
            _options.AddArgument("disable-gpu");
        }

        static public string COOKIES = "";
        static public string SESS = "";

        private void richTextBox_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
        }

        void append(string ww) =>
            Extends.Post(this, () => richTextBox1.AppendText(ww + "\r\n"));
        void status(string ww) => Extends.Post(this, () => label6.Text = ww);

        private void login()
        {
            append("로그인 요청 시작 id=" + textBox1.Text + ", pwd=" + textBox2.Text);
            _driver = new ChromeDriver(_driverService, _options);
            _driver.Navigate().GoToUrl("https://www.dcinside.com/");

            Thread.Sleep(500);

            var element = _driver.FindElementByXPath("//input[@id='user_id']");
            element.SendKeys(textBox1.Text);
            append("Append userid to //input[@id='user_id']");

            element = _driver.FindElementByXPath("//input[@id='pw']");
            element.SendKeys(textBox2.Text);
            append("Append userpwd to //input[@id='user_id']");

            element = _driver.FindElementByXPath("//button[@id='login_ok']");
            element.Click();
            append("Remote login button click");

            Thread.Sleep(500);

            try
            {
                var x = _driver.Manage().Cookies;
                COOKIES = string.Join("; ", x.AllCookies.Select(y => y.Name + "=" + y.Value));
                append("쿠키: " + COOKIES);
                SESS = x.AllCookies.First(y => y.Name == "PHPSESSID").Value;
                append("세션쿠키: " + SESS);
                if (SESS != "")
                    append("로그인 성공");
                _driver.Close();
                _driver.Dispose();
                _driverService.Dispose();
            }
            catch (Exception e)
            {
                append("예외: " + e.Message);
                if (SESS != "")
                    append("로그인 실패");
                _driver.Close();
                _driver.Dispose();
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            textBox1.Enabled = false;
            textBox2.Enabled = false;

            await Task.Run(() => login());
            if (SESS != "")
            {
                button1.Enabled = false;
                label6.Text = "로그인 성공. 시작 버튼을 누르세요";
            }
            else
            {
                textBox1.Enabled = true;
                textBox2.Enabled = true;

                label6.Text = "로그인 실패. 다시 시도해주세요";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            Task.Run(() => start());
            //var wc = NetCommon.GetDefaultClient();
            //wc.Headers.Add("X-Requested-With", "XMLHttpRequest");
            //wc.QueryString.Add("under_name", un);
            //var subhtml = Encoding.UTF8.GetString(wc.UploadValues("https://gall.dcinside.com/ajax/minor_ajax/get_under_gall", "POST", wc.QueryString));

            //var task = NetTask.MakeDefault("https://gall.dcinside.com/mgallery/board/lists/?id=github&page=1");
            //task.Cookie = "PHPSESSID=" + SESS;
            //var xx = NetTools.DownloadString(task);
        }

        private void start()
        {
            int ps;
            if (!int.TryParse(textBox3.Text, out ps))
            {
                append("숫자만 입력해주세요.");
                return;
            }
            int pe;
            if (!int.TryParse(textBox4.Text, out pe))
            {
                append("숫자만 입력해주세요.");
                return;
            }

            var id = "monmusu";

            var starts = ps;

            status("진행중...[0/" + (pe - ps + 1).ToString("#,#") + "]");
            bool real_cookie_receive = false;

            var articles = new List<DCInsidePageArticle>();

            try
            {
                for (; ps <= pe; ps++)
                {
                    string url;

                    if (true)
                        url = $"https://gall.dcinside.com/mgallery/board/lists/?id={id}&page={ps}";
                    else
                        url = $"https://gall.dcinside.com/board/lists/?id={id}&page={ps}";

                    Logger.Instance.Push("Downloading String... = " + url);
                    var task = NetTask.MakeDefault(url);
                    task.Cookie = COOKIES;
                    bool y = real_cookie_receive;
                    if (real_cookie_receive == false)
                    {
                        task.HeaderReceive = (ef) =>
                        {
                            append("헤더받음: " + ef);
                            var xx = ef.Split('\n').First(x => x.Contains("Set-Cookie")).Replace("Set-Cookie: ", "").Trim();
                            COOKIES = "PHPSESSID=" + SESS;
                            COOKIES += "; PHPSESSKEY=" + xx.Split(new[] { "PHPSESSKEY=" }, StringSplitOptions.None)[1].Split(';')[0].Trim();
                            COOKIES += $"; block_alert_{id}=1";
                            real_cookie_receive = true;
                            append("쿠키 변경됨: " + COOKIES);
                        };
                    }
                    var html = NetTools.DownloadString(task);
                    if (y == false)
                    {
                        append("PS1");
                        ps--;
                        continue;
                    }
                    Logger.Instance.Push("Downloaded String = " + url);
                    if (string.IsNullOrEmpty(html))
                    {
                        append("실패: " + url);
                        Logger.Instance.Push("Fail: " + url);
                        goto NEXT;
                    }
                    if (html.Length < 1000 && html.Contains("해당 마이너 갤러리는 운영원칙 위반으로 접근이 제한되었습니다."))
                    {
                        append("실패: 접근 거부, 접근 가능한 아이디로 재시도하시기 바랍니다.");
                        break;
                    }
                    if (html.Contains("해당 마이너 갤러리는 운영원칙 위반(사유: 누드패치, 성행위 패치, 음란성 게시물 공지 등록 및 정리 안됨) 으로 접근이 제한되었습니다."))
                    {
                        //goto FUCK;
                        return;
                    }

                    DCInsideGallery gall;

                    if (true)
                        gall = DCInsideUtils.ParseMinorGallery(html);
                    else
                        gall = DCInsideUtils.ParseGallery(html);

                    if (true && (gall.articles == null || gall.articles.Length == 0))
                        gall = DCInsideUtils.ParseGallery(html);

                    if (gall.articles.Length == 0)
                        break;

                    articles.AddRange(gall.articles);

                    Logger.Instance.Push("Parse: " + url);
                    // 해당 마이너 갤러리는 운영원칙 위반으로 접근이 제한되었습니다.\n마이너 갤러리 메인으로 돌아갑니다.

               NEXT:
                    var ss = TimeSpan.FromMilliseconds(720 * (pe - ps));
                    var yy = "";
                    if (ss.Days > 0)
                        yy += ss.Days.ToString() + "일 ";
                    if (ss.Days > 0 || ss.Hours > 0)
                        yy += ss.Hours.ToString() + "시간 ";
                    if (ss.Days > 0 || ss.Hours > 0 || ss.Minutes > 0)
                        yy += ss.Minutes.ToString() + "분 ";
                    if (ss.Days > 0 || ss.Hours > 0 || ss.Minutes > 0 || ss.Seconds > 0)
                        yy += ss.Seconds.ToString() + "초 남음";

                    status("진행중...[" + (ps - starts + 1).ToString() + "/" + (pe - starts + 1).ToString("#,#") + "] 남은시간: " + yy);
                    Logger.Instance.Push("next: " + url + $" || {ps}/{pe}/{starts}");
                    Thread.Sleep(700);
                }

                DCGalleryAnalyzer.Instance.Articles.AddRange(articles);
                DCGalleryAnalyzer.Instance.Save();

                status("완료");
            }
            catch (Exception e)
            {
                append("실패: " + e.Message + "\r\n" + e.StackTrace);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //DCGalleryAnalyzer.Instance.Open("툴리우스갤 데이터.txt");
        }
    }

    public static class Extends
    {
        public static T Send<T>(this Control control, Func<T> func)
            => control.InvokeRequired ? (T)control.Invoke(func) : func();

        public static void Post(this Control control, Action action)
        {
            if (control.InvokeRequired) control.BeginInvoke(action);
            else action();
        }

        public static int ToInt32(this string str) => Convert.ToInt32(str);
    }
}
