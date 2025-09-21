namespace CorporateApp.Application.Interfaces
{
    public interface IPersonelSyncService
    {
        Task<bool> SyncPersonelFromDiaAsync();
    }
}