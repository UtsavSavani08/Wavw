using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wavw.Model;
using Wavw.Services;    



namespace Wavw.Views;

public partial class PopularBeachesPage : ContentPage, INotifyPropertyChanged
{
    private ObservableCollection<PopularBeach> _popularBeaches;
    private bool _isLoading;
    private readonly BeachService _beachService;  // Add this line

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
        _beachService = new BeachService(); // Add this line
        BindingContext = this;
        LoadPopularBeachesAsync();
    }

    private async void LoadPopularBeachesAsync()
    {
        try
        {
            IsLoading = true;
            var beaches = await _beachService.GetPopularBeachesAsync();
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PopularBeaches.Clear();
                foreach (var beach in beaches)
                {
                    PopularBeaches.Add(beach);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading popular beaches: {ex.Message}");
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