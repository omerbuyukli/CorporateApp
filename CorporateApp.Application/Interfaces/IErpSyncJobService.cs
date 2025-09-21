namespace CorporateApp.Application.Interfaces
{
    public interface IErpSyncJobService
    {
        Task SyncUsersFromErpAsync();
        Task IncrementalSyncAsync();
    }
}
