using AlertaJa.Api.Data;
using AlertaJa.Api.DTOs.Sensores;
using AlertaJa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertaJa.Api.Services
{
    public class SensorService
    {
        private readonly AppDbContext _context;

        public SensorService(AppDbContext context)
        {
            _context = context;
        }

        //listar todos os sensores
        public async Task<List<SensorResponseDto>> ListarTodos()
        {
            return await _context.Sensores
                .AsNoTracking()
                .Select(s => new SensorResponseDto
                {
                    Id = s.Id,
                    RegiaoId = s.RegiaoId,
                    CodigoIdentificador = s.CodigoIdentificador,
                    Tipo = s.Tipo,
                    UnidadeMedida = s.UnidadeMedida,
                    LimiteAtencao = s.LimiteAtencao,
                    LimiteAlerta = s.LimiteAlerta,
                    LimitePerigo = s.LimitePerigo,
                    Ativo = s.Ativo,
                    UltimaLeitura = s.UltimaLeitura,
                    CriadoEm = s.CriadoEm,
                    AtualizadoEm = s.AtualizadoEm

                })
                .ToListAsync();
        }

        // listar sensor por id

        public async Task<SensorResponseDto?> ObterPorId(Guid id
        {
            return await _context.Sensores
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SensorResponseDto
                {
                    Id = s.Id,
                    RegiaoId = s.RegiaoId,
                    CodigoIdentificador = s.CodigoIdentificador,
                    Tipo = s.Tipo,
                    UnidadeMedida = s.UnidadeMedida,
                    LimiteAtencao = s.LimiteAtencao,
                    LimiteAlerta = s.LimiteAlerta,
                    LimitePerigo = s.LimitePerigo,
                    Ativo = s.Ativo,
                    UltimaLeitura = s.UltimaLeitura,
                    CriadoEm = s.CriadoEm,
                    AtualizadoEm = s.AtualizadoEm
                })
                .FirstOrDefaultAsync();
        }

        // res - sensor após cadastro
        public async Task<SensorResponseDto> Criar(CriarSensorDto dto)
        {
            var sensor = new Sensor
            {

            }
        }
    }
}
