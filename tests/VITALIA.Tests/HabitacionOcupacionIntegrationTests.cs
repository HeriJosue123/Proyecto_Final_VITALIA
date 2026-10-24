using Microsoft.EntityFrameworkCore;
using VITALIA.Data;
using Xunit;

namespace VITALIA.Tests;

/// <summary>
/// Pruebas de integración contra la base de datos SQL Server (VITALIADB).
/// Trasladadas desde Controllers/TestOcupacionController.cs (Sección 5).
/// REGLA DE SEGURIDAD: Todas las consultas se realizan de forma estrictamente de solo lectura (AsNoTracking).
/// No se insertan, modifican ni eliminan registros de la base de datos real.
/// </summary>
public class HabitacionOcupacionIntegrationTests
{
    private const string ConnectionString = "Server=localhost;Database=VITALIADB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    private VitaliaDbContext CreateReadOnlyContext()
    {
        var options = new DbContextOptionsBuilder<VitaliaDbContext>()
            .UseSqlServer(ConnectionString)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new VitaliaDbContext(options);
    }

    [Fact]
    public async Task BaseDeDatos_VerificarHabitacionesRegistradasEnVITALIADB()
    {
        using var context = CreateReadOnlyContext();

        if (!await context.Database.CanConnectAsync())
        {
            // Limitación documentada: si el servidor local de SQL Server no está disponible en el entorno
            return;
        }

        // 5.1 Base de datos: Hay habitaciones registradas en VITALIADB
        var habsDb = await context.Habitaciones.AsNoTracking().Include(h => h.Residentes).ToListAsync();
        Assert.NotEmpty(habsDb);
    }

    [Fact]
    public async Task BaseDeDatos_VerificarEstadoCoherenteHabitacion205b()
    {
        using var context = CreateReadOnlyContext();

        if (!await context.Database.CanConnectAsync())
        {
            return;
        }

        var hab205bDb = await context.Habitaciones
            .AsNoTracking()
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.Numero == "205b");

        Assert.NotNull(hab205bDb);

        // Validación dinámica que preserva los casos esperados de TestOcupacionController
        if (hab205bDb.ResidentesAsignados == 0)
        {
            // 5.2 BD 205b vacia: Su EstadoCoherente es 'Disponible'
            Assert.Equal("Disponible", hab205bDb.EstadoCoherente);

            // 5.3 BD 205b vacia: Tiene 1 plaza disponible
            Assert.Equal(1, hab205bDb.PlazasDisponibles);

            // 5.4 BD 205b vacia: Si figura guardada como 'Ocupada', detecta la inconsistencia
            if (string.Equals(hab205bDb.Estado, "Ocupada", StringComparison.OrdinalIgnoreCase))
            {
                Assert.True(hab205bDb.TieneInconsistenciaEstado);
            }
            else
            {
                Assert.False(hab205bDb.TieneInconsistenciaEstado);
            }
        }
        else
        {
            // 5.2 BD 205b ocupada: Tiene 1 residente asignado
            Assert.Equal(1, hab205bDb.ResidentesAsignados);

            // 5.3 BD 205b ocupada: Su EstadoCoherente es 'Ocupada'
            Assert.Equal("Ocupada", hab205bDb.EstadoCoherente);

            // 5.4 BD 205b ocupada: Tiene 0 plazas disponibles
            Assert.Equal(0, hab205bDb.PlazasDisponibles);

            // 5.5 BD 205b ocupada: Estado sincronizado correctamente sin inconsistencia
            Assert.False(hab205bDb.TieneInconsistenciaEstado);
        }
    }

    [Fact]
    public async Task BaseDeDatos_VerificarEstadoCoherenteHabitacion101()
    {
        using var context = CreateReadOnlyContext();

        if (!await context.Database.CanConnectAsync())
        {
            return;
        }

        var hab101Db = await context.Habitaciones
            .AsNoTracking()
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.Numero == "101");

        Assert.NotNull(hab101Db);

        // 5.6 BD 101: Su EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hab101Db.EstadoCoherente);

        // 5.7 BD 101: No tiene inconsistencia en BD
        Assert.False(hab101Db.TieneInconsistenciaEstado);
    }

    [Fact]
    public async Task BaseDeDatos_VerificarEstadoCoherenteHabitacion201()
    {
        using var context = CreateReadOnlyContext();

        if (!await context.Database.CanConnectAsync())
        {
            return;
        }

        var hab201Db = await context.Habitaciones
            .AsNoTracking()
            .Include(h => h.Residentes)
            .FirstOrDefaultAsync(h => h.Numero == "201");

        Assert.NotNull(hab201Db);

        // 5.8 BD 201: Su EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hab201Db.EstadoCoherente);

        // 5.9 BD 201: No tiene inconsistencia en BD
        Assert.False(hab201Db.TieneInconsistenciaEstado);
    }
}
