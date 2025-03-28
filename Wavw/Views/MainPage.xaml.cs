using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Wavw.Models;
using Wavw.Services;
using System.Collections.ObjectModel;
using Microsoft.Maui.Graphics;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System.Linq;
using System.Text.Json;

namespace Wavw.Views;

public partial class MainPage : ContentPage
{
    private List<Beach> _allBeaches;
    private readonly string _beachesFilePath = "Resources/beaches.json";

    public MainPage()
    {
        InitializeComponent();
        LoadBeaches();
    }

    private void LoadBeaches()
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(_beachesFilePath).Result;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            _allBeaches = JsonSerializer.Deserialize<List<Beach>>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading beaches: {ex.Message}");
            _allBeaches = new List<Beach>();
        }
    }

    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
            return;

        var searchTerm = e.NewTextValue.Trim().ToLower();
        var matchingBeach = _allBeaches?.FirstOrDefault(b => 
            b.Name.ToLower().Contains(searchTerm));

        if (matchingBeach != null)
        {
            await Navigation.PushAsync(new BeachPage(matchingBeach));
            SearchEntry.Text = string.Empty; // Clear search after navigation
        }
    }
}