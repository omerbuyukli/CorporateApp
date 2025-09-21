using CorporateApp.Application.Interfaces;
using CorporateApp.Core.Entities;
using CorporateApp.Core.Entities.DiaEntities;
using CorporateApp.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CorporateApp.Infrastructure.Services.DiaServices
{
    // PersonelDto'yu bu namespace içinde tanımlayalım
    public class PersonelDto
    {
        public string Adi { get; set; }
        public string Soyadi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Eposta { get; set; }
        public string Sube { get; set; }
        public string IstenAyrilmaTarihi { get; set; }
    }

    public class PersonelSyncService : IPersonelSyncService
    {
        private readonly IDiaLoginService _loginService;
        private readonly IUserRepository _userRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PersonelSyncService> _logger;

        public PersonelSyncService(
            IDiaLoginService loginService,
            IUserRepository userRepository,
            IHttpClientFactory httpClientFactory,
            ILogger<PersonelSyncService> logger)
        {
            _loginService = loginService;
            _userRepository = userRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SyncPersonelFromDiaAsync()
        {
            try
            {
                // 1. Login to DIA
                var loginResult = await _loginService.LoginAsync();
                if (!loginResult.Success)
                {
                    _logger.LogError("DIA login failed: {Message}", loginResult.Message);
                    return false;
                }

                // 2. Get personel list from DIA
                var personelList = await GetPersonelFromDiaAsync(loginResult.SessionId);
                if (personelList == null || !personelList.Any())
                {
                    _logger.LogWarning("No personel data received from DIA");
                    return false;
                }

                // 3. Get existing users
                var existingUsers = await _userRepository.GetAllAsync();
                var existingUsersByTc = existingUsers.ToDictionary(u => u.Tcno);

                // 4. Sync each personel
                foreach (var personel in personelList)
                {
                    await SyncSinglePersonelAsync(personel, existingUsersByTc);
                }

                _logger.LogInformation("Personel sync completed successfully. Total: {Count}", personelList.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during personel sync");
                return false;
            }
        }

        private async Task<List<PersonelDto>> GetPersonelFromDiaAsync(string sessionId)
        {
            try
            {
                var request = new PersonelRequest.Root
                {
                    PerPersonelListele = new PersonelRequest.PersonelListele
                    {
                        SessionId = sessionId,
                        FirmaKodu = 1,
                        DonemKodu = 1,
                        Limit = 1000,
                        Offset = 0,
                        Filters = new List<PersonelRequest.Filter>(),
                        Sorts = new List<PersonelRequest.Sort>()
                    }
                };

                var client = _httpClientFactory.CreateClient("DiaApi");
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("personel/list", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PersonelResponse.Root>(responseContent);
                    
                    return result?.result?.Select(MapToPersonelDto).ToList();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personel from DIA");
                return null;
            }
        }

        private async Task SyncSinglePersonelAsync(PersonelDto personel, Dictionary<string, User> existingUsers)
        {
            if (string.IsNullOrEmpty(personel.TcKimlikNo))
                return;

            var user = new User
            {
                Name = personel.Adi,
                LastName = personel.Soyadi,
                Email = personel.Eposta,
                RoleId = 2,
                Tcno = personel.TcKimlikNo,
                Password = personel.TcKimlikNo, // Hash this in production!
                Location = personel.Sube,
                IsActive = string.IsNullOrEmpty(personel.IstenAyrilmaTarihi),
                CreatedDate = DateTime.UtcNow
            };

            if (existingUsers.ContainsKey(personel.TcKimlikNo))
            {
                // Update existing user
                var existingUser = existingUsers[personel.TcKimlikNo];
                user.Id = existingUser.Id;
                user.UpdatedDate = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
                
                _logger.LogInformation("Updated user: {Name} {LastName}", user.Name, user.LastName);
            }
            else if (user.IsActive)
            {
                // Add new active user
                await _userRepository.AddAsync(user);
                _logger.LogInformation("Added new user: {Name} {LastName}", user.Name, user.LastName);
            }
        }

        private PersonelDto MapToPersonelDto(PersonelResponse.Result result)
        {
            if (result == null) return null;
            
            return new PersonelDto
            {
                Adi = result.adi,
                Soyadi = result.soyadi,
                TcKimlikNo = result.tckimlikno,
                Eposta = result.eposta,
                Sube = result.sube,
                IstenAyrilmaTarihi = result.istenayrilmatarihi
            };
        }
    }
}
