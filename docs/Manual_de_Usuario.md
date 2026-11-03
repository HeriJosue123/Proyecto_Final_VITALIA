# VITALIA: manual de usuario

VITALIA es el sistema del hogar de adultos mayores. Sirve para llevar el expediente de cada residente, la bitácora diaria de atenciones, las alertas y tareas del personal, la farmacia, el personal y sus asignaciones, y para que la familia consulte cómo está su residente.

Este manual está ordenado por rol. Cada persona solo ve en el menú lo que su rol le permite. Si alguien intenta abrir una pantalla que no le toca, el sistema le muestra **"Acceso denegado"**.

---

## 1. Lo que es igual para todos

### 1.1 Iniciar sesión (UC-01)

1. Abra la dirección de VITALIA en el navegador.
2. Escriba su **correo** y su **contraseña**.
3. Si está en su propio equipo, puede marcar **"Mantener la sesión iniciada"**.
4. Presione **Entrar al Sistema**.

| Si pasa esto | Significa |
|---|---|
| "Correo o contraseña incorrectos." | El correo no existe o la contraseña está mal. Por seguridad, el sistema no le dice cuál de los dos falló. |
| "Este usuario está desactivado. Consulte con el administrador." | Su cuenta existe, pero el administrador la desactivó (por ejemplo, la persona ya no trabaja en el hogar). |

Al entrar, el personal llega al **Panel principal** y la familia llega a **Mi familiar**. La sesión dura 8 horas y se renueva mientras usted siga usando el sistema.

### 1.2 Mi perfil

En la parte de abajo del menú de la izquierda está su tarjeta, con sus iniciales, su nombre y su rol. Al tocarla se abre la **tarjeta de perfil**, que muestra:

- su nombre, correo, rol y cargo;
- cuántos residentes tiene a su cargo (personal) o cuántos familiares tiene vinculados (familia);
- el estado de la cuenta;
- los botones **Cambiar contraseña** y **Cerrar sesión**.

La tarjeta se cierra con la **X**, con la tecla **Esc** o tocando fuera de ella. En el celular se ajusta al ancho de la pantalla.

### 1.3 Cambiar la contraseña

1. Abra su perfil y elija **Cambiar contraseña**.
2. Escriba su contraseña actual, la nueva y repita la nueva.
3. Presione **Guardar contraseña nueva**.

La contraseña nueva debe tener **al menos 8 caracteres**, **al menos una letra y un número**, y debe ser **distinta de la actual**. Por seguridad, si hay un error, los campos se vacían y hay que escribirlos de nuevo.

### 1.4 Cerrar sesión

Abra su perfil y elija **Cerrar sesión**. Confirme en la ventana que aparece. Haga esto siempre que use una computadora compartida.

---

## 2. Administrador

El administrador (por ejemplo, la directora del hogar) ve **todo** el sistema. Además de lo que hacen el médico y la enfermería, administra el personal, las cuentas, los respaldos y los vínculos con la familia.

### 2.1 Menú del administrador

| Sección | Opción | Para qué sirve |
|---|---|---|
| Módulos | Inicio | Resumen general: residentes activos, personal, atenciones de hoy, alertas y medicamentos. |
| Módulos | Residentes | Expedientes de los residentes. |
| Módulos | Bitácora | Atenciones diarias registradas. |
| Módulos | Alertas y Tareas | Tablero de pendientes del personal. |
| Módulos | Farmacia y Stock | Medicamentos, lotes y existencias. |
| Módulos | Personal | Empleados y asignaciones de médico y cuidador. |
| Sistema | Usuarios y Roles | Cuentas para entrar al sistema y sus roles. |
| Sistema | Respaldos | Copias de seguridad de la base de datos. |
| Sistema | Datos de presentación | Solo aparece si todavía no se cargaron (ver 2.8). |

### 2.2 Residentes

**Ver y buscar.** En **Residentes** aparece la lista. Puede buscar por nombre, apellido o código y filtrar por estado (**Activos**, **Inactivos** o **Todos**) y por habitación. Al tocar un residente, sus datos se ven en el panel de la derecha. **Ver ficha** abre el expediente completo.

**Registrar un residente.**
1. Presione **Registrar residente**.
2. El sistema propone el código siguiente (por ejemplo, `RES-2026-117`). Puede cambiarlo, pero no puede repetirse.
3. Llene nombre, apellido, fecha de nacimiento, habitación, tipo de sangre, alergias, contacto de emergencia (nombre y teléfono) y observaciones.
4. Presione **Guardar Residente**.

Reglas que revisa el sistema:
- El residente debe tener **60 años o más** (y no más de 130).
- El teléfono debe tener 8 dígitos. Se guarda como `7000-1111`.
- El tipo de sangre debe ser uno de los 8 válidos (A+, A-, B+, B-, AB+, AB-, O+, O-) o quedar vacío.

**Editar.** En la ficha, presione **Editar datos**, cambie lo necesario y guarde.

**Dar de baja.** Presione **Dar de baja** y confirme. El residente **no se borra**: pasa a **Inactivos** y conserva todo su historial. Ya no aparece en las listas para registrar atenciones ni alertas, y su familia deja de verlo.

### 2.3 Vincular a un familiar con un residente

Primero, el familiar necesita una cuenta con rol **Familiar** (ver 2.6).

1. Abra la ficha del residente (o selecciónelo en la lista).
2. En la parte de **Familiares**, presione **Vincular**.
3. Elija el correo del familiar, el **parentesco** (Hijo, Hija, Nieto, Esposa, etc.) y su **teléfono**.
4. Presione **Vincular familiar**.

Desde ese momento, el familiar ve a ese residente cuando entra. Para quitarle el acceso, presione **Quitar** junto a su nombre en la ficha y confirme.

Solo se pueden vincular cuentas activas con rol Familiar, y no se puede vincular dos veces al mismo familiar con el mismo residente.

### 2.4 Personal

En **Personal** está la lista de empleados. El filtro muestra los activos y, si elige **Inactivos**, los que ya no trabajan en el hogar.

**Registrar un empleado.**
1. Primero cree su cuenta en **Usuarios y Roles** (ver 2.6).
2. En **Personal**, presione **Registrar Empleado**.
3. Elija la cuenta (solo aparecen cuentas activas sin empleado), y escriba nombre, apellido y cargo.
4. Presione **Guardar Empleado**.

**Editar** cambia los datos del empleado. **Eliminar** hace una baja lógica: el empleado queda inactivo, pero su historial se conserva.

### 2.5 Asignaciones (médico y cuidador)

Cada residente puede tener **un médico** y **un cuidador (o enfermero)** a la vez, pero no dos del mismo tipo.

**Asignar.**
1. En **Personal**, presione **Nueva** (asignación).
2. Elija el empleado y el residente. En la lista se ve a quién tiene ya asignado cada residente.
3. Presione **Crear Asignación**.

**Cambiar de cuidador.** Primero **finalice** la asignación actual (botón **Finalizar Asignación** en la lista de Personal y luego **Confirmar**) y después asigne al nuevo. La asignación vieja queda guardada con su fecha de fin.

El tipo sale del rol de la cuenta: si la cuenta del empleado tiene rol **Médico**, cuenta como médico. Cualquier otro rol cuenta como cuidador.

### 2.6 Usuarios y Roles

**Crear una cuenta.**
1. Presione **+ Nuevo Usuario**.
2. Escriba el correo, la contraseña inicial y elija el rol.
3. Presione **Guardar Usuario**. Entréguele la contraseña a la persona y pídale que la cambie al entrar (ver 1.3).

**Editar.** Puede cambiar el correo, el rol, **activar o desactivar** la cuenta y, si hace falta, poner una contraseña nueva.

**Eliminar.** Solo se puede eliminar una cuenta que no tenga registros. Si la cuenta es de un empleado o ya tiene movimientos, el sistema no deja borrarla; en ese caso, **desactívela**.

**Roles.** En la pestaña **Roles** se ven los 4 roles del sistema: Administrador, Médico, Enfermero y Familiar. **No cambie el nombre de estos 4 roles**, porque el sistema los usa para dar los permisos.

### 2.7 Respaldos

1. En **Respaldos**, presione **Nuevo Respaldo** y luego **Generar Respaldo**.
2. El sistema genera una copia de toda la base de datos (archivo `.bak`) y la muestra en la lista con su fecha y tamaño.
3. Con **Descargar** la baja a su computadora. Con **Eliminar** la borra del servidor.

Haga un respaldo antes de cualquier cambio grande y guarde una copia fuera del servidor.

### 2.8 Datos de presentación

Esta opción existe para que todo el grupo tenga **los mismos datos** en la presentación final.

1. En **Sistema**, presione **Datos de presentación**.
2. Lea qué se va a cargar y presione **Cargar datos de presentación**.
3. Al terminar, se muestran el resumen y la lista de cuentas creadas.

Qué conviene saber:
- Se carga **una sola vez** por base de datos. Después, la opción desaparece del menú.
- Es **todo o nada**: si algo falla, no se guarda nada.
- **No toca los datos que ya existen.** Si un código o un correo ya está ocupado, lo salta y avisa.
- Si faltan las tablas de Farmacia, aparece un mensaje para crearlas primero.

---

## 3. Médico

El médico trabaja con los residentes, la bitácora, las alertas y la farmacia. No ve Personal, Usuarios, Respaldos ni Datos de presentación.

### 3.1 Menú del médico

Inicio, Residentes, Bitácora, Alertas y Tareas, y Farmacia y Stock.

### 3.2 Residentes

Igual que el administrador (ver 2.2): buscar, ver la ficha, registrar, editar y dar de baja. En la ficha se ve quiénes lo atienden y quiénes son sus familiares, pero **solo el administrador** puede vincular o quitar familiares.

### 3.3 Bitácora de atenciones

La bitácora es el registro de todo lo que se le hace a cada residente: comidas, medicamentos, signos vitales, higiene, terapia, etc.

**Consultar.** Use los filtros de arriba: residente, tipo de atención, fecha de inicio y de fin, y búsqueda por texto. Presione **Filtrar**.

**Registrar una atención.**
1. En el panel de la derecha, elija el **residente**, el **tipo de atención** y el **empleado** que la hizo.
2. Escriba lo que se hizo en **Descripción / observación**.
3. Presione **Guardar atención**. La fecha y la hora se ponen solas.

**Editar.** Toque el lápiz de un registro, corrija el tipo o la observación y guarde.

**Eliminar.** Toque el ícono de borrar y confirme. Úselo solo para registros hechos por error.

**Tipos de atención.** Con el botón **Tipos** se pueden ver, crear y editar las categorías.

**Exportar a PDF.** El botón de PDF descarga la bitácora con los mismos filtros que tenga puestos.

### 3.4 Alertas y tareas

El tablero tiene 3 columnas: **Pendiente**, **En proceso** y **Atendida**. Cada tarjeta muestra el residente, la prioridad (**Urgente**, **Programada** o **Rutina**), la fecha y el responsable. Puede filtrar por residente y por prioridad.

**Crear una alerta.**
1. Presione **Nueva alerta**.
2. Elija el residente y, si quiere, el empleado responsable.
3. Escriba el título y la descripción, elija la prioridad y la fecha programada.
4. **Solo el médico** ve además la lista de **medicamentos** y el campo de **dosis**. Si elige uno, la receta se agrega al final de la descripción como `[PRESCRIPCIÓN MÉDICA: ...]`.
5. Presione **Guardar Alerta**. La alerta empieza en **Pendiente**.

**Mover una alerta.** En la tarjeta, presione **Iniciar → En Proceso** y, cuando esté resuelta, **Marcar Atendida**. Cada cambio queda en el historial de la alerta, con fecha y usuario. Una alerta **Atendida ya no se puede reabrir ni editar**.

**Editar o eliminar.** Se puede editar mientras no esté Atendida. Solo se pueden eliminar alertas **Pendientes que no tengan historial**.

### 3.5 Farmacia y stock

**Ver.** En **Farmacia y Stock** está el catálogo con código, nombre, presentación, existencias y estado. Puede buscar y filtrar por existencias (por ejemplo, **Agotado**).

**Registrar un medicamento.**
1. Presione **Registrar Medicamento**.
2. Llene código, nombre, presentación y descripción.
3. Llene el **lote inicial**: número de lote, cantidad y fecha de vencimiento.
4. Presione **Guardar Medicamento**.

**Editar.** Puede cambiar los datos del medicamento, su lote y marcarlo como **Inactivo** si ya no se usa.

Solo el administrador puede **eliminar** medicamentos, y solo si nunca se entregaron a un residente.

---

## 4. Enfermero / Cuidador

En VITALIA, los cuidadores y el personal de enfermería entran con el rol **Enfermero**. Su cargo (Enfermera jefe, Cuidador, etc.) aparece en su perfil.

### 4.1 Menú del enfermero o cuidador

Inicio, Residentes, Bitácora, Alertas y Tareas, y Farmacia y Stock (solo para consultar).

### 4.2 Su trabajo diario

| Qué necesita hacer | Dónde | Ver |
|---|---|---|
| Consultar los datos y alergias de un residente | Residentes → Ver ficha | 2.2 |
| Anotar una comida, medicamento, baño, toma de signos, etc. | Bitácora → panel derecho → Guardar atención | 3.3 |
| Ver las tareas pendientes del turno | Alertas y Tareas | 3.4 |
| Empezar una tarea y marcarla como hecha | Tarjeta de la alerta → Iniciar → En Proceso → Marcar Atendida | 3.4 |
| Avisar de algo (una caída, fiebre, etc.) | Alertas y Tareas → Nueva alerta (prioridad Urgente) | 3.4 |
| Ver si hay un medicamento en existencia | Farmacia y Stock | 3.5 |

### 4.3 Lo que no puede hacer

- No puede registrar, editar ni eliminar medicamentos (solo verlos).
- No puede agregar recetas a las alertas (eso es del médico).
- No puede vincular familiares ni entrar a Personal, Usuarios y Roles o Respaldos.

---

## 5. Familiar

El familiar solo **consulta** información. No puede cambiar nada. Solo ve a los residentes que el administrador le vinculó.

### 5.1 Qué puede hacer

| Caso de uso | Qué hace |
|---|---|
| UC-01 Iniciar sesión | Entra con su correo y contraseña (ver 1.1). |
| UC-10 Consultar el historial | Ve la ficha y la bitácora de su residente. |
| UC-11 Exportar el historial a PDF | Descarga la ficha completa en PDF. |
| UC-14 Consultar las alertas | Ve las alertas activas y atendidas de su residente. |

### 5.2 Mi familiar (pantalla de inicio)

Al entrar, ve una tarjeta por cada residente vinculado con:
- nombre, código, edad y habitación;
- su parentesco ("Usted es su hija", por ejemplo);
- el médico y el cuidador que lo atienden;
- cuántas alertas activas tiene;
- la fecha de la última atención.

Si todavía no tiene residentes vinculados, aparece un aviso para que hable con el administrador.

### 5.3 La ficha del residente (UC-10 y UC-14)

Toque la tarjeta para abrir la ficha. Ahí se ve:

- **Datos generales y de salud:** tipo de sangre, alergias, contacto de emergencia y observaciones.
- **Personal que lo atiende:** médico y cuidador, con la fecha desde la que lo atienden.
- **Alertas y tareas activas**, y las ya atendidas, con su prioridad y responsable.
- **Medicamentos que recibe:** las entregas, con fecha, cantidad y quién las entregó.
- **Bitácora de atenciones:** lo que se le ha hecho, quién lo hizo y cuándo. Si un registro fue corregido, aparece "Registro corregido:" con el motivo. Los registros anulados no se muestran.

### 5.4 Descargar el historial en PDF (UC-11)

En la ficha, presione **Descargar historial en PDF**. Se descarga un archivo con el nombre `Historial_<código>_<fecha>.pdf`, con toda la ficha y números de página.

### 5.5 Lo que no puede hacer

- Ver residentes que no son suyos. Si lo intenta, aparece **Acceso denegado**.
- Entrar a las pantallas del personal (Residentes, Bitácora, Alertas, Farmacia, etc.).
- Cambiar cualquier dato. Si nota algo raro, comuníquese con el hogar.

---

## 6. Preguntas frecuentes

**Olvidé mi contraseña.** En la pantalla de inicio, el enlace **¿Olvidó su contraseña?** explica qué hacer. Pídale al administrador que le ponga una nueva desde **Usuarios y Roles → Editar**. Después cámbiela usted desde su perfil.

**Un empleado ya no trabaja en el hogar.** El administrador debe:
1. Finalizar sus asignaciones en **Personal**.
2. Dar de baja al empleado en **Personal**.
3. Desactivar su cuenta en **Usuarios y Roles**.

**Un residente se fue del hogar.** Use **Dar de baja**. Su expediente queda guardado como inactivo.

**Me sale "Acceso denegado".** Esa pantalla no le corresponde a su rol. Si cree que sí debería verla, hable con el administrador.
