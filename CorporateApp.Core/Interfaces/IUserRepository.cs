using CorporateApp.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CorporateApp.Core.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByTcnoAsync(string tcno);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersByRoleAsync(int roleId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> TcnoExistsAsync(string tcno);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<(IEnumerable<User> Users, int TotalCount)> GetPagedUsersAsync(
            int pageNumber, 
            int pageSize, 
            string searchTerm = null,
            int? roleId = null,
            bool? isActive = null);
    }
}
