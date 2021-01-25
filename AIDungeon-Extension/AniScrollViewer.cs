using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AIDungeon_Extension
{
    class AniScrollViewer : ScrollViewer
    {
        public static readonly DependencyProperty CurrentVerticalOffsetProperty =
            DependencyProperty.Register("CurrentVerticalOffset", typeof(double), typeof(AniScrollViewer),
                new PropertyMetadata(new PropertyChangedCallback(OnVerticalChanged)));
        public static readonly DependencyProperty CurrentHorizontalOffsetProperty =
            DependencyProperty.Register("CurrentHorizontalOffset", typeof(double), typeof(AniScrollViewer),
                new PropertyMetadata(new PropertyChangedCallback(OnHorizontalChanged)));

        public double CurrentVerticalOffset
        {
            get { return (double)GetValue(CurrentVerticalOffsetProperty); }
            set { SetValue(CurrentVerticalOffsetProperty, value); }
        }
        public double CurrentHorizontalOffset
        {
            get { return (double)GetValue(CurrentHorizontalOffsetProperty); }
            set { SetValue(CurrentHorizontalOffsetProperty, value); }
        }

        private static void OnVerticalChanged(DependencyObject property, DependencyPropertyChangedEventArgs e)
        {
            AniScrollViewer viewer = property as AniScrollViewer;
            viewer.ScrollToVerticalOffset((double)e.NewValue);
        }
        private static void OnHorizontalChanged(DependencyObject property, DependencyPropertyChangedEventArgs e)
        {
            AniScrollViewer viewer = property as AniScrollViewer;
            viewer.ScrollToHorizontalOffset((double)e.NewValue);
        }
    }
}
