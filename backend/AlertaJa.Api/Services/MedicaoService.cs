
using AlertaJa.Api.Data;
using AlertaJa.Api.DTOs.Medicoes;
using AlertaJa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertaJa.Api.Services
{
    public class MedicaoService
    {
        private readonly AppDbContext _context;

        public MedicaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task Receber(ReceberMedicaoDto dto)
        {
            if (dto.SensorId == Guid.Empty)
            {
                throw new ArgumentException(
                    "O identificador do sensor é inválido.");
            }

            if (dto.Valor < 0)
            {
                throw new ArgumentException(
                    "O valor da medição não pode ser negativo.");
            }

            var sensor = await _context.Sensores
                .FirstOrDefaultAsync(s => s.Id == dto.SensorId);

            if (sensor == null || !sensor.Ativo)
            {
                throw new ArgumentException(
                    "O sensor informado não existe ou está inativo.");
            }

            var agora = DateTime.Now;
 
            var medicao = new Medicao
            {
                Id = Guid.NewGuid(),
                SensorId = sensor.Id,
                Valor = dto.Valor,
                DataHora = agora,
                CriadoEm = agora
            };

            sensor.UltimaLeitura = medicao.DataHora;

            _context.Medicoes.Add(medicao);

            await _context.SaveChangesAsync();
        }
    }
}
