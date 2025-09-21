using CorporateApp.Application.Interfaces;
using CorporateApp.Core.Entities;
using CorporateApp.Core.Interfaces;
using CorporateApp.Infrastructure.Configuration;
using CorporateApp.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CorporateApp.Infrastructure.Services.DiaServices
{
    public class PersonelDto
    {
        public string Adi { get; set; }
        public string Soyadi { get; set; }
        public string TcKimlikNo { get; set; }
        public string Eposta { get; set; }
        public string Sube { get; set; }
        public string IstenAyrilmaTarihi { get; set; }
    }

    public class PersonelSyncService : DiaApiServiceBase, IPersonelSyncService
    {
        private readonly IDiaLoginService _loginService;
        private readonly IUserRepository _userRepository;

        public PersonelSyncService(
            IDiaLoginService loginService,
            IUserRepository userRepository,
            IHttpClientFactory httpClientFactory,
            IOptions<DiaApiConfiguration> configuration,
            ILogger<PersonelSyncService> logger)
            : base(httpClientFactory, configuration, logger)
        {
            _loginService = loginService;
            _userRepository = userRepository;
        }



        private async Task<List<PersonelDto>> GetPersonelFromDiaAsync(string sessionId)
        {
            try
            {
                var allPersonel = new List<PersonelDto>();
                int offset = 0;
                int limit = 100;
                bool hasMore = true;

                while (hasMore)
                {
                    // Aktif personeller için filtreleme
                    var request = new
                    {
                        per_personel_listele = new
                        {
                            session_id = sessionId,
                            firma_kodu = 1,
                            donem_kodu = 1,
                            filters = new[]
                            {
                                new
                                {
                                    field = "istenayrilmatarihi",
                                    @operator = "=",  // Boş olanları getir
                                    value = ""
                                },
                                new
                                {
                                    field = "tckimlikno",
                                    @operator = "!=",
                                    value = ""
                                }

                            },
                            sorts = new[]
                            {
                        new
                        {
                            field = "adi",
                            sorttype = "ASC"  // Ada göre sırala
                        }
                    },
                            @params = "",  // Ekstra parametre yok
                            limit = limit,
                            offset = offset
                        }
                    };

                    var client = _httpClientFactory.CreateClient("DiaApi");
                    var json = JsonConvert.SerializeObject(request);

                    _logger.LogInformation($"Fetching active personel - Offset: {offset}, Limit: {limit}");
                    _logger.LogDebug($"Request: {json}");

                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var endpoint = GetEndpoint(DiaApiConstants.Modules.PER);
                    var response = await client.PostAsync(endpoint, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        dynamic responseObj = JsonConvert.DeserializeObject(responseContent);

                        // Başarı kontrolü
                        string code = responseObj?.code?.ToString() ?? "";
                        string msg = responseObj?.msg?.ToString() ?? "";

                        if (code != "200")
                        {
                            _logger.LogError($"API Error - Code: {code}, Message: {msg}");

                            // Yetki hatası kontrolü
                            if (msg.Contains("INSUFFICIENT_PRIVILEGES"))
                            {
                                _logger.LogError("Personel listeleme yetkisi yok!");
                            }
                            break;
                        }

                        // Personel listesini parse et
                        if (responseObj?.result != null)
                        {
                            var results = responseObj.result as IEnumerable<dynamic>;
                            if (results != null)
                            {
                                int recordCount = 0;

                                foreach (var personel in results)
                                {
                                    allPersonel.Add(new PersonelDto
                                    {
                                        Adi = personel.adi?.ToString() ?? "",
                                        Soyadi = personel.soyadi?.ToString() ?? "",
                                        TcKimlikNo = personel.tckimlikno?.ToString() ?? "",
                                        Eposta = personel.eposta?.ToString() ?? "",
                                        Sube = personel.sube?.ToString() ?? "",
                                        IstenAyrilmaTarihi = personel.istenayrilmatarihi?.ToString()
                                    });
                                    recordCount++;
                                }

                                _logger.LogInformation($"Page {(offset / limit) + 1}: Fetched {recordCount} active personel. Total: {allPersonel.Count}");

                                // Daha fazla kayıt var mı?
                                if (recordCount < limit)
                                {
                                    hasMore = false; // Son sayfa
                                    _logger.LogInformation("Last page reached");
                                }
                                else
                                {
                                    offset += limit; // Sonraki sayfa
                                }
                            }
                            else
                            {
                                hasMore = false;
                                _logger.LogInformation("No more results");
                            }
                        }
                        else
                        {
                            hasMore = false;
                            _logger.LogWarning("Result is null");
                        }
                    }
                    else
                    {
                        hasMore = false;
                        _logger.LogError("Empty response from API");
                    }

                    // Rate limiting için kısa bekleme (opsiyonel)
                    if (hasMore)
                    {
                        await Task.Delay(100); // 100ms bekle
                    }
                }

                _logger.LogInformation($"✅ Total active personel fetched: {allPersonel.Count}");
                return allPersonel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personel from DIA");
                return new List<PersonelDto>();
            }
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

                SetSessionId(loginResult.SessionId);

                // 2. Get personel list from DIA
                var personelList = await GetPersonelFromDiaAsync(loginResult.SessionId);
                if (personelList == null || !personelList.Any())
                {
                    _logger.LogWarning("No personel data received from DIA");
                    return false;
                }

                // 3. Get existing users
                var existingUsers = (await _userRepository.GetAllAsync()).ToList();
                _logger.LogInformation("Mevcut kullanıcı sayısı: {Count}", existingUsers.Count);

                // TC'ye göre dictionary oluştur
                var usersByTc = existingUsers
                    .Where(u => !string.IsNullOrEmpty(u.Tcno))
                    .ToDictionary(u => u.Tcno, u => u);

                // DIA'dan gelen TC listesi
                var activeTcNumbers = new HashSet<string>(
                    personelList.Where(p => !string.IsNullOrEmpty(p.TcKimlikNo))
                               .Select(p => p.TcKimlikNo)
                );

                _logger.LogInformation("DIA'dan gelen personel sayısı: {Count}", personelList.Count);

                // İşlem listelerini hazırla
                var usersToAdd = new List<User>();
                var usersToUpdate = new List<User>();

                // 4. DIA'dan gelenleri işle
                foreach (var personel in personelList)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(personel.TcKimlikNo))
                        {
                            _logger.LogWarning("TC boş olan personel atlandı: {Name} {LastName}",
                                personel.Adi, personel.Soyadi);
                            continue;
                        }

                        if (usersByTc.TryGetValue(personel.TcKimlikNo, out var existingUser))
                        {
                            // ⭐ GÜNCELLEME - NULL kontrollerini ekle
                            var updatedUser = new User
                            {
                                Id = existingUser.Id,
                                Name = !string.IsNullOrEmpty(personel.Adi) ? personel.Adi : "Unknown",
                                LastName = !string.IsNullOrEmpty(personel.Soyadi) ? personel.Soyadi : "Unknown",
                                Email = !string.IsNullOrEmpty(personel.Eposta)
                                    ? personel.Eposta
                                    : !string.IsNullOrEmpty(existingUser.Email)
                                        ? existingUser.Email
                                        : $"{personel.TcKimlikNo}@corporateapp.com",
                                Password = !string.IsNullOrEmpty(existingUser.Password)
                                    ? existingUser.Password
                                    : BCrypt.Net.BCrypt.HashPassword(personel.TcKimlikNo),
                                Tcno = personel.TcKimlikNo,
                                Location = !string.IsNullOrEmpty(personel.Sube) ? personel.Sube : "Belirtilmemiş",
                                RoleId = existingUser.RoleId > 0 ? existingUser.RoleId : 2,
                                IsActive = true,
                                CreatedDate = existingUser.CreatedDate != default ? existingUser.CreatedDate : DateTime.UtcNow,
                                UpdatedDate = DateTime.UtcNow
                            };

                            // ⭐ Güncelleme öncesi validasyon
                            try
                            {
                                ValidateUser(updatedUser);
                                usersToUpdate.Add(updatedUser);
                                _logger.LogDebug("Güncelleme listesine eklendi: {Tcno} - {Name} {LastName}",
                                    updatedUser.Tcno, updatedUser.Name, updatedUser.LastName);
                            }
                            catch (Exception valEx)
                            {
                                _logger.LogError("Güncelleme validasyon hatası - TC: {Tcno}, Hata: {Error}",
                                    personel.TcKimlikNo, valEx.Message);
                            }
                        }
                        else
                        {
                            // ⭐ YENİ KULLANICI - NULL kontrollerini ekle
                            var newUser = new User
                            {
                                Name = !string.IsNullOrEmpty(personel.Adi) ? personel.Adi : "Unknown",
                                LastName = !string.IsNullOrEmpty(personel.Soyadi) ? personel.Soyadi : "Unknown",
                                Email = !string.IsNullOrEmpty(personel.Eposta)
                                    ? personel.Eposta
                                    : $"{personel.TcKimlikNo}@corporateapp.com",
                                Password = BCrypt.Net.BCrypt.HashPassword(personel.TcKimlikNo ?? "defaultpass"),
                                Tcno = personel.TcKimlikNo,
                                Location = !string.IsNullOrEmpty(personel.Sube) ? personel.Sube : "Belirtilmemiş",
                                RoleId = 2,
                                IsActive = true,
                                CreatedDate = DateTime.UtcNow,
                                UpdatedDate = null
                            };

                            // ⭐ Ekleme öncesi validasyon
                            try
                            {
                                ValidateUser(newUser);
                                usersToAdd.Add(newUser);
                                _logger.LogDebug("Ekleme listesine eklendi: {Tcno} - {Name} {LastName}",
                                    newUser.Tcno, newUser.Name, newUser.LastName);
                            }
                            catch (Exception valEx)
                            {
                                _logger.LogError("Yeni kullanıcı validasyon hatası - TC: {Tcno}, Hata: {Error}",
                                    personel.TcKimlikNo, valEx.Message);
                            }
                        }
                    }
                    catch (Exception pEx)
                    {
                        _logger.LogError(pEx, "Personel işleme hatası - TC: {Tcno}", personel?.TcKimlikNo ?? "NULL");
                    }
                }

                // 5. DIA'da olmayanları pasif yap
                var usersToDeactivate = new List<User>();
                foreach (var user in existingUsers.Where(u => u.IsActive && !string.IsNullOrEmpty(u.Tcno)))
                {
                    if (!activeTcNumbers.Contains(user.Tcno))
                    {
                        var deactivateUser = new User
                        {
                            Id = user.Id,
                            Name = user.Name ?? "Unknown",
                            LastName = user.LastName ?? "Unknown",
                            Email = user.Email ?? $"{user.Tcno}@corporateapp.com",
                            Password = user.Password ?? BCrypt.Net.BCrypt.HashPassword("temp"),
                            Tcno = user.Tcno,
                            Location = user.Location ?? "Belirtilmemiş",
                            RoleId = user.RoleId > 0 ? user.RoleId : 2,
                            IsActive = false,
                            CreatedDate = user.CreatedDate != default ? user.CreatedDate : DateTime.UtcNow,
                            UpdatedDate = DateTime.UtcNow
                        };

                        try
                        {
                            ValidateUser(deactivateUser);
                            usersToDeactivate.Add(deactivateUser);
                        }
                        catch (Exception valEx)
                        {
                            _logger.LogError("Deactivate validasyon hatası - TC: {Tcno}, Hata: {Error}",
                                user.Tcno, valEx.Message);
                        }
                    }
                }

                _logger.LogInformation("İşlem özeti - Eklenecek: {Add}, Güncellenecek: {Update}, Pasif yapılacak: {Deactivate}",
                    usersToAdd.Count, usersToUpdate.Count, usersToDeactivate.Count);

                // 6. İşlemleri yap
                int addedCount = 0, updatedCount = 0, deactivatedCount = 0;
                int addErrorCount = 0, updateErrorCount = 0, deactivateErrorCount = 0;

                // Yeni kullanıcıları ekle
                foreach (var user in usersToAdd)
                {
                    try
                    {
                        _logger.LogDebug("Ekleniyor: {Tcno} - {Name} {LastName}", user.Tcno, user.Name, user.LastName);

                        // ⭐ Ekleme öncesi son kontrol
                        LogUserDetails(user, "ADD");

                        await _userRepository.AddAsync(user);
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        addErrorCount++;
                        _logger.LogError(ex, "Kullanıcı eklenemedi - TC: {Tcno}, Email: {Email}, Hata: {Error}",
                            user.Tcno, user.Email, ex.InnerException?.Message ?? ex.Message);
                    }
                }

                // Güncellemeleri yap
                foreach (var user in usersToUpdate)
                {
                    try
                    {
                        _logger.LogDebug("Güncelleniyor: {Id} - {Tcno}", user.Id, user.Tcno);

                        // ⭐ Güncelleme öncesi son kontrol
                        LogUserDetails(user, "UPDATE");

                        await _userRepository.UpdateAsync(user);
                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        updateErrorCount++;
                        _logger.LogError(ex, "Kullanıcı güncellenemedi - Id: {Id}, TC: {Tcno}, Hata: {Error}",
                            user.Id, user.Tcno, ex.InnerException?.Message ?? ex.Message);
                    }
                }

                // Pasif yapma işlemleri
                foreach (var user in usersToDeactivate)
                {
                    try
                    {
                        _logger.LogDebug("Pasif yapılıyor: {Id} - {Tcno}", user.Id, user.Tcno);

                        // ⭐ Deactivate öncesi son kontrol
                        LogUserDetails(user, "DEACTIVATE");

                        await _userRepository.UpdateAsync(user);
                        deactivatedCount++;
                    }
                    catch (Exception ex)
                    {
                        deactivateErrorCount++;
                        _logger.LogError(ex, "Kullanıcı pasif yapılamadı - Id: {Id}, TC: {Tcno}, Hata: {Error}",
                            user.Id, user.Tcno, ex.InnerException?.Message ?? ex.Message);
                    }
                }

                _logger.LogInformation(
                    "✅ Sync tamamlandı - Eklenen: {Added}/{TotalAdd}, Güncellenen: {Updated}/{TotalUpdate}, Pasif: {Deactivated}/{TotalDeactivate}",
                    addedCount, usersToAdd.Count,
                    updatedCount, usersToUpdate.Count,
                    deactivatedCount, usersToDeactivate.Count);

                if (addErrorCount > 0 || updateErrorCount > 0 || deactivateErrorCount > 0)
                {
                    _logger.LogWarning("⚠️ Hatalar - Eklenemeyenler: {AddErr}, Güncellenemeyenler: {UpdateErr}, Pasif yapılamayanlar: {DeactivateErr}",
                        addErrorCount, updateErrorCount, deactivateErrorCount);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Personel sync genel hata: {Message}", ex.InnerException?.Message ?? ex.Message);
                return false;
            }
        }

        // Validation metodu
        private void ValidateUser(User user)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(user.Name))
                errors.Add($"Name is null or empty (TC: {user.Tcno})");
            if (string.IsNullOrEmpty(user.LastName))
                errors.Add($"LastName is null or empty (TC: {user.Tcno})");
            if (string.IsNullOrEmpty(user.Email))
                errors.Add($"Email is null or empty (TC: {user.Tcno})");
            if (string.IsNullOrEmpty(user.Password))
                errors.Add($"Password is null or empty (TC: {user.Tcno})");
            if (user.RoleId <= 0)
                errors.Add($"RoleId is invalid: {user.RoleId} (TC: {user.Tcno})");

            if (errors.Any())
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogError("User validation failed: {Errors}", errorMessage);
                throw new Exception($"User validation failed: {errorMessage}");
            }
        }

        // User detaylarını loglama
        private void LogUserDetails(User user, string operation)
        {
            _logger.LogInformation(@"[{Operation}] User Details:
        Id: {Id}
        Name: '{Name}' (Null: {NameNull}, Empty: {NameEmpty})
        LastName: '{LastName}' (Null: {LastNameNull}, Empty: {LastNameEmpty})
        Email: '{Email}' (Null: {EmailNull}, Empty: {EmailEmpty})
        Password: {HasPassword} (Null: {PasswordNull})
        Tcno: '{Tcno}' (Null: {TcnoNull})
        Location: '{Location}' (Null: {LocationNull})
        RoleId: {RoleId}
        IsActive: {IsActive}
        CreatedDate: {CreatedDate}
        UpdatedDate: {UpdatedDate}",
                operation,
                user.Id,
                user.Name, user.Name == null, string.IsNullOrEmpty(user.Name),
                user.LastName, user.LastName == null, string.IsNullOrEmpty(user.LastName),
                user.Email, user.Email == null, string.IsNullOrEmpty(user.Email),
                !string.IsNullOrEmpty(user.Password), user.Password == null,
                user.Tcno, user.Tcno == null,
                user.Location, user.Location == null,
                user.RoleId,
                user.IsActive,
                user.CreatedDate,
                user.UpdatedDate);
        }



    }
}