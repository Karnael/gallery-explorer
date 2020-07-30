using CefSharp;
using CefSharp.Wpf;
using GalleryExplorer.Core;
using System;
using System.Collections.Generic;
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
        string html;
        List<CommentColumnModel> comments;
        CommentViewer comment_viewer;

        public ArchiveViewer(string id)
        {
            InitializeComponent();

            browser = new ChromiumWebBrowser(string.Empty)
            {
                RequestHandler = new MyRequestHandler(),
            };
            browserContainer.Content = browser;
            html = DCInsideArchiveDB.Instance.QueryById(id).raw;
            comments = DCInsideArchiveCommentDB.Instance.QueryById(id);

            if (comments != null && comments.Count != 0)
            {
                comment_viewer = new CommentViewer(comments);
                comment_viewer.Show();
            }

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            const string resourcename = "http://mypage.html";
            //html = html.Replace("dcimg2.dcinside", "dcimg6.dcinside");
            System.IO.MemoryStream memorystream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));
            browser.RegisterResourceHandler(resourcename, memorystream);
            browser.LoadHtml(html, resourcename);
            browser.UnRegisterResourceHandler(resourcename);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (comment_viewer != null)
                comment_viewer.Close();
        }
    }
}
