using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace API.RequestHelpers
{
    public class PagedList<T> : List<T>
    {
        // Constructor for the PagedList class
        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            // Creates metadata to store pagination-related information
            MetaData = new MetaData
            {
                TotalCount = count,
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };

            AddRange(items);
        }

        // Property to hold pagination metadata
        public MetaData MetaData { get; set; }

        // Static method to create a paginated list from an IQueryable
        public static async Task<PagedList<T>> ToPagedList(IQueryable<T> query, int pageNumber, int pageSize)
        {
            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}