using CorporateApp.Application.Interfaces;
using CorporateApp.Core.Interfaces;

namespace CorporateApp.Application.Services
{
    public class ErpSyncJobService : IErpSyncJobService
    {
        private readonly IUserRepository _userRepository;

        public ErpSyncJobService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task SyncUsersFromErpAsync()
        {
            try
            {
                // TODO: ERP'den kullanıcıları çek
                // Şimdilik test için basit bir işlem
                var users = await _userRepository.GetAllAsync();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Mevcut kullanıcı sayısı: {users.Count()}");
                // ERP API çağrısı simülasyonu
                await Task.Delay(3000);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ERP SYNC TAMAMLANDI!");
                // Sync işlemleri burada yapılacak
            }
            catch (Exception ex)
            {
                // Hata yönetimi
                throw new Exception($"ERP Sync hatası: {ex.Message}", ex);
            }
        }

        public async Task IncrementalSyncAsync()
        {
            // TODO: Sadece değişenleri sync et
            await Task.Delay(500);

            // Incremental sync logic
        }
    }
}
