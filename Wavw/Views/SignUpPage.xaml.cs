namespace Wavw.Views;
using Wavw.Services;
using System;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Text.RegularExpressions;

public partial class SignUpPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private bool _isPasswordVisible;
    private readonly Regex _emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public SignUpPage()
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

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            string email = EmailEntry.Text?.Trim();
            string password = PasswordEntry.Text;

            // Validate email and password
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Validation Error", "Please enter both email and password", "OK");
                return;
            }

            // Validate email format
            if (!_emailRegex.IsMatch(email))
            {
                await DisplayAlert("Validation Error", "Please enter a valid email address", "OK");
                return;
            }

            // Validate password strength
            if (password.Length < 6)
            {
                await DisplayAlert("Validation Error", "Password must be at least 6 characters long", "OK");
                return;
            }

            // Attempt to sign up
            bool isSuccess = await _supabaseService.SignUpAsync(email, password);
            if (isSuccess)
            {
                await DisplayAlert("Success", "Account created successfully! Please log in.", "OK");
                
                // Clear the entries
                EmailEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                
                // Navigate to login page
                await Navigation.PushAsync(new LoginPage());
            }
            else
            {
                await DisplayAlert("Error", "Unable to create account. The email might already be registered.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "An unexpected error occurred. Please try again later.", "OK");
            System.Diagnostics.Debug.WriteLine($"Sign-up error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (IsBusy) return;
        await Navigation.PushAsync(new LoginPage());
    }

    protected override bool OnBackButtonPressed()
    {
        // Navigate back to login page when hardware back button is pressed
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(new LoginPage());
        });
        return true;
    }
}