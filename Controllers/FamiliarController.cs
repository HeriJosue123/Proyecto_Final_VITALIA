using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using VITALIA.Models;
using VITALIA.Models.ViewModels;

namespace VITALIA.Controllers
{
    // Vista del familiar: solo lectura y solo de los residentes que tiene vinculados
    [Authorize(Roles = RolesSistema.Familiar)]
    public class FamiliarController : Controller
    {
        private readonly VitaliaDbContext _context;

        public FamiliarController(VitaliaDbContext context)
        {
            _context = context;
        }

        private int IdUsuarioActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

        // GET: Familiar
        public async Task<IActionResult> Index()
        {
            var vinculos = await _context.FamiliarResidentes
                .Include(f => f.IdResidenteNavigation)
                .Where(f => f.IdUsuario == IdUsuarioActual && f.IdResidenteNavigation.Estado)
                .OrderBy(f => f.IdResidenteNavigation.Nombre)
                .ToListAsync();

            var ids = vinculos.Select(v => v.IdResidente).ToList();

            var equipo = await _context.AsignacionCuidados
                .Include(a => a.IdEmpleadoNavigation)
                .Where(a => ids.Contains(a.IdResidente) && a.Estado)
                .ToListAsync();

            var alertas = await _context.Alerta
                .Where(a => ids.Contains(a.IdResidente) && a.EstadoActual != AlertaEstados.Atendida)
                .GroupBy(a => a.IdResidente)
                .Select(g => new { IdResidente = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var ultimas = await _context.AtencionDiaria
                .Where(a => ids.Contains(a.IdResidente))
                .GroupBy(a => a.IdResidente)
                .Select(g => new { IdResidente = g.Key, Fecha = g.Max(a => a.FechaHora) })
                .ToListAsync();

            var tarjetas = vinculos.Select(v => new FamiliarResidenteTarjeta
            {
                IdResidente = v.IdResidente,
                Nombre = v.IdResidenteNavigation.Nombre + " " + v.IdResidenteNavigation.Apellido,
                CodigoUnico = v.IdResidenteNavigation.CodigoUnico,
                Edad = Edades.Calcular(v.IdResidenteNavigation.FechaNacimiento),
                Habitacion = v.IdResidenteNavigation.NumHabitacion,
                Parentesco = v.Parentesco,
                Equipo = equipo.Where(a => a.IdResidente == v.IdResidente)
                    .Select(a => a.IdEmpleadoNavigation.Nombre + " " + a.IdEmpleadoNavigation.Apellido + " · " + a.IdEmpleadoNavigation.Cargo)
                    .ToList(),
                AlertasActivas = alertas.FirstOrDefault(a => a.IdResidente == v.IdResidente)?.Cantidad ?? 0,
                UltimaAtencion = ultimas.FirstOrDefault(u => u.IdResidente == v.IdResidente)?.Fecha
            }).ToList();

            return View(tarjetas);
        }

        // GET: Familiar/Residente/5
        public async Task<IActionResult> Residente(int id)
        {
            // Solo puede ver a los residentes que tiene vinculados
            var vinculo = await _context.FamiliarResidentes
                .Include(f => f.IdResidenteNavigation)
                .FirstOrDefaultAsync(f => f.IdUsuario == IdUsuarioActual && f.IdResidente == id && f.IdResidenteNavigation.Estado);

            if (vinculo == null)
            {
                return Forbid();
            }

            var equipo = await _context.AsignacionCuidados
                .Include(a => a.IdEmpleadoNavigation).ThenInclude(e => e.IdUsuarioNavigation).ThenInclude(u => u!.IdRolNavigation)
                .Where(a => a.IdResidente == id && a.Estado)
                .OrderBy(a => a.FechaInicio)
                .ToListAsync();

            var atenciones = await _context.AtencionDiaria
                .Include(a => a.IdTipoAtencionNavigation)
                .Include(a => a.IdEmpleadoNavigation)
                .Include(a => a.CorreccionAtencions)
                .Where(a => a.IdResidente == id)
                .OrderByDescending(a => a.FechaHora)
                .ToListAsync();

            var alertas = await _context.Alerta
                .Include(a => a.IdEmpleadoResponsableNavigation)
                .Where(a => a.IdResidente == id)
                .OrderBy(a => a.FechaProgramada)
                .ToListAsync();

            // Medicamentos entregados (módulo Farmacia). Si la base todavía no tiene esas tablas,
            // la ficha se muestra igual y solo avisa que la información no está disponible.
            List<FamiliarMedicamentoItem> medicamentos = new();
            bool medicamentosDisponibles = true;
            try
            {
                medicamentos = await _context.EntregaMedicamentos
                    .Where(e => e.IdResidente == id)
                    .OrderByDescending(e => e.FechaHora)
                    .Select(e => new FamiliarMedicamentoItem
                    {
                        FechaHora = e.FechaHora,
                        Medicamento = e.IdLoteNavigation.IdMedicamentoNavigation.Nombre,
                        Presentacion = e.IdLoteNavigation.IdMedicamentoNavigation.Presentacion,
                        Cantidad = e.Cantidad,
                        EntregadoPor = e.IdEmpleadoNavigation.Nombre + " " + e.IdEmpleadoNavigation.Apellido,
                        Observaciones = e.Observaciones
                    })
                    .ToListAsync();
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                medicamentosDisponibles = false;
            }

            FamiliarAlertaItem AItem(Alerta a) => new()
            {
                Titulo = a.Titulo,
                Descripcion = a.Descripcion,
                FechaProgramada = a.FechaProgramada,
                Prioridad = a.Prioridad,
                Estado = a.EstadoActual,
                Responsable = a.IdEmpleadoResponsableNavigation != null
                    ? a.IdEmpleadoResponsableNavigation.Nombre + " " + a.IdEmpleadoResponsableNavigation.Apellido
                    : null
            };

            var modelo = new FamiliarFichaViewModel
            {
                Residente = vinculo.IdResidenteNavigation,
                Edad = Edades.Calcular(vinculo.IdResidenteNavigation.FechaNacimiento),
                Parentesco = vinculo.Parentesco,
                Equipo = equipo.Select(a => new FamiliarEquipoItem
                {
                    Nombre = a.IdEmpleadoNavigation.Nombre + " " + a.IdEmpleadoNavigation.Apellido,
                    Cargo = a.IdEmpleadoNavigation.Cargo,
                    Tipo = TiposAsignacion.De(a.IdEmpleadoNavigation),
                    Desde = a.FechaInicio
                }).ToList(),
                // Las atenciones anuladas no se muestran; las corregidas llevan el motivo
                Atenciones = atenciones
                    .Where(a => !a.CorreccionAtencions.Any(c => c.TipoAccion == "Anulación"))
                    .Select(a => new FamiliarAtencionItem
                    {
                        FechaHora = a.FechaHora,
                        Tipo = a.IdTipoAtencionNavigation.NombreCategoria,
                        Observaciones = a.Observaciones,
                        AtendidoPor = a.IdEmpleadoNavigation.Nombre + " " + a.IdEmpleadoNavigation.Apellido,
                        MotivoCorreccion = a.CorreccionAtencions
                            .Where(c => c.TipoAccion == "Corrección")
                            .OrderByDescending(c => c.FechaHora)
                            .Select(c => c.Motivo)
                            .FirstOrDefault()
                    }).ToList(),
                AlertasActivas = alertas.Where(a => a.EstadoActual != AlertaEstados.Atendida).Select(AItem).ToList(),
                AlertasAtendidas = alertas.Where(a => a.EstadoActual == AlertaEstados.Atendida)
                    .OrderByDescending(a => a.FechaProgramada).Select(AItem).ToList(),
                Medicamentos = medicamentos,
                MedicamentosDisponibles = medicamentosDisponibles
            };

            return View(modelo);
        }
    }
}
