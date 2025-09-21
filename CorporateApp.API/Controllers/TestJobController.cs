using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.Storage;
using CorporateApp.Application.Interfaces;

namespace CorporateApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestJobController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobs;
        private readonly IErpSyncJobService _erpSyncService;

        public TestJobController(
            IBackgroundJobClient backgroundJobs,
            IErpSyncJobService erpSyncService)
        {
            _backgroundJobs = backgroundJobs;
            _erpSyncService = erpSyncService;
        }

        [HttpPost("run-sync-now")]
        public IActionResult RunSyncNow()
        {
            // Hemen çalıştır
            var jobId = _backgroundJobs.Enqueue(() => _erpSyncService.SyncUsersFromErpAsync());

            return Ok(new
            {
                message = "Job başlatıldı!",
                jobId = jobId,
                hangfireUrl = "http://localhost:5000/hangfire",
                timestamp = DateTime.Now
            });
        }

        [HttpPost("schedule-sync")]
        public IActionResult ScheduleSync(int delayMinutes = 5)
        {
            // X dakika sonra çalıştır
            var scheduledTime = DateTime.Now.AddMinutes(delayMinutes);
            var jobId = _backgroundJobs.Schedule(
                () => _erpSyncService.SyncUsersFromErpAsync(),
                TimeSpan.FromMinutes(delayMinutes));

            return Ok(new
            {
                message = $"Job {delayMinutes} dakika sonra çalışacak",
                jobId = jobId,
                scheduledTime = scheduledTime
            });
        }

        [HttpGet("test-connection")]
        public IActionResult TestConnection()
        {
            var stats = JobStorage.Current.GetMonitoringApi();
            var servers = stats.Servers();

            // Recurring jobs'ı farklı şekilde alalım
            var connection = JobStorage.Current.GetConnection();
            var recurringJobs = connection.GetRecurringJobs();

            return Ok(new
            {
                message = "Hangfire çalışıyor!",
                serverCount = servers.Count,
                enqueuedCount = stats.EnqueuedCount("default"),
                recurringJobsCount = recurringJobs.Count,
                recurringJobs = recurringJobs.Select(x => new
                {
                    x.Id,
                    x.NextExecution,
                    x.LastExecution,
                    x.Cron
                })
            });
        }

        [HttpGet("job-stats")]
        public IActionResult GetJobStats()
        {
            var monitor = JobStorage.Current.GetMonitoringApi();

            return Ok(new
            {
                servers = monitor.Servers().Count,
                queues = monitor.Queues(),
                succeeded = monitor.SucceededListCount(),
                failed = monitor.FailedCount(),
                processing = monitor.ProcessingCount(),
                scheduled = monitor.ScheduledCount(),
                enqueued = monitor.EnqueuedCount("default")
            });
        }
    }
}