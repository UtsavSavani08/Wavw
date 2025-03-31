using Wavw.Views;

namespace Wavw;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes
        Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
        Routing.RegisterRoute(nameof(SignUpPage), typeof(SignUpPage));
        Routing.RegisterRoute(nameof(BeachPage), typeof(BeachPage));
        Routing.RegisterRoute("PopularBeachesPage", typeof(PopularBeachesPage));
    }
}
