using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;
using System.Net.Http;
using System.Threading;

namespace Wavw.Services
{
    internal class SupabaseService
    {
        private static readonly string SupabaseUrl = "SUPABASE_URL";
        private static readonly string SupabaseKey = "SUPABASE_KEY";

        private static Supabase.Client _client;
        private static readonly SemaphoreSlim _clientLock = new SemaphoreSlim(1, 1);

        public SupabaseService()
        {
            InitializeClient();
        }

        private async void InitializeClient()
        {
            try
            {
                await _clientLock.WaitAsync();
                if (_client == null)
                {
                    var options = new SupabaseOptions
                    {
                        AutoConnectRealtime = false,
                        AutoRefreshToken = true
                    };
                    _client = new Supabase.Client(SupabaseUrl, SupabaseKey, options);
                    await _client.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Supabase initialization error: {ex.Message}");
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async Task<bool> SignUpAsync(string email, string password)
        {
            try
            {
                await _clientLock.WaitAsync();
                if (_client == null)
                {
                    InitializeClient();
                }

                var response = await _client.Auth.SignUp(email, password);
                return response != null;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-up HTTP error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-up error: {ex.Message}");
                return false;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            try
            {
                await _clientLock.WaitAsync();
                if (_client == null)
                {
                    InitializeClient();
                }

                var session = await _client.Auth.SignIn(email, password);
                if (session != null)
                {
                    Preferences.Set("auth_token", session.AccessToken);
                    return true;
                }
                return false;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-in HTTP error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-in error: {ex.Message}");
                return false;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async Task<bool> IsUserLoggedIn()
        {
            try
            {
                var token = Preferences.Get("auth_token", null);
                if (string.IsNullOrEmpty(token))
                    return false;

                await _clientLock.WaitAsync();
                if (_client == null)
                {
                    InitializeClient();
                }

                var user = await _client.Auth.GetUser(token);
                return user != null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Session check error: {ex.Message}");
                return false;
            }
            finally
            {
                _clientLock.Release();
            }
        }

        public async Task SignOutAsync()
        {
            try
            {
                await _clientLock.WaitAsync();
                if (_client == null)
                {
                    InitializeClient();
                }

                await _client.Auth.SignOut();
                Preferences.Remove("auth_token");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sign-out error: {ex.Message}");
            }
            finally
            {
                _clientLock.Release();
            }
        }
    }
}
