using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;
using Supabase.Gotrue;

namespace Wavw.Services
{
    internal class SupabaseService
    {
        private static readonly string SupabaseUrl = "https://sgbxtacuzhmneufaomyi.supabase.co";
        private static readonly string SupabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InNnYnh0YWN1emhtbmV1ZmFvbXlpIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDMwOTg1NDAsImV4cCI6MjA1ODY3NDU0MH0.-MVekDYhdDt9ywYp829fBS-GlneLqG6UpjnfVkAZkqA";

        private static Supabase.Client _client;

        public SupabaseService()
        {
            if (_client == null)
            {
                _client = new Supabase.Client(SupabaseUrl, SupabaseKey);
            }
        }

        public async Task<bool> SignUpAsync(string email, string password)
        {
            try
            {
                var response = await _client.Auth.SignUp(email, password);
                return response != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sign-up error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            try
            {
                var session = await _client.Auth.SignIn(email, password);
                if (session != null)
                {
                    Preferences.Set("auth_token", session.AccessToken);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sign-in error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsUserLoggedIn()
        {
            var token = Preferences.Get("auth_token", null);
            return !string.IsNullOrEmpty(token);
        }

        public async Task SignOutAsync()
        {
            await _client.Auth.SignOut();
            Preferences.Remove("auth_token");
        }
    }
}
