using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;

namespace BigZipUI.Dialogs
{
    public partial class ConfirmDialog : Window
    {
        private TextBlock? _messageTextBlock;
        private TextBlock? _questionTextBlock;
        private Button? _yesButton;
        private Button? _noButton;
        private readonly TaskCompletionSource<bool> _tcs = new();

        public ConfirmDialog()
        {
            InitializeComponent();
            InitializeControls();
        }

        public ConfirmDialog(string message, string question) : this()
        {
            if (_messageTextBlock is not null)
            {
                _messageTextBlock.Text = message;
            }

            if (_questionTextBlock is not null)
            {
                _questionTextBlock.Text = question;
            }
        }

        public static ConfirmDialog CreateOverwriteDialog(string filePath)
        {
            return new ConfirmDialog(
                $"The file '{filePath}' already exists.",
                "Do you want to overwrite it?"
            )
            {
                Title = "Confirm Overwrite"
            };
        }

        public static ConfirmDialog CreateCancelDialog(string message)
        {
            return new ConfirmDialog(
                message,
                "Are you sure you want to cancel?"
            )
            {
                Title = "Confirm Cancel"
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeControls()
        {
            _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock");
            _questionTextBlock = this.FindControl<TextBlock>("QuestionTextBlock");
            _yesButton = this.FindControl<Button>("YesButton");
            _noButton = this.FindControl<Button>("NoButton");

            if (_yesButton is not null)
            {
                _yesButton.Click += (s, e) =>
                {
                    _tcs.TrySetResult(true);
                    Close();
                };
            }

            if (_noButton is not null)
            {
                _noButton.Click += (s, e) =>
                {
                    _tcs.TrySetResult(false);
                    Close();
                };
            }

            Closing += (s, e) =>
            {
                _tcs.TrySetResult(false);
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Escape)
                {
                    _tcs.TrySetResult(false);
                    Close();
                }
            };
        }

        public new async Task<bool> ShowDialog(Window owner)
        {
            _ = base.ShowDialog(owner);
            return await _tcs.Task;
        }
    }
}
