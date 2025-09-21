using CorporateApp.Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CorporateApp.Application.Services
{
    public class PersonelSyncJobService : IPersonelSyncJobService
    {
        private readonly IPersonelSyncService _personelSyncService;
        private readonly IRecurringJobManager _recurringJobManager;  // ← EKLE
        private readonly IBackgroundJobClient _backgroundJobClient;   // ← EKLE
        private readonly ILogger<PersonelSyncJobService> _logger;

        public PersonelSyncJobService(
            IPersonelSyncService personelSyncService,
            IRecurringJobManager recurringJobManager,  // ← EKLE
            IBackgroundJobClient backgroundJobClient,   // ← EKLE
            ILogger<PersonelSyncJobService> logger)
        {
            _personelSyncService = personelSyncService;
            _recurringJobManager = recurringJobManager;  // ← EKLE
            _backgroundJobClient = backgroundJobClient;   // ← EKLE
            _logger = logger;
        }

        public void ConfigureRecurringJobs()
        {
            // Her gün saat 02:00'de çalışacak - IRecurringJobManager kullan
            _recurringJobManager.AddOrUpdate(
                "personel-sync-job",
                () => ExecutePersonelSync(),
                "0 2 * * *", // Cron expression
                TimeZoneInfo.Local);

            _logger.LogInformation("Personel sync job configured to run daily at 02:00");
        }

        public async Task ExecutePersonelSync()
        {
            _logger.LogInformation("Starting personel sync job at {Time}", DateTime.Now);

            try
            {
                var result = await _personelSyncService.SyncPersonelFromDiaAsync();

                if (result)
                {
                    _logger.LogInformation("Personel sync job completed successfully");
                }
                else
                {
                    _logger.LogWarning("Personel sync job completed with warnings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in personel sync job");
                throw; // Hangfire will retry
            }
        }

        public void TriggerImmediateSync()
        {
            // BackgroundJob yerine IBackgroundJobClient kullan
            _backgroundJobClient.Enqueue(() => ExecutePersonelSync());
            _logger.LogInformation("Immediate personel sync triggered");
        }
    }
}
