namespace Wavw;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.WeatherPage), typeof(Views.WeatherPage));
        Routing.RegisterRoute(nameof(Views.HomePage), typeof(Views.HomePage));
    }
}
