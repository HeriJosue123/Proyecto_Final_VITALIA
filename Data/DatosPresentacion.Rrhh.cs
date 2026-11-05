using Microsoft.EntityFrameworkCore;
using VITALIA.Models.Rrhh;

namespace VITALIA.Data;

/// <summary>
/// Parte de RRHH de los datos de presentación: contratos y nóminas del personal,
/// permisos y el personal externo (cocina, limpieza, seguridad, mantenimiento y jardinería).
/// </summary>
public static partial class DatosPresentacion
{
    // ---------------------------------------------------------------- contratos (uno por persona del personal, mismo orden)

    private record ContratoDato(string Tipo, decimal Salario, int DiasDesdeInicio, int? DiasParaTerminar = null);

    private static readonly ContratoDato[] Contratos =
    {
        new("Indefinido", 1450m, 900),          // Ricardo Menjívar, médico general
        new("Indefinido", 1380m, 640),          // Patricia Villalta, médica geriatra
        new("Indefinido", 980m, 1100),          // Sandra Alvarado, enfermera jefe
        new("Indefinido", 820m, 480),           // Marvin Orellana, enfermero
        new("Indefinido", 800m, 350),           // Gloria Campos, enfermera
        new("Indefinido", 520m, 700),           // Jorge Aguilar, cuidador
        new("Indefinido", 520m, 560),           // Rosa Escobar, cuidadora
        new("Indefinido", 500m, 410),           // Wilfredo Cruz, cuidador
        new("Temporal", 480m, 160, 20),         // Xiomara Henríquez, cuidadora: contrato por vencer
        new("Indefinido", 1650m, 1500),         // Claudia Arévalo, directora administrativa
        new("Indefinido", 480m, 800, -120)      // Alicia Benítez: ya no trabaja, contrato terminado
    };

    // ---------------------------------------------------------------- permisos

    private record PermisoDato(int Persona, string Motivo, int Desde, int Hasta, string Estado, string Observacion);

    private static readonly PermisoDato[] Permisos =
    {
        new(3, "Maternidad / Paternidad", -40, -28, "Aprobado", "Licencia por el nacimiento de su hija."),
        new(6, "Vacaciones", -20, -14, "Aprobado", "Vacaciones anuales."),
        new(2, "Incapacidad Médica", -9, -7, "Aprobado", "Incapacidad del ISSS por cuadro gripal."),
        new(7, "Duelo / Luto", -3, -1, "Aprobado", "Fallecimiento de su abuela en Chalatenango."),
        new(5, "Permiso Personal", 3, 3, "Pendiente", "Trámite de renovación de DUI y licencia."),
        new(0, "Permiso Personal", 8, 8, "Pendiente", "Congreso de geriatría en San Salvador."),
        new(4, "Vacaciones", 15, 22, "Pendiente", "Viaje familiar a Guatemala."),
        new(8, "Vacaciones", 45, 52, "Rechazado", "Coincide con el turno de fin de año; se reprogramará para enero.")
    };

    // ---------------------------------------------------------------- personal externo

    private record ExternoDato(
        string Nombre, string Apellido, string Dui, string Empresa, string Area,
        string Telefono, string Horario, decimal Monto, string EstadoPago, bool Activo = true);

    private static readonly ExternoDato[] Externos =
    {
        new("Marta Lidia", "Rauda", "04567812-3", "Particular", "Cocina General", "7623-4410", "Lunes a viernes, 6:00 a 14:00", 420m, "Pagado"),
        new("Juan Carlos", "Pacheco", "03918274-6", "Particular", "Cocina General", "7712-0385", "Fines de semana, 6:00 a 14:00", 210m, "Pendiente"),
        new("Ana Sofía", "Cornejo", "05122938-1", "Servicios de Limpieza Integral", "Limpieza", "7488-2261", "Lunes a sábado, 7:00 a 15:00", 380m, "Pagado"),
        new("Kevin Alexander", "Mejicanos", "06031452-8", "Servicios de Limpieza Integral", "Limpieza", "6145-9032", "Lunes a sábado, 14:00 a 22:00", 380m, "Pendiente"),
        new("Douglas Ernesto", "Calderón", "02874519-0", "Seguridad Privada El Roble", "Seguridad", "7350-6618", "Turno nocturno, 18:00 a 6:00", 550m, "Pendiente"),
        new("Mario Antonio", "Ventura", "01957364-2", "Particular", "Mantenimiento", "7031-8847", "Martes y jueves, 8:00 a 12:00", 260m, "Pagado"),
        new("Felipe de Jesús", "Amaya", "03366901-4", "Particular", "Jardinería y Exteriores", "7905-2214", "Sábados, 7:00 a 11:00", 120m, "Pagado"),
        new("Reina Isabel", "Portal", "04410287-9", "Particular", "Cocina General", "7264-1193", "Lunes a viernes, 6:00 a 14:00", 400m, "Pagado", Activo: false)
    };

    // ================================================================ cargar

    /// <summary>Contratos, nóminas y permisos del personal del paquete, más el personal externo. Devuelve cuántos registros creó.</summary>
    private static async Task<int> CargarRrhhAsync(VitaliaDbContext db, int[] empleados, DateTime ahora)
    {
        int total = 0;
        var hoy = ahora.Date;

        for (int i = 0; i < Personal.Length; i++)
        {
            var idEmpleado = empleados[i];
            if (await db.ContratosEmpleado.AnyAsync(c => c.IdEmpleado == idEmpleado)) continue; // ya tenía su parte de RRHH

            var c = Contratos[i];
            var activo = Personal[i].Activo;
            db.ContratosEmpleado.Add(new ContratoEmpleado
            {
                IdEmpleado = idEmpleado,
                TipoContrato = c.Tipo,
                FechaInicio = hoy.AddDays(-c.DiasDesdeInicio),
                FechaFin = c.DiasParaTerminar is int dias ? hoy.AddDays(dias) : null,
                SalarioBase = c.Salario,
                Activo = activo
            });
            total++;

            // Nóminas de los dos meses anteriores (pagadas) y del mes actual (pendiente)
            if (activo)
            {
                for (int atras = 2; atras >= 0; atras--)
                {
                    var mes = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(-atras);
                    var deducciones = Deducciones(c.Salario);
                    db.Nominas.Add(new Nomina
                    {
                        IdEmpleado = idEmpleado,
                        Mes = mes.Month,
                        Anio = mes.Year,
                        SalarioBruto = c.Salario,
                        Deducciones = deducciones,
                        SalarioNeto = c.Salario - deducciones,
                        FechaPago = mes.AddMonths(1).AddDays(-1),
                        Estado = atras == 0 ? "Pendiente" : "Pagado"
                    });
                    total++;
                }
            }

            foreach (var p in Permisos.Where(p => p.Persona == i))
            {
                db.SolicitudesPermiso.Add(new SolicitudPermiso
                {
                    IdEmpleado = idEmpleado,
                    TipoMotivo = p.Motivo,
                    FechaInicio = hoy.AddDays(p.Desde),
                    FechaFin = hoy.AddDays(p.Hasta),
                    Observacion = p.Observacion,
                    Estado = p.Estado
                });
                total++;
            }
        }

        foreach (var e in Externos)
        {
            if (await db.PersonalExterno.AnyAsync(x => x.DocumentoIdentidad == e.Dui)) continue;
            db.PersonalExterno.Add(new PersonalExterno
            {
                Nombre = e.Nombre,
                Apellido = e.Apellido,
                DocumentoIdentidad = e.Dui,
                EmpresaProveedora = e.Empresa,
                AreaServicio = e.Area,
                Telefono = e.Telefono,
                HorarioTurno = e.Horario,
                MontoServicio = e.Monto,
                EstadoPago = e.EstadoPago,
                Activo = e.Activo
            });
            total++;
        }

        await db.SaveChangesAsync();
        return total;
    }

    /// <summary>Descuentos de ley en El Salvador: ISSS 3 % (tope de $1,000), AFP 7.25 % y renta por tramos (mensual).</summary>
    private static decimal Deducciones(decimal salario)
    {
        var isss = Math.Min(salario, 1000m) * 0.03m;
        var afp = salario * 0.0725m;
        var gravado = salario - isss - afp;
        var renta = gravado <= 550m ? 0m
            : gravado <= 895.24m ? (gravado - 550m) * 0.10m + 17.67m
            : gravado <= 2038.10m ? (gravado - 895.24m) * 0.20m + 60m
            : (gravado - 2038.10m) * 0.30m + 288.57m;
        return Math.Round(isss + afp + renta, 2);
    }

    // ================================================================ quitar

    /// <summary>Quita lo de RRHH del personal del paquete y el personal externo del paquete.</summary>
    private static async Task<int> QuitarRrhhAsync(VitaliaDbContext db, List<int> empleados)
    {
        var contratos = await db.ContratosEmpleado.Where(c => empleados.Contains(c.IdEmpleado)).ToListAsync();
        var nominas = await db.Nominas.Where(n => empleados.Contains(n.IdEmpleado)).ToListAsync();
        var permisos = await db.SolicitudesPermiso.Where(p => empleados.Contains(p.IdEmpleado)).ToListAsync();
        var duis = Externos.Select(e => e.Dui).ToList();
        var externos = await db.PersonalExterno.Where(x => duis.Contains(x.DocumentoIdentidad)).ToListAsync();

        db.ContratosEmpleado.RemoveRange(contratos);
        db.Nominas.RemoveRange(nominas);
        db.SolicitudesPermiso.RemoveRange(permisos);
        db.PersonalExterno.RemoveRange(externos);
        await db.SaveChangesAsync();
        return contratos.Count + nominas.Count + permisos.Count + externos.Count;
    }
}
