using GalleryExplorer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// CommentViewer.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CommentViewer : Window
    {
        List<CommentColumnModel> comments;
        public CommentViewer(List<CommentColumnModel> comments)
        {
            InitializeComponent();

            this.comments = comments;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var builder = new StringBuilder();

            comments.Sort((x, y) => x.no.CompareTo(y.no));

            var dd = new Dictionary<string, List<CommentColumnModel>>();
            comments.ForEach(x =>
            {
                if (x.depth != 0)
                {
                    if (!dd.ContainsKey(x.c_no))
                    {
                        dd.Add(x.c_no, new List<CommentColumnModel>());
                    }

                    dd[x.c_no].Add(x);
                }
            });

            comments.Where(x => x.depth == 0).ToList().ForEach(y =>
            {
                var name = y.name;
                if (y.user_id == null || y.user_id.Trim() == "")
                    name += " (" + y.ip + ")";
                else name += " (" + y.user_id + ")";

                name += ": " + y.reg_date;
                builder.Append(name);
                builder.Append("\r\n");
                builder.Append(y.memo);
                builder.Append("\r\n");

                if (dd.ContainsKey(y.no.ToString()))
                {
                    builder.Append("\r\n");
                    dd[y.no.ToString()].ForEach(z =>
                    {
                        var zname =" ㄴ " + z.name;
                        if (z.user_id == null || z.user_id.Trim() == "")
                            zname += " (" + z.ip + ")";
                        else zname += " (" + z.user_id + ")";

                        zname += ": " + z.reg_date;
                        builder.Append(zname);
                        builder.Append("\r\n");
                        builder.Append("     " + z.memo);
                        builder.Append("\r\n");
                        builder.Append("\r\n");
                    });
                }

                builder.Append("----------------\r\n");
                //builder.Append()
            });

            body.Text = builder.ToString();
        }
    }
}
