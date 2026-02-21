using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BigZipUI.Dialogs
{
    public partial class ErrorDialog : Window
    {
        private TextBlock? _messageTextBlock;
        private Button? _okButton;

        public ErrorDialog()
        {
            InitializeComponent();
            InitializeControls();
        }

        public ErrorDialog(string message) : this()
        {
            if (_messageTextBlock is not null)
            {
                _messageTextBlock.Text = message;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _okButton = this.FindControl<Button>("OkButton");

            if (_okButton is not null)
            {
                _okButton.Click += (s, e) => Close();
            }

            KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter ||
                    e.Key == Avalonia.Input.Key.Return ||
                    e.Key == Avalonia.Input.Key.Escape)
                {
                    Close();
                }
            };
        }
    }
}
