using CefSharp;
using CefSharp.Wpf;
using GalleryExplorer.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GalleryExplorer
{
    /// <summary>
    /// ArchiveViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ArchiveViewer : Window
    {
        ChromiumWebBrowser browser;
        ArticleColumnModel article;
        List<CommentColumnModel> comments;
        CommentViewer comment_viewer;

        const string gallname = "monmusu";

        public ArchiveViewer(string id)
        {
            InitializeComponent();

            browser = new ChromiumWebBrowser(string.Empty)
            {
                RequestHandler = new MyRequestHandler(),
            };
            browserContainer.Content = browser;
            article = DCInsideArchiveDB.Instance.QueryById(id);
            Title.Text = article.title;
            comments = DCInsideArchiveCommentDB.Instance.QueryById(id);

            if (comments != null && comments.Count != 0)
            {
                comment_viewer = new CommentViewer(comments);
                comment_viewer.Show();
            }

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            browser.IsBrowserInitializedChanged += Browser_IsBrowserInitializedChanged;
        }

        private void Browser_IsBrowserInitializedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                const string resourcename = "http://mypage.html";
                //html = html.Replace("dcimg2.dcinside", "dcimg6.dcinside");
                System.IO.MemoryStream memorystream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(article.raw));
                browser.RegisterResourceHandler(resourcename, memorystream);
                browser.LoadHtml(article.raw, resourcename);
                browser.UnRegisterResourceHandler(resourcename);
            } catch { }
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        public class MyCustomResourceRequestHandler : CefSharp.Handler.ResourceRequestHandler
        {
            protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
            {
                request.SetReferrer("https://gall.dcinside.com/board/lists?id=programming", ReferrerPolicy.Default);
                return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);
            }
        }

        public class MyRequestHandler : CefSharp.Handler.RequestHandler
        {
            protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
            {
                if (request.Url.Contains("dcinside"))
                {
                    return new MyCustomResourceRequestHandler();
                }
                return null;
            }

            protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            { 
                if (request.Url.StartsWith($"https://gall.dcinside.com/mgallery/board/view?id={gallname}&no=") ||
                    request.Url.StartsWith($"https://gall.dcinside.com/mgallery/board/view/?id={gallname}&no=") ||
                    request.Url.StartsWith($"https://gall.dcinside.com/m/{gallname}/"))
                {
                    var no = request.Url.Split(new[] { '=', '/' }).Last();
                    (new ArchiveViewer(no)).Show();
                    return true;
                }
                return false;
            }

            protected override bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
            { // https://gall.dcinside.com/m/{gallname}/
                if (targetUrl.StartsWith($"https://gall.dcinside.com/mgallery/board/view?id={gallname}&no=") || 
                    targetUrl.StartsWith($"https://gall.dcinside.com/mgallery/board/view/?id={gallname}&no=") ||
                    targetUrl.StartsWith($"https://gall.dcinside.com/m/{gallname}/"))
                {
                    var no = targetUrl.Split(new[] { '=', '/' }).Last();
                    (new ArchiveViewer(no)).Show();
                }
                else
                {
                    Process.Start(targetUrl);
                }
                return true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (comment_viewer != null)
                comment_viewer.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var links = article.datalinks.Split('|').Where(x => x != "").ToList();
            var files = article.filenames.Split('|').Where(x => x != "").ToList();

            var tasks = new List<NetTask>();
            Directory.CreateDirectory("Images");
            for (int i = 0; i < links.Count; i++) {
                var task = MainWindow.Queue.MakeDefault(links[i]);
                task.Filename = $"Images/[{article.no}] " + i.ToString().PadLeft(3, '0') + "." + files[i].Split('.').Last();
                task.Referer = "https://gall.dcinside.com/mgallery/board/view?id=aoegame";
                MainWindow.Queue.DownloadFileAsync(task);
            }
        }
    }
}
