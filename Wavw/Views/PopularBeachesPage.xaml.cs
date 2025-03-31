using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wavw.Model;

namespace Wavw.Views;

public partial class PopularBeachesPage : ContentPage, INotifyPropertyChanged
{
    private ObservableCollection<PopularBeach> _popularBeaches;
    private bool _isLoading;

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

    public ICommand BeachSelectedCommand { get; }

    public PopularBeachesPage()
    {
        InitializeComponent();
        _popularBeaches = new ObservableCollection<PopularBeach>();
        BeachSelectedCommand = new Command<PopularBeach>(OnBeachSelected);
        BindingContext = this;
        LoadPopularBeachesAsync();
    }

    private async void LoadPopularBeachesAsync()
    {
        try
        {
            IsLoading = true;
            System.Diagnostics.Debug.WriteLine("Starting to load all popular beaches...");
            
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
                throw;
            }

            if (string.IsNullOrEmpty(jsonString))
            {
                throw new Exception("JSON file is empty");
            }
            
            System.Diagnostics.Debug.WriteLine($"JSON content length: {jsonString.Length}");
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            try
            {
                var data = JsonSerializer.Deserialize<PopularBeachData>(jsonString, options);
                if (data == null || data.Beaches == null)
                {
                    throw new Exception("Deserialization resulted in null data");
                }
                
                System.Diagnostics.Debug.WriteLine($"Deserialized {data.Beaches.Count} beaches");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    PopularBeaches.Clear();
                    foreach (var beach in data.Beaches)
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

    private async void OnBeachSelected(PopularBeach beach)
    {
        if (beach == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Beach", new Beach
                {
                    Name = beach.Name,
                    State = beach.State,
                    City = beach.City,
                    Rating = beach.Rating,
                    ImageUrl = beach.ImageUrl,
                    Latitude = beach.Latitude,
                    Longitude = beach.Longitude,
                    Cleanliness = beach.Cleanliness,
                    BestSeason = beach.BestSeason,
                    MainAttractions = beach.MainAttractions
                }
            }
        };

        await Shell.Current.GoToAsync("BeachPage", navigationParameter);
    }

    private async void OnBackButtonClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    public class PopularBeachData
    {
        [JsonPropertyName("popular_beaches")]
        public List<PopularBeach> Beaches { get; set; } = new List<PopularBeach>();
    }
} 