using API.Data;
using API.Entities;
using API.Extensions;
using API.RequestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class ProductsController : BaseApiController
    {
        private readonly StoreContext _context;
        public ProductsController(StoreContext context)
        {
            _context = context;
        }

        // GetProducts is an API endpoint that retrieves a paginated list of products based on specified parameters.
        [HttpGet]
        public async Task<ActionResult<PagedList<Product>>> GetProducts([FromQuery] ProductParams productParams)
        {
            // Construct the initial query for products from the database using the DbContext.
            var query = _context.Products
                        .Sort(productParams.OrderBy)      // Apply sorting based on the provided OrderBy parameter.
                        .Search(productParams.SearchTerm) // Apply search filtering based on the provided SearchTerm.
                        .Filter(productParams.Brands, productParams.Types) // Apply additional filters based on Brands and Types.
                        .AsQueryable();                   // Convert the query to an IQueryable for further manipulation.

            // Use the custom ToPagedList method to paginate the query results.
            var products = await PagedList<Product>.ToPagedList(query, productParams.PageNumber, productParams.PageSize);

            // Add pagination-related information to the HTTP response headers.
            Response.AddPaginationHeader(products.MetaData);

            // Return the paginated list of products as an ActionResult.
            return products;
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            return await _context.Products.FindAsync(id);
        }

    }
}