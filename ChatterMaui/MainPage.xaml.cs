using ChatterMaui.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace ChatterMaui
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage(IMessenger messenger)
        {
            InitializeComponent();
            BindingContext = new MainViewModel(Dispatcher, messenger);
        }
    }

}
