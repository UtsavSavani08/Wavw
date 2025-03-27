using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Wavw.Services;
using Wavw.Model;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;

namespace Wavw.Views;

public partial class MainPage : ContentPage
{
    private readonly IGeolocation _geolocation;
    private readonly BeachService _beachService;
    private HomePage _homePage;

    public MainPage()
    {
        InitializeComponent();
        _geolocation = Geolocation.Default;
        _beachService = new BeachService();
        LoadHomePage();
    }

    private void LoadHomePage()
    {
        _homePage = new HomePage(_geolocation, _beachService);
        MainContent.Content = _homePage.Content;
    }
} 