using Microsoft.Maui.Controls;
using System;
using System.Linq;
using Wavw.Services;

namespace Wavw.Views;

public partial class LoginPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private bool _isPasswordVisible;

    public LoginPage()
    {
        InitializeComponent();
        _supabaseService = new SupabaseService();
        _isPasswordVisible = false;
    }

    private void OnTogglePassword(object sender, TappedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        var entry = ((sender as Image)?.Parent as Grid)?.Children.OfType<Entry>().FirstOrDefault();
        if (entry != null)
        {
            entry.IsPassword = !_isPasswordVisible;
            ((Image)sender).Source = _isPasswordVisible ? "eye_off.png" : "eye.png";
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter both email and password", "OK");
                return;
            }

            bool isSuccess = await _supabaseService.SignInAsync(email, password);
            if (isSuccess)
            {
                // Navigate to HomePage
                Application.Current.MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                await DisplayAlert("Error", "Invalid email or password", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "An error occurred while logging in", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void OnSignUpTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushAsync(new SignUpPage());
    }
}