using Wavw.Views;

namespace Wavw;

public partial class App : Application
{
    public App(HomePage homePage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(homePage);
    }
}
