using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views
{
    public partial class ModerationPage : ContentPage
    {
        public ModerationPage(ModerationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

