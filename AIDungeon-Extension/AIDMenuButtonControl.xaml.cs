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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AIDungeon_Extension
{
    /// <summary>
    /// AIDMenuButtonControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AIDMenuButtonControl : UserControl
    {
        public AIDMenuButtonControl()
        {
            InitializeComponent();
        }

        public static DependencyProperty IconTextProperty =
        DependencyProperty.Register("IconText", typeof(string), typeof(AIDMenuButtonControl));
        public string IconText
        {
            get { return (string)GetValue(IconTextProperty); }
            set { SetValue(IconTextProperty, value); }
        }
        public static DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(AIDMenuButtonControl));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public event RoutedEventHandler Click;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
