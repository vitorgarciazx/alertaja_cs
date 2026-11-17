
using AlertaJa.Api.DTOs.Medicoes;
using AlertaJa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlertaJa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicoesController : ControllerBase
    {
        private readonly MedicaoService _service;

        public MedicoesController(MedicaoService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Receber(
            [FromBody] ReceberMedicaoDto dto)
        {
            try
            {
                await _service.Receber(dto);

                return StatusCode(StatusCodes.Status201Created, new
                {
                    mensagem = "Medição Recebida com Sucesso."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    mensagem = ex.Message
                });
            }
        }
    }
}
