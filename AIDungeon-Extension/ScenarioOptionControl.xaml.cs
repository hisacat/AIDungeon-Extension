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
    /// ScenarioOptionControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ScenarioOptionControl : UserControl
    {
        public ScenarioOptionControl()
        {
            InitializeComponent();
        }

        public static DependencyProperty OrderTextProperty =
        DependencyProperty.Register("OrderText", typeof(string), typeof(ScenarioOptionControl));
        public string OrderText
        {
            get { return (string)GetValue(OrderTextProperty); }
            set { SetValue(OrderTextProperty, value); }
        }
        public static DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(ScenarioOptionControl));
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        #region Fonts
        /*
        public static DependencyProperty FontFamilyProperty =
        DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(ScenarioOptionControl));
        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }
        public static DependencyProperty FontSizeProperty =
        DependencyProperty.Register("FontSize", typeof(double), typeof(ScenarioOptionControl));
        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }
        public static DependencyProperty FontWeightProperty =
        DependencyProperty.Register("FontWeight", typeof(FontWeight), typeof(ScenarioOptionControl));
        public FontWeight FontWeight
        {
            get { return (FontWeight)GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }
        public static DependencyProperty FontStyleProperty =
        DependencyProperty.Register("FontStyle", typeof(FontStyle), typeof(ScenarioOptionControl));
        public FontStyle FontStyle
        {
            get { return (FontStyle)GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }
        */
        public static DependencyProperty TextDecorationsProperty =
        DependencyProperty.Register("TextDecorations", typeof(TextDecorationCollection), typeof(ScenarioOptionControl));
        public TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection)GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }
        #endregion

        public event RoutedEventHandler Click;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
