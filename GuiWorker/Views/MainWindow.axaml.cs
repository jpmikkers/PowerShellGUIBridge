using System;
using Avalonia.Controls;
using GuiWorker.ViewModels;
using System.Threading.Tasks;

namespace GuiWorker.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is MainWindowViewModel vm)
        {
            _ = vm.InitializeNamedPipe(this).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }        
    }
}