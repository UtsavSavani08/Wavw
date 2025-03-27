using Wavw.Views;

namespace Wavw;

public partial class App : Application
{
    public App(SignUpPage signUpPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(signUpPage);
    }
}
