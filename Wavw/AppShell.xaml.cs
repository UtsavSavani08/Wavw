using Wavw.Views;

namespace Wavw;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
        Routing.RegisterRoute("BeachPage", typeof(BeachPage));
        Routing.RegisterRoute("PopularBeachesPage", typeof(PopularBeachesPage));
    }
}
