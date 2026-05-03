using DonTroc.ViewModels;
using Microsoft.Maui.Controls;

namespace DonTroc.Views;

public partial class MesEvenementsView : ContentPage
{
    private readonly MesEvenementsViewModel _vm;

    public MesEvenementsView(MesEvenementsViewModel vm) // Méthode évenementielle pour la page "Mes événements" (créés / je participe).
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}

