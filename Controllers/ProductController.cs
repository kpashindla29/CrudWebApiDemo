using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using CrudWebApiDemo.Models;
using CrudWebApiDemo.Data;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace CrudWebApiDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ProductContext _context;

        private readonly IConfiguration _configuration; // Add this

        public ProductsController(ProductContext context, IConfiguration configuration )
        {
            _context = context;
            _configuration = configuration; // Store it
        }

        // Public endpoint - no authentication required
        [HttpGet("public")]
        [AllowAnonymous]
        public ActionResult<List<Product>> GetPublicProducts()
        {
            // Return only basic product info for public access
            return _context.Products
                .Select(p => new Product 
                { 
                    Id = p.Id, 
                    Name = p.Name, 
                    Price = p.Price 
                })
                .ToList();
        }

        // GET: api/products
        [HttpGet]
        [Authorize] // Requires any authenticated user
        public ActionResult<List<Product>> GetAll()
        {
            // Get user info for auditing
            var userName = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            return _context.Products.ToList();
        }

        // GET: api/products/{id}
        [HttpGet("{id}", Name = "GetProduct")]
        [Authorize]
        public ActionResult<Product> GetById(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // POST: api/products
        [HttpPost]
        [Authorize] 
        public IActionResult Create(Product product)
        {
              // Get user info from token
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;
            
            // Set the CreatedBy field (required by database)
            product.CreatedBy = userId ?? userName ?? "Admin";
                    
            _context.Products.Add(product);
            _context.SaveChanges();

            return CreatedAtRoute("GetProduct", new { id = product.Id }, product);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize]
        public IActionResult Update(int id, Product product)
        {
            var existingProduct = _context.Products.Find(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            // Optional: Check ownership
            var createdBy = existingProduct.CreatedBy; // Assuming you have this property
            var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (createdBy != currentUser && !User.IsInRole("Admin"))
            {
                return Forbid("You can only update your own products");
            }

            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;

            _context.Products.Update(existingProduct);
            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            // Only admins can delete
            if (!User.IsInRole("Admin"))
            {
                return Forbid("Only administrators can delete products");
            }

            _context.Products.Remove(product);
            _context.SaveChanges();

            return NoContent();
        }

        // GET: api/products/user
        [HttpGet("user")]
        [Authorize]
        public IActionResult GetUserProducts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Assuming you have a UserId field in Product model
            var userProducts = _context.Products
                // .Where(p => p.CreatedBy == userId) // Uncomment if you track ownership
                .ToList();
            
            return Ok(userProducts);
        }

        // GET: api/products/userinfo
        [HttpGet("userinfo")]
        [Authorize]
        public IActionResult GetUserInfo()
        {
            var userInfo = new
            {
                Name = User.Identity?.Name,
                Email = User.FindFirstValue(ClaimTypes.Email),
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                TenantId = User.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid"),
                Roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                    .Select(c => c.Value)
                    .ToList(),
                AllClaims = User.Claims
                    .Select(c => new { c.Type, c.Value })
                    .ToList()
            };
            
            return Ok(userInfo);
        }


        [HttpGet("check-roles")]
        [Authorize]
        public IActionResult CheckRoles()
        {
            var hasRoles = User.Claims.Any(c => c.Type == "roles" || c.Type == ClaimTypes.Role);
            
            return Ok(new
            {
                HasRolesClaim = hasRoles,
                RoleClaims = User.Claims
                    .Where(c => c.Type == "roles" || c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList(),
                AllClaims = User.Claims
                    .Select(c => new { c.Type, c.Value })
                    .ToList()
            });
        }

        [HttpGet("debug-config")]
        [AllowAnonymous]
        public IActionResult DebugConfig()
        {
            var config = _configuration.GetSection("AzureAd");
            
            return Ok(new
            {
                // What your API is configured to accept
                AzureAdConfiguration = new
                {
                    TenantId = config["TenantId"],
                    ClientId = config["ClientId"],
                    Audience = config["Audience"],
                    Instance = config["Instance"],
                    HasAudience = !string.IsNullOrEmpty(config["Audience"])
                },
                
                // Instructions for fixing
                Instructions = "If getting 401 invalid_token:",
                Steps = new[]
                {
                    "1. Get token and decode at https://jwt.ms",
                    "2. Compare 'aud' claim in token with 'Audience' above",
                    "3. They must match EXACTLY",
                    "4. Update appsettings.json if they don't match"
                },
                
                // Common issues
                CommonIssues = new[]
                {
                    "Token 'aud' is 'api://client-id' but API expects just 'client-id'",
                    "Missing 'api://' prefix in Audience",
                    "ClientId doesn't match token's 'appid' claim"
                }
            });
        }


        [HttpGet("check-scopes")]
        [Authorize]
        public IActionResult CheckScopes()
        {
            // Get all scope claims
            var scopeClaims = User.Claims
                .Where(c => c.Type == "scp" || c.Type == "http://schemas.microsoft.com/identity/claims/scope")
                .ToList();
            
            var allScopes = scopeClaims
                .SelectMany(c => c.Value.Split(' '))
                .ToList();
            
            return Ok(new
            {
                HasScopeClaims = scopeClaims.Any(),
                ScopeClaimTypes = scopeClaims.Select(c => c.Type).Distinct().ToList(),
                AllScopeValues = scopeClaims.Select(c => c.Value).ToList(),
                AllScopes = allScopes,
                HasProductsRead = allScopes.Contains("Products.Read"),
                AllClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
     }


    
}