namespace CorporateApp.Application.Interfaces
{
    public interface IPersonelSyncJobService
    {
        void ConfigureRecurringJobs();
        Task ExecutePersonelSync();
        void TriggerImmediateSync();
    }
}