
using AlertaJa.Api.Data;
using AlertaJa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AlertaJa.Api.Services
{
    public class AlertaService
    {
        private readonly AppDbContext _context;

        public AlertaService(AppDbContext context)
        {
            _context = context;
        }

        private static string ClassificarNivel(
            decimal valor,
            Sensor sensor)
        {
            if (valor >= sensor.LimitePerigo)
                return "PERIGO";

            if (valor >= sensor.LimiteAlerta)
                return "ALERTA";

            if (valor >= sensor.LimiteAtencao)
                return "ATENCAO";

            return "NORMAL";
        }

        public async Task ProcessarMedicao(
    Sensor sensor,
    Medicao medicao,
    DateTime agora)
        {
            var nivel = ClassificarNivel(medicao.Valor, sensor);

            var regiao = await _context.Regioes
                .Include(r => r.Alertas)
                .FirstOrDefaultAsync(r => r.Id == sensor.RegiaoId);

            if (regiao == null)
                throw new InvalidOperationException(
                    "A região vinculada ao sensor não foi encontrada.");

            regiao.AtualizadoEm = agora;

            var alerta = regiao.Alertas
                .FirstOrDefault(a => a.SensorId == sensor.Id && a.Ativo);

            if (nivel == "NORMAL")
            {
                if (alerta != null)
                {
                    alerta.Ativo = false;
                    alerta.Status = "RESOLVIDO_AUTO";
                    alerta.DataResolucao = agora;
                    alerta.MedicaoUltimaId = medicao.Id;
                    alerta.DataUltimaOcorrencia = agora;
                }
            }
            else
            {
                if (alerta == null)
                {
                    alerta = new Alerta
                    {
                        Id = Guid.NewGuid(),
                        RegiaoId = regiao.Id,
                        SensorId = sensor.Id,
                        MedicaoOrigemId = medicao.Id,
                        MedicaoUltimaId = medicao.Id,
                        Nivel = nivel,
                        Status = "ATIVO",
                        Ativo = true,
                        Titulo = $"Alerta de {nivel.ToLower()}",
                        DataEmissao = agora,
                        DataUltimaOcorrencia = agora
                    };

                    regiao.Alertas.Add(alerta);
                }
                else
                {
                    alerta.Nivel = nivel;
                    alerta.Status = "ATIVO";
                    alerta.Ativo = true;
                    alerta.MedicaoUltimaId = medicao.Id;
                    alerta.DataUltimaOcorrencia = agora;
                    alerta.Titulo = $"Alerta de {nivel.ToLower()}";
                }
            }

            regiao.NivelRiscoAtual = CalcularRiscoRegiao(regiao);
        }

        private static string CalcularRiscoRegiao(Regiao regiao)
        {
            var niveis = regiao.Alertas
                .Where(a => a.Ativo)
                .Select(a => a.Nivel);

            if (niveis.Contains("PERIGO"))
                return "PERIGO";

            if (niveis.Contains("ALERTA"))
                return "ALERTA";

            if (niveis.Contains("ATENCAO"))
                return "ATENCAO";

            return "NORMAL";
        }

    }
}