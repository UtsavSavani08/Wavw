using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Wavw.Services;
using Wavw.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace Wavw.Views;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private readonly BeachService _beachService;
    private ObservableCollection<PopularBeach> _popularBeaches;
    private string _searchText;
    private bool _isLoading;
    private CancellationTokenSource _searchCancellationTokenSource;
    private readonly int _searchDelayMs = 500; // Delay in milliseconds

    public ObservableCollection<PopularBeach> PopularBeaches
    {
        get => _popularBeaches;
        set
        {
            if (_popularBeaches != value)
            {
                _popularBeaches = value;
                OnPropertyChanged(nameof(PopularBeaches));
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                DelayedSearch();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }
    }

    public ICommand SeeMoreCommand { get; }
    public ICommand PopularBeachSelectedCommand { get; }
    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToWeatherCommand { get; }
    public ICommand NavigateToMapCommand { get; }

    public MainPage()
    {
        InitializeComponent();
        _beachService = new BeachService();
        PopularBeaches = new ObservableCollection<PopularBeach>();
        PopularBeachSelectedCommand = new Command<PopularBeach>(OnPopularBeachSelected);
        SeeMoreCommand = new Command(OnSeeMoreClicked);
        
        // Initialize navigation commands
        NavigateToHomeCommand = new Command(async () => await Shell.Current.GoToAsync("//MainPage"));
        NavigateToWeatherCommand = new Command(async () => await Shell.Current.GoToAsync("//WeatherPage"));
        NavigateToMapCommand = new Command(async () => await Shell.Current.GoToAsync("//HomePage"));
        
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (PopularBeaches.Count == 0)
        {
            LoadPopularBeachesAsync();
        }
    }

    private async void LoadPopularBeachesAsync()
    {
        try
        {
            IsLoading = true;
            System.Diagnostics.Debug.WriteLine("Starting to load popular beaches...");
            
            string jsonString = null;
            try
            {
                System.Diagnostics.Debug.WriteLine("Attempting to load popular_beaches.json");
                using var stream = await FileSystem.OpenAppPackageFileAsync("popular_beaches.json");
                using var reader = new StreamReader(stream);
                jsonString = await reader.ReadToEndAsync();
                System.Diagnostics.Debug.WriteLine("Successfully loaded JSON file");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load JSON file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to be caught by outer try-catch
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                throw new Exception("JSON file is empty");
            }
            
            System.Diagnostics.Debug.WriteLine($"JSON content length: {jsonString.Length}");
            System.Diagnostics.Debug.WriteLine($"JSON content preview: {jsonString.Substring(0, Math.Min(100, jsonString.Length))}");
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            try
            {
                var data = JsonSerializer.Deserialize<PopularBeachData>(jsonString, options);
                if (data == null)
                {
                    throw new Exception("Deserialization resulted in null data");
                }
                
                var popularBeaches = data.Beaches;
                System.Diagnostics.Debug.WriteLine($"Deserialized {popularBeaches.Count} beaches");

                if (popularBeaches.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: No beaches found in the JSON data");
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PopularBeaches.Clear();
                    foreach (var beach in popularBeaches.Take(7))
                    {
                        System.Diagnostics.Debug.WriteLine($"Adding beach: {beach.Name} ({beach.City}, {beach.State})");
                        PopularBeaches.Add(beach);
                    }
                    System.Diagnostics.Debug.WriteLine($"Total beaches added to collection: {PopularBeaches.Count}");
                });
            }
            catch (JsonException jex)
            {
                System.Diagnostics.Debug.WriteLine($"JSON deserialization error: {jex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {jex.StackTrace}");
                throw;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading popular beaches: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Error", "Failed to load popular beaches. Please try again later.", "OK");
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void DelayedSearch()
    {
        if (_searchCancellationTokenSource != null)
        {
            _searchCancellationTokenSource.Cancel();
            _searchCancellationTokenSource.Dispose();
        }
        _searchCancellationTokenSource = new CancellationTokenSource();

        Task.Delay(_searchDelayMs, _searchCancellationTokenSource.Token)
            .ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    MainThread.BeginInvokeOnMainThread(SearchBeaches);
                }
            }, TaskScheduler.Default);
    }

    private async void SearchBeaches()
    {
        // Don't reload popular beaches when search is empty
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return;
        }

        // Don't search if text is too short
        if (SearchText.Trim().Length < 3)
        {
            return;
        }

        try
        {
            IsLoading = true;
            string jsonString;
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("beaches.json");
                using var reader = new StreamReader(stream);
                jsonString = await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load beaches.json: {ex.Message}");
                throw;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var data = JsonSerializer.Deserialize<BeachData>(jsonString, options);
            if (data?.Beaches == null)
            {
                throw new Exception("Failed to deserialize beach data");
            }

            var searchTerm = SearchText.Trim().ToLower();
            
            // First try exact match for name only
            var matchingBeach = data.Beaches.FirstOrDefault(b => 
                b.Name?.Trim().ToLower() == searchTerm);

            // If no exact name match, try exact matches for city and state
            if (matchingBeach == null)
            {
                matchingBeach = data.Beaches.FirstOrDefault(b => 
                    b.City?.Trim().ToLower() == searchTerm || 
                    b.State?.Trim().ToLower() == searchTerm);
            }

            // Only if we have an exact match, navigate to the beach page
            if (matchingBeach != null)
            {
                var navigationParameter = new Dictionary<string, object>
                {
                    { "Beach", matchingBeach }
                };

                await Shell.Current.GoToAsync("BeachPage", navigationParameter);
                SearchText = string.Empty; // Clear search after navigation
            }
            else
            {
                // Only show no results if the search term is long enough
                if (searchTerm.Length >= 3)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await DisplayAlert("No Results", "No beaches found matching your search.", "OK");
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching beaches: {ex.Message}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlert("Error", "Failed to search beaches. Please try again.", "OK");
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnPopularBeachSelected(PopularBeach selectedBeach)
    {
        if (selectedBeach == null)
            return;

        // Convert PopularBeach to Beach for the BeachPage
        var beach = new Beach
        {
            Name = selectedBeach.Name,
            City = selectedBeach.City,
            State = selectedBeach.State,
            Rating = selectedBeach.Rating,
            ImageUrl = selectedBeach.ImageUrl,
            Latitude = selectedBeach.Latitude,
            Longitude = selectedBeach.Longitude,
            Cleanliness = selectedBeach.Cleanliness,
            BestSeason = selectedBeach.BestSeason,
            MainAttractions = selectedBeach.MainAttractions
        };

        var navigationParameter = new Dictionary<string, object>
        {
            { "Beach", beach }
        };

        await Shell.Current.GoToAsync("BeachPage", navigationParameter);
    }

    private async void OnSeeMoreClicked()
    {
        await Shell.Current.GoToAsync("//PopularBeachesPage");
    }

    public class PopularBeachData
    {
        [JsonPropertyName("popular_beaches")]
        public List<PopularBeach> Beaches { get; set; } = new List<PopularBeach>();
    }

    public class BeachData
    {
        [JsonPropertyName("beaches")]
        public List<Beach> Beaches { get; set; } = new List<Beach>();
    }
} 