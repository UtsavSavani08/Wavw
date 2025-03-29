using Microsoft.Maui.Controls;
using System;
using System.Linq;
using Wavw.Services;

namespace Wavw.Views;

public partial class LoginPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private bool _isLoading;

    public LoginPage()
    {
        InitializeComponent();
        _supabaseService = new SupabaseService();
        _isLoading = false;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        if (_isLoading) return;
        _isLoading = true;

        try
        {
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please enter both email and password", "OK");
                return;
            }

            // Show loading indicator
            IsBusy = true;
            LoginButton.IsEnabled = false;

            bool isSuccess = await _supabaseService.SignInAsync(email, password);
            if (isSuccess)
            {
                // Navigate to MainPage
                Application.Current.MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                await DisplayAlert("Error", "Invalid email or password. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            await DisplayAlert("Error", "Unable to connect to the server. Please check your internet connection and try again.", "OK");
        }
        finally
        {
            _isLoading = false;
            IsBusy = false;
            LoginButton.IsEnabled = true;
        }
    }

    private async void OnSignUpTapped(object sender, TappedEventArgs e)
    {
        if (_isLoading) return;
        await Navigation.PushAsync(new SignUpPage());
    }
}