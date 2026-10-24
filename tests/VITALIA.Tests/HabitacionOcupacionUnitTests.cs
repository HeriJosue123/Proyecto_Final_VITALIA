using VITALIA.Models;
using VITALIA.Models.ViewModels;
using Xunit;

namespace VITALIA.Tests;

/// <summary>
/// Pruebas unitarias de cálculo de ocupación y coherencia de estado de habitaciones.
/// Trasladadas desde Controllers/TestOcupacionController.cs (Secciones 1 a 4).
/// Ejecución 100% en memoria e independiente de base de datos.
/// </summary>
public class HabitacionOcupacionUnitTests
{
    // ==========================================
    // SECCIÓN 1: MODELO HABITACION (32 Aserciones)
    // ==========================================

    [Fact]
    public void Habitacion_205b_VaciaConEstadoOcupada_DetectaInconsistenciaYEstadoCoherenteDisponible()
    {
        // Caso específico detectado originalmente en la habitación 205b
        var h205b = new Habitacion { Numero = "205b", Capacidad = 1, Estado = "Ocupada" };

        // 1.1 Habitacion 205b: ResidentesAsignados es 0
        Assert.Equal(0, h205b.ResidentesAsignados);

        // 1.2 Habitacion 205b: PlazasDisponibles es 1
        Assert.Equal(1, h205b.PlazasDisponibles);

        // 1.3 Habitacion 205b: No esta completa
        Assert.False(h205b.EstaCompleta);

        // 1.4 Habitacion 205b: EstadoCoherente es 'Disponible' aunque en BD este como 'Ocupada'
        Assert.Equal("Disponible", h205b.EstadoCoherente);

        // 1.5 Habitacion 205b: TieneInconsistenciaEstado es True
        Assert.True(h205b.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_VaciaConEstadoDisponible_SinInconsistencia()
    {
        var hVaciaDisp = new Habitacion { Numero = "101", Capacidad = 4, Estado = "Disponible" };

        // 1.6 Habitacion vacia con Estado 'Disponible' -> EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hVaciaDisp.EstadoCoherente);

        // 1.7 Habitacion vacia con Estado 'Disponible' -> TieneInconsistenciaEstado es False
        Assert.False(hVaciaDisp.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_VaciaEnMantenimiento_ConservaEstadoSinInconsistencia()
    {
        var hVaciaMant = new Habitacion { Numero = "102", Capacidad = 2, Estado = "Mantenimiento" };

        // 1.8 Habitacion vacia en Mantenimiento -> EstadoCoherente es 'Mantenimiento'
        Assert.Equal("Mantenimiento", hVaciaMant.EstadoCoherente);

        // 1.9 Habitacion vacia en Mantenimiento -> TieneInconsistenciaEstado es False
        Assert.False(hVaciaMant.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_VaciaInactiva_ConservaEstadoSinInconsistencia()
    {
        var hVaciaInact = new Habitacion { Numero = "103", Capacidad = 2, Estado = "Inactiva" };

        // 1.10 Habitacion vacia Inactiva -> EstadoCoherente es 'Inactiva'
        Assert.Equal("Inactiva", hVaciaInact.EstadoCoherente);

        // 1.11 Habitacion vacia Inactiva -> TieneInconsistenciaEstado es False
        Assert.False(hVaciaInact.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_OcupacionParcial_ConEstadoDisponible_CalculaPlazasYCoherencia()
    {
        var hParcial = new Habitacion { Numero = "201", Capacidad = 3, Estado = "Disponible" };
        hParcial.Residentes.Add(new Residente { Nombre = "Juan", Apellido = "Perez" });

        // 1.12 Ocupacion parcial: ResidentesAsignados es 1
        Assert.Equal(1, hParcial.ResidentesAsignados);

        // 1.13 Ocupacion parcial: PlazasDisponibles es 2
        Assert.Equal(2, hParcial.PlazasDisponibles);

        // 1.14 Ocupacion parcial: EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hParcial.EstadoCoherente);

        // 1.15 Ocupacion parcial con Estado 'Disponible' -> Sin inconsistencia
        Assert.False(hParcial.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_OcupacionParcial_GuardadaComoOcupada_DetectaInconsistencia()
    {
        var hParcialInconsistente = new Habitacion { Numero = "202", Capacidad = 3, Estado = "Ocupada" };
        hParcialInconsistente.Residentes.Add(new Residente { Nombre = "Ana", Apellido = "Gomez" });

        // 1.16 Ocupacion parcial guardada como 'Ocupada' -> EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hParcialInconsistente.EstadoCoherente);

        // 1.17 Ocupacion parcial guardada como 'Ocupada' -> TieneInconsistenciaEstado es True
        Assert.True(hParcialInconsistente.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_Llena_ConEstadoOcupada_CoherenteSinInconsistencia()
    {
        var hLlena = new Habitacion { Numero = "301", Capacidad = 2, Estado = "Ocupada" };
        hLlena.Residentes.Add(new Residente { Nombre = "Carlos", Apellido = "Lopez" });
        hLlena.Residentes.Add(new Residente { Nombre = "Maria", Apellido = "Ruiz" });

        // 1.18 Habitacion llena: ResidentesAsignados es 2
        Assert.Equal(2, hLlena.ResidentesAsignados);

        // 1.19 Habitacion llena: PlazasDisponibles es 0
        Assert.Equal(0, hLlena.PlazasDisponibles);

        // 1.20 Habitacion llena: EstaCompleta es True
        Assert.True(hLlena.EstaCompleta);

        // 1.21 Habitacion llena: EstadoCoherente es 'Ocupada'
        Assert.Equal("Ocupada", hLlena.EstadoCoherente);

        // 1.22 Habitacion llena guardada como 'Ocupada' -> Sin inconsistencia
        Assert.False(hLlena.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_Llena_GuardadaComoDisponible_DetectaInconsistencia()
    {
        var hLlenaInconsistente = new Habitacion { Numero = "302", Capacidad = 2, Estado = "Disponible" };
        hLlenaInconsistente.Residentes.Add(new Residente { Nombre = "Luis", Apellido = "Mendoza" });
        hLlenaInconsistente.Residentes.Add(new Residente { Nombre = "Rosa", Apellido = "Flores" });

        // 1.23 Habitacion llena guardada como 'Disponible' -> EstadoCoherente es 'Ocupada'
        Assert.Equal("Ocupada", hLlenaInconsistente.EstadoCoherente);

        // 1.24 Habitacion llena guardada como 'Disponible' -> TieneInconsistenciaEstado es True
        Assert.True(hLlenaInconsistente.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_Llena_GuardadaComoMantenimiento_CorrigeAOcupadaYDetectaInconsistencia()
    {
        var hLlenaMant = new Habitacion { Numero = "303", Capacidad = 2, Estado = "Mantenimiento" };
        hLlenaMant.Residentes.Add(new Residente { Nombre = "Pedro", Apellido = "Reyes" });
        hLlenaMant.Residentes.Add(new Residente { Nombre = "Elena", Apellido = "Sol" });

        // 1.25 Habitacion llena guardada como 'Mantenimiento' -> EstadoCoherente es 'Ocupada'
        Assert.Equal("Ocupada", hLlenaMant.EstadoCoherente);

        // 1.26 Habitacion llena guardada como 'Mantenimiento' -> TieneInconsistenciaEstado es True
        Assert.True(hLlenaMant.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_OcupacionParcial_GuardadaComoMantenimiento_CorrigeADisponibleYDetectaInconsistencia()
    {
        var hParcialMant = new Habitacion { Numero = "304", Capacidad = 3, Estado = "Mantenimiento" };
        hParcialMant.Residentes.Add(new Residente { Nombre = "Sara", Apellido = "Castro" });

        // 1.27 Ocupacion parcial guardada como 'Mantenimiento' -> EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hParcialMant.EstadoCoherente);

        // 1.28 Ocupacion parcial guardada como 'Mantenimiento' -> TieneInconsistenciaEstado es True
        Assert.True(hParcialMant.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_OcupacionParcial_GuardadaComoInactiva_CorrigeADisponibleYDetectaInconsistencia()
    {
        var hParcialInact = new Habitacion { Numero = "305", Capacidad = 3, Estado = "Inactiva" };
        hParcialInact.Residentes.Add(new Residente { Nombre = "Mario", Apellido = "Luna" });

        // 1.29 Ocupacion parcial guardada como 'Inactiva' -> EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", hParcialInact.EstadoCoherente);

        // 1.30 Ocupacion parcial guardada como 'Inactiva' -> TieneInconsistenciaEstado es True
        Assert.True(hParcialInact.TieneInconsistenciaEstado);
    }

    [Fact]
    public void Habitacion_VaciaConEspaciosYMinusculas_NormalizaEstadoCoherente()
    {
        var hVaciaTrim = new Habitacion { Numero = "306", Capacidad = 2, Estado = " mantenimiento " };

        // 1.31 Vacia con espacios/minusculas en estado -> EstadoCoherente normalizado
        Assert.Equal("mantenimiento", hVaciaTrim.EstadoCoherente);

        // 1.32 Vacia con espacios/minusculas en estado -> Sin inconsistencia
        Assert.False(hVaciaTrim.TieneInconsistenciaEstado);
    }

    // ==========================================
    // SECCIÓN 2: HABITACION ITEM VIEWMODEL (4 Aserciones)
    // ==========================================

    [Fact]
    public void HabitacionItemViewModel_205bVacia_DetectaEstadoCoherenteEInconsistencia()
    {
        var item205b = new HabitacionItemViewModel { Numero = "205b", Capacidad = 1, Estado = "Ocupada", ResidentesAsignados = 0 };

        // 2.1 ViewModel 205b: EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", item205b.EstadoCoherente);

        // 2.2 ViewModel 205b: TieneInconsistenciaEstado es True
        Assert.True(item205b.TieneInconsistenciaEstado);
    }

    [Fact]
    public void HabitacionItemViewModel_Llena_DetectaEstadoCoherenteSinInconsistencia()
    {
        var itemLlena = new HabitacionItemViewModel { Numero = "101", Capacidad = 2, Estado = "Ocupada", ResidentesAsignados = 2 };

        // 2.3 ViewModel Llena: EstadoCoherente es 'Ocupada'
        Assert.Equal("Ocupada", itemLlena.EstadoCoherente);

        // 2.4 ViewModel Llena: TieneInconsistenciaEstado es False
        Assert.False(itemLlena.TieneInconsistenciaEstado);
    }

    // ==========================================
    // SECCIÓN 3: CAMBIAR ESTADO VIEWMODEL (15 Aserciones)
    // ==========================================

    [Fact]
    public void CambiarEstadoViewModel_HabitacionVacia_ValidaTransicionesPermitidas()
    {
        var ceVacia = new CambiarEstadoViewModel { Numero = "205b", Capacidad = 1, EstadoActual = "Ocupada", ResidentesAsignados = 0 };

        // 3.1 CambiarEstado Vacia: No permite 'Ocupada'
        Assert.False(ceVacia.EsEstadoPermitido("Ocupada", out _));

        // 3.2 CambiarEstado Vacia: Permite 'Disponible'
        Assert.True(ceVacia.EsEstadoPermitido("Disponible", out _));

        // 3.3 CambiarEstado Vacia: Permite 'Mantenimiento'
        Assert.True(ceVacia.EsEstadoPermitido("Mantenimiento", out _));

        // 3.4 CambiarEstado Vacia: Permite 'Inactiva'
        Assert.True(ceVacia.EsEstadoPermitido("Inactiva", out _));

        // 3.5 CambiarEstado Vacia 205b: TieneInconsistenciaEstado es True
        Assert.True(ceVacia.TieneInconsistenciaEstado);
    }

    [Fact]
    public void CambiarEstadoViewModel_HabitacionOcupacionParcial_ValidaTransicionesPermitidas()
    {
        var ceParcial = new CambiarEstadoViewModel { Numero = "101", Capacidad = 3, EstadoActual = "Disponible", ResidentesAsignados = 1 };

        // 3.6 CambiarEstado Parcial: No permite 'Ocupada' (plazas libres)
        Assert.False(ceParcial.EsEstadoPermitido("Ocupada", out _));

        // 3.7 CambiarEstado Parcial: Permite 'Disponible'
        Assert.True(ceParcial.EsEstadoPermitido("Disponible", out _));

        // 3.8 CambiarEstado Parcial: No permite 'Mantenimiento'
        Assert.False(ceParcial.EsEstadoPermitido("Mantenimiento", out _));

        // 3.9 CambiarEstado Parcial: No permite 'Inactiva'
        Assert.False(ceParcial.EsEstadoPermitido("Inactiva", out _));

        // 3.10 CambiarEstado Parcial 101: TieneInconsistenciaEstado es False
        Assert.False(ceParcial.TieneInconsistenciaEstado);
    }

    [Fact]
    public void CambiarEstadoViewModel_HabitacionLlena_ValidaTransicionesPermitidas()
    {
        var ceLlena = new CambiarEstadoViewModel { Numero = "102", Capacidad = 2, EstadoActual = "Ocupada", ResidentesAsignados = 2 };

        // 3.11 CambiarEstado Llena: No permite 'Disponible' (capacidad completa)
        Assert.False(ceLlena.EsEstadoPermitido("Disponible", out _));

        // 3.12 CambiarEstado Llena: Permite 'Ocupada'
        Assert.True(ceLlena.EsEstadoPermitido("Ocupada", out _));

        // 3.13 CambiarEstado Llena: No permite 'Mantenimiento'
        Assert.False(ceLlena.EsEstadoPermitido("Mantenimiento", out _));

        // 3.14 CambiarEstado Llena 102: TieneInconsistenciaEstado es False
        Assert.False(ceLlena.TieneInconsistenciaEstado);
    }

    [Fact]
    public void CambiarEstadoViewModel_RechazaEstadosDesconocidos()
    {
        var ceVacia = new CambiarEstadoViewModel { Numero = "205b", Capacidad = 1, EstadoActual = "Ocupada", ResidentesAsignados = 0 };

        // 3.15 CambiarEstado: Rechaza estados desconocidos
        Assert.False(ceVacia.EsEstadoPermitido("Invalido123", out _));
    }

    // ==========================================
    // SECCIÓN 4: HABITACION OPCION VIEWMODEL (5 Aserciones)
    // ==========================================

    [Fact]
    public void HabitacionOpcionViewModel_205bVacia_HabilitadaParaAsignar()
    {
        var op205b = new HabitacionOpcionViewModel { Numero = "205b", Capacidad = 1, Estado = "Ocupada", ResidentesAsignados = 0 };

        // 4.1 Selector 205b: EstadoCoherente es 'Disponible'
        Assert.Equal("Disponible", op205b.EstadoCoherente);

        // 4.2 Selector 205b: EstaHabilitadaParaAsignar es True
        Assert.True(op205b.EstaHabilitadaParaAsignar);
    }

    [Fact]
    public void HabitacionOpcionViewModel_Llena_NoHabilitadaParaAsignar()
    {
        var opLlena = new HabitacionOpcionViewModel { Numero = "101", Capacidad = 1, Estado = "Ocupada", ResidentesAsignados = 1 };

        // 4.3 Selector Llena: EstaHabilitadaParaAsignar es False
        Assert.False(opLlena.EstaHabilitadaParaAsignar);
    }

    [Fact]
    public void HabitacionOpcionViewModel_Mantenimiento_NoHabilitadaParaAsignar()
    {
        var opMant = new HabitacionOpcionViewModel { Numero = "102", Capacidad = 2, Estado = "Mantenimiento", ResidentesAsignados = 0 };

        // 4.4 Selector Mantenimiento: EstaHabilitadaParaAsignar es False
        Assert.False(opMant.EstaHabilitadaParaAsignar);
    }

    [Fact]
    public void HabitacionOpcionViewModel_Inactiva_NoHabilitadaParaAsignar()
    {
        var opInact = new HabitacionOpcionViewModel { Numero = "103", Capacidad = 2, Estado = "Inactiva", ResidentesAsignados = 0 };

        // 4.5 Selector Inactiva: EstaHabilitadaParaAsignar es False
        Assert.False(opInact.EstaHabilitadaParaAsignar);
    }
}
