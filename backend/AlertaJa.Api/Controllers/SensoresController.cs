using AlertaJa.Api.DTOs.Sensores;
using AlertaJa.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AlertaJa.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SensoresController : ControllerBase
    {
        private readonly SensorService _service;

        public SensoresController(SensorService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ListarTodos()
        {
            var sensores = await _service.ListarTodos();

            return Ok(sensores);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var sensor = await _service.ObterPorId(id);

            if (sensor == null)
            {
                return NotFound();
            }

            return Ok(sensor);
        }

        [HttpPost]
        public async Task<IActionResult> Criar(CriarSensorDto dto)
        {
            var sensor = await _service.Criar(dto);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = sensor.Id },
                sensor
                );
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Atualizar(Guid id, AtualizarSensorDto dto)
        {
            var sensor = await _service.Atualizar(id, dto);

            if (sensor == null)
            {
                return NotFound();
            }

            return Ok(sensor);
        }
    }
}
