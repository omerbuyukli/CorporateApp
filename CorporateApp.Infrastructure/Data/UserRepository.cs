using CorporateApp.Core.Entities;
using CorporateApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CorporateApp.Infrastructure.Data
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByTcnoAsync(string tcno)
        {
            if (string.IsNullOrWhiteSpace(tcno))
                throw new ArgumentException("Tcno boş olamaz.", nameof(tcno));

            if (!tcno.All(char.IsDigit))
                throw new ArgumentException("Tcno sadece rakamlardan oluşmalıdır.", nameof(tcno));

            try
            {
                var ret = await _context.Users.FirstOrDefaultAsync(u => u.Tcno == tcno);

                return ret;
            }
            catch (Exception ex)
            {
                throw new Exception("Veritabanı sorgusu sırasında hata oluştu.", ex);
            }
        }
        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(int roleId)
        {
            return await _context.Users
                .Where(u => u.RoleId == roleId)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }

        public async Task<bool> TcnoExistsAsync(string tcno)
        {
            return await _context.Users
                .AnyAsync(u => u.Tcno == tcno);
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            searchTerm = searchTerm.ToLower();

            return await _context.Users
                .Where(u => u.Name.ToLower().Contains(searchTerm) ||
                           u.LastName.ToLower().Contains(searchTerm) ||
                           u.Email.ToLower().Contains(searchTerm) ||
                           u.Tcno.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<(IEnumerable<User> Users, int TotalCount)> GetPagedUsersAsync(
            int pageNumber,
            int pageSize,
            string searchTerm = null,
            int? roleId = null,
            bool? isActive = null)
        {
            var query = _context.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(u => u.Name.ToLower().Contains(searchTerm) ||
                                         u.LastName.ToLower().Contains(searchTerm) ||
                                         u.Email.ToLower().Contains(searchTerm));
            }

            if (roleId.HasValue)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Name)
                .ThenBy(u => u.LastName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
    }
}
