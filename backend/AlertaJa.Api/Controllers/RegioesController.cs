
using AlertaJa.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlertaJa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegioesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RegioesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("teste")]
        public async Task<IActionResult> Teste()
        {
            var regioes = await _context.Regioes
                .AsNoTracking()
                .Select(r => new
                {
                    r.Id,
                    r.Nome,
                    r.Cidade,
                    r.Estado
                })
                .Take(5)
                .ToListAsync();

            return Ok(regioes);
        }
    }
}
