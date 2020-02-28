using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using wBeatSaberCamera.Annotations;

namespace wBeatSaberCamera.Controls
{
    /// <summary>
    /// Interaction logic for AnnounceControl.xaml
    /// </summary>
    public partial class AnnounceControl : UserControl
    {
        [PublicAPI]
        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(AnnounceControl), new PropertyMetadata(null));

        [PublicAPI]
        public bool TextTemplate
        {
            get => (bool)GetValue(TextTemplateProperty);
            set => SetValue(TextTemplateProperty, value);
        }

        public static readonly DependencyProperty TextTemplateProperty = DependencyProperty.Register(nameof(TextTemplate), typeof(string), typeof(AnnounceControl), new PropertyMetadata(null));

        [PublicAPI]
        public IEnumerable<string> Macros
        {
            get => (IEnumerable<string>)GetValue(MacrosProperty);
            set => SetValue(MacrosProperty, value);
        }

        public static readonly DependencyProperty MacrosProperty = DependencyProperty.Register(nameof(Macros), typeof(IEnumerable<string>), typeof(AnnounceControl), new PropertyMetadata(null));

        [PublicAPI]
        public object AnnounceContent
        {
            get => TheBox.GetValue(CheckBox.ContentProperty);
            set => TheBox.SetValue(CheckBox.ContentProperty, value);
        }

        public AnnounceControl()
        {
            InitializeComponent();
            TheGrid.DataContext = this;
        }
    }
}