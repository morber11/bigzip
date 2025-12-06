using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using BigZipUI.Dialogs;
using BigZipUI.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using ui.Dialogs;

namespace BigZipUI;

public partial class MainWindow : Window
{
    private readonly Border? _inputBorder;
    private MainWindowViewModel? _vm;

    private readonly Func<Task<string?>>? _openPicker;
    private readonly Func<Task<string?>>? _savePicker;
    private readonly Func<string, Task>? _showDialog;
    private readonly Func<string, Task<bool>>? _confirmDialog;
    private readonly Func<bool, string, Task>? _showResultDialog;

    public MainWindow()
    {
        InitializeComponent();

        _inputBorder = this.FindControl<Border>("InputBorder");

        _openPicker = new Func<Task<string?>>(async () =>
            {
                var res = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });

                if (res.Any())
                {
                    return res[0]?.Path?.LocalPath;
                }

                return null;
            });

        _savePicker = new Func<Task<string?>>(async () =>
            {
                var res = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions());

                return res?.Path?.LocalPath;
            });

        _showDialog = new Func<string, Task>(async (message) =>
            {
                var dialog = new ErrorDialog(message);
                await dialog.ShowDialog(this);
            });

        _confirmDialog = new Func<string, Task<bool>>(async (message) =>
            {
                var dialog = ConfirmDialog.CreateOverwriteDialog(message);
                return await dialog.ShowDialog(this);
            });

        _showResultDialog = new Func<bool, string, Task>(async (success, message) =>
            {
                var dlg = new ResultDialog(success, message);

                await dlg.ShowDialog(this);
            });

        DataContextChanged += (s, e) =>
        {
            _vm = DataContext as MainWindowViewModel;

            _vm?.SetDialogs(_openPicker, _savePicker, _showDialog, _confirmDialog, _showResultDialog);
        };

        DragDrop.SetAllowDrop(this, true);

        AddHandler(DragDrop.DropEvent, Window_Drop);
        AddHandler(DragDrop.DragOverEvent, Window_DragOver);
        AddHandler(DragDrop.DragLeaveEvent, Window_DragLeave);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Window_Drop(object? sender, DragEventArgs e)
    {
        HandleFileDrop(e);
    }

    private void Window_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;

            _inputBorder?.Classes.Add("drag-over");
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }
    }

    private void Window_DragLeave(object? sender, DragEventArgs e)
    {
        _inputBorder?.Classes.Remove("drag-over");
    }

    private void HandleFileDrop(DragEventArgs e)
    {
        _inputBorder?.Classes.Remove("drag-over");

        var files = e.Data.GetFiles();

        if (files is not null && files.Any())
        {
            var firstFile = files.First();
            var path = firstFile.Path.LocalPath;

            if (_vm is not null)
            {
                _vm.InputPath = path;
            }
        }
    }
}