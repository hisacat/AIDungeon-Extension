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
    public partial class MainWindow : Window
    {
        public class DialogData : IComparable
        {
            public string id { get; set; }
            public string dialog { get; set; }

            private static List<DialogData> instance;
            public static List<DialogData> Instance
            {
                get
                {
                    if (instance == null)
                        instance = new List<DialogData>();
                    return instance;
                }
            }
            public static void Sort()
            {
                Instance.Sort();
            }

            int IComparable.CompareTo(object obj)
            {
                if (obj == null) return 1;
                var target = (DialogData)obj;

                if (this.id.Length == target.id.Length)
                    return this.id.CompareTo(target.id);
                else
                    return this.id.Length.CompareTo(target.id.Length);

            }
        }

        public MainWindow()
        {
            InitializeComponent();

            UpdateMode(WriteMode.Say);

            DialogData.Instance.Add(new DialogData() { id = "10", dialog = "test1" });
            DialogData.Instance.Add(new DialogData() { id = "15", dialog = "test2" });
            DialogData.Instance.Add(new DialogData() { id = "12", dialog = "test3\r\nLine... ok" });
            DialogData.Instance.Add(new DialogData() { id = "106", dialog = "sgs" });

            DialogData.Instance.Sort();

            this.dialogListView.ItemsSource = DialogData.Instance;

            //dialogListView.Items
            //System.Environment.Exit(0);
        }
        private void SendTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                //Send
                e.Handled = true;
            }

            if (e.Key == Key.Space)
            {
                if (sendTextBox.Text.StartsWith("/"))
                {
                    if (sendTextBox.Text.Equals("/say", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        sendTextBox.Text = string.Empty;
                        UpdateMode(WriteMode.Say);
                    }
                    else if (sendTextBox.Text.Equals("/do", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        sendTextBox.Text = string.Empty;
                        UpdateMode(WriteMode.Do);
                    }
                    else if (sendTextBox.Text.Equals("/story", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Handled = true;
                        sendTextBox.Text = string.Empty;
                        UpdateMode(WriteMode.Story);
                    }
                }
            }
        }

        public enum WriteMode : int
        {
            Say = 0,
            Do = 1,
            Story = 2,
        }
        public void UpdateMode(WriteMode mode)
        {
            switch (mode)
            {
                case WriteMode.Say:
                    placeHolderTextBlock.Text = "What do you say?";
                    break;
                case WriteMode.Do:
                    placeHolderTextBlock.Text = "What do you do?";
                    break;
                case WriteMode.Story:
                    placeHolderTextBlock.Text = "What happens next?";
                    break;
            }
        }

        private void SendTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }
    }
}
