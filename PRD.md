# PRD-001: Gestor de gastos — control mensual de gastos personales

## Contexto y Problema
Actualmente la gestión de gastos personales se realiza mediante una planilla en la que, mes a mes, se agregan, eliminan y modifican filas para registrar los distintos gastos y sus importes.
Esta operatoria resulta tediosa y propensa a errores, especialmente porque muchos gastos son recurrentes pero pueden cambiar de importe entre un mes y otro. Por ejemplo, ante el aumento permanente de una cuota, actualmente es necesario modificar manualmente el valor correspondiente y recordar trasladar ese cambio a los meses siguientes.
Además, la planilla se utiliza como mecanismo de seguimiento de pagos, marcando visualmente aquellos gastos que ya fueron abonados. Esto obliga a revisar periódicamente la información para detectar qué gastos continúan pendientes y puede provocar que algunos pagos se realicen después de su fecha de vencimiento.
La información tampoco permite obtener de forma inmediata una visión consolidada de cuánto se está gastando, cuáles son las categorías con mayor impacto o cuánto dinero permanece pendiente de pago.

Persona:
  - Andrés: administra sus gastos personales mes a mes. Necesita registrar sus obligaciones de forma rápida, identificar fácilmente qué cosas están pagas y cuáles no, conocer próximos vencimientos y consultar en qué categorías concentra sus gastos.

## Objetivos
Gestionar de manera práctica los gastos personales de forma mensual para realizar los pagos en tiempo sin olvidarse de nada. También poder visualizar de manera intuitiva un resumen de los gastos para saber dónde se está gastando más.

- Métrica de éxito: al cierre de cada mes, 0 gastos con fecha de vencimiento en ese mes que estén en estado Vencido o que se hayan marcado como pagados con una fecha de pago posterior a su fecha de vencimiento.

## Requerimientos Funcionales

### Alta de gastos
- RF-01: El sistema debe permitir crear un gasto indicando como mínimo nombre y monto. Opcionalmente se podrá indicar descripción, categoría y fecha de vencimiento.
- RF-02: El sistema debe asignar cada gasto creado al mes que el usuario está visualizando en pantalla al momento del alta.
- RF-03: El sistema debe aceptar únicamente montos mayores a 0, con hasta 2 decimales, expresados en pesos argentinos (ARS) e ingresados con coma como separador decimal y, opcionalmente, punto como separador de miles.
- RF-04: Al crear un gasto, el usuario debe poder indicar si se trata de un gasto recurrente o puntual.
- RF-05: El sistema debe permitir que la fecha de vencimiento de un gasto caiga en el mismo mes al que pertenece el gasto o en un mes posterior, y debe rechazar fechas de vencimiento de meses anteriores.

### Gastos recurrentes y puntuales
- RF-06: Un gasto recurrente debe estar disponible automáticamente en los meses siguientes mientras permanezca activo.
- RF-07: En cada mes proyectado, la fecha de vencimiento de un gasto recurrente debe caer dentro de ese mismo mes, en el mismo día del mes que su fecha de vencimiento vigente; si ese mes no tiene ese día, debe usarse el último día de ese mes.
- RF-08: Un gasto puntual debe pertenecer únicamente al mes para el cual fue registrado y no debe generarse automáticamente en períodos posteriores.

### Edición, baja y eliminación
- RF-09: El sistema debe permitir editar el nombre, descripción, monto, categoría y fecha de vencimiento de un gasto.
- RF-10: Al modificar un gasto recurrente, el usuario debe poder elegir entre modificar solamente el gasto del mes visualizado ("Solo este mes") o aplicar el cambio al mes visualizado y a los meses futuros ("Este mes y los siguientes").
- RF-11: Al aplicar un cambio con "Este mes y los siguientes", el sistema debe sobrescribir los campos editados también en los meses futuros que tengan modificaciones propias, sin alterar su estado de pago.
- RF-12: El sistema debe permitir cambiar un gasto puntual a recurrente; el gasto debe proyectarse a partir del mes visualizado.
- RF-13: El sistema debe permitir cambiar un gasto recurrente a puntual; el gasto debe quedar en el mes visualizado, conservar sus registros de los meses anteriores y no mostrarse en los meses posteriores, aunque en esos meses estuviera marcado como pagado.
- RF-14: El sistema debe permitir dar de baja un gasto recurrente. A partir de la baja, el gasto no debe mostrarse en el mes visualizado al darlo de baja ni en los meses posteriores, aunque en esos meses estuviera marcado como pagado.
- RF-15: El sistema debe conservar los registros de los meses anteriores al mes visualizado al dar de baja un gasto recurrente.
- RF-16: El sistema debe permitir eliminar un gasto puntual.
- RF-17: Las modificaciones realizadas sobre un gasto recurrente en un determinado mes no deben alterar el monto, la fecha de vencimiento, la categoría ni el estado de pago registrados en meses anteriores.

### Categorías
- RF-18: El sistema debe permitir crear categorías de gastos.
- RF-19: El sistema debe rechazar la creación o el renombrado de una categoría cuando ya exista otra con el mismo nombre o cuando el nombre sea el reservado "Sin categoría", sin distinguir mayúsculas, minúsculas ni acentos.
- RF-20: El sistema debe permitir editar el nombre de una categoría. El nuevo nombre debe mostrarse en todos los gastos asociados, en todos los meses, incluidos los anteriores.
- RF-21: El sistema debe permitir eliminar una categoría. Los gastos asociados a ella deben pasar a "Sin categoría" en todos los meses, incluidos los anteriores.
- RF-22: El usuario debe poder asociar opcionalmente un gasto a una categoría.

### Resumen
- RF-23: El sistema debe mostrar, para un mes determinado, el total de los gastos agrupados por categoría, sumando gastos pagados y pendientes.
- RF-24: En el resumen por categoría, el sistema debe agrupar los gastos sin categoría en un grupo "Sin categoría".
- RF-25: El sistema debe mostrar el total de gastos del mes visualizado.
- RF-26: El sistema debe mostrar el total pendiente de pago del mes visualizado.

### Pagos y vencimientos
- RF-27: El sistema debe permitir marcar un gasto como pagado.
- RF-28: Al marcar como pagado un gasto que no está en estado Vencido, el sistema debe registrar automáticamente la fecha actual como fecha de pago.
- RF-29: Al marcar como pagado un gasto en estado Vencido, el sistema debe preguntar si la fecha de pago es la fecha actual y, si no lo es, permitir ingresar la fecha real de pago.
- RF-30: El sistema debe rechazar una fecha de pago posterior a la fecha actual.
- RF-31: El sistema debe permitir desmarcar un gasto pagado, que vuelve a quedar pendiente y sin fecha de pago.
- RF-32: Cada vez que se abre o recarga la aplicación, haya o no que iniciar sesión, el sistema debe mostrar una alerta interna cuando exista al menos un gasto pendiente de pago cuya fecha de vencimiento coincida con la fecha actual.
- RF-33: Mientras un gasto permanezca pendiente, el sistema debe distinguir visualmente su situación respecto de la fecha de vencimiento:
    - Al día: faltan más de 3 días para su vencimiento o no posee fecha de vencimiento.
    - Próximo a vencer: faltan entre 1 y 3 días.
    - Vence hoy: la fecha de vencimiento coincide con la fecha actual.
    - Vencido: la fecha de vencimiento ya pasó.

### Navegación
- RF-34: El sistema debe mostrar inicialmente los gastos correspondientes al mes actual.
- RF-35: El usuario debe poder navegar entre distintos meses y años para consultar información histórica o períodos futuros.

### Acceso
- RF-36: El sistema debe requerir autenticación mediante email y contraseña antes de permitir el acceso a la información de gastos y categorías.
- RF-37: El único usuario del sistema debe crearse mediante un seed; el sistema no debe ofrecer ninguna funcionalidad de registro de usuarios, ni en la interfaz ni en la API.

### Exportación
- RF-38: El sistema debe permitir exportar a un archivo CSV el historial de gastos desde el primer mes con al menos un gasto registrado hasta el mes actual inclusive, incluyendo los meses en que un gasto recurrente solo está proyectado.
- RF-39: El archivo CSV exportado debe cumplir el siguiente formato:
    - Codificación UTF-8 con BOM; separador coma; campos con comas o comillas entre comillas dobles (RFC 4180).
    - Primera fila de encabezado: `periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago`.
    - `periodo` en formato `aaaa-mm`; `vencimiento` y `fecha_pago` en formato `aaaa-mm-dd`, vacíos si no existen.
    - `monto` con punto decimal, 2 decimales y sin separador de miles (ej.: `30000.00`).
    - `categoria` con el valor `Sin categoría` cuando el gasto no tiene categoría.
    - `estado` con los valores `Pagado` o `Pendiente`.

## Requerimientos No Funcionales
- RNF-01: Las siguientes operaciones deben responder en menos de 2 segundos en el percentil 95 (p95), medido en el servidor, considerando un único usuario concurrente, hasta 100 gastos por mes y hasta 10 años de información histórica: iniciar sesión, ver la alerta de vencimientos al abrir la aplicación, consultar un mes, navegar entre meses y años, crear, editar, dar de baja o eliminar un gasto, cambiar su tipo, marcar o desmarcar un pago, crear, editar o eliminar una categoría, consultar el resumen mensual y exportar el CSV.
- RNF-02: La interfaz debe ser responsive: en pantallas de al menos 360 px de ancho, todas las operaciones listadas en RNF-01 deben poder utilizarse sin scroll horizontal ni superposición de elementos.
- RNF-03: La aplicación debe poder instalarse y ejecutarse como una Progressive Web App (PWA) en las últimas 2 versiones estables de Chrome y Edge de escritorio y de Chrome para Android. En las últimas 2 versiones estables de Firefox de escritorio y Safari de macOS debe poder utilizarse desde el navegador con todas las operaciones de RNF-01, sin requisito de instalación. El acceso y modificación de los datos requerirá conexión a Internet; el funcionamiento offline queda fuera del alcance inicial.
- RNF-04: Las contraseñas no deben almacenarse en texto plano: deben persistirse utilizando un algoritmo de hash con salt, con un costo equivalente a PBKDF2 con al menos 100.000 iteraciones (mecanismo por defecto de ASP.NET Core Identity) o superior.
- RNF-05: Los gastos recurrentes no deben pre-generar registros: para todo mes sin información específica de un gasto recurrente, la cantidad de registros mensuales persistidos de ese gasto debe ser 0; el gasto se proyecta al consultar el período.
- RNF-06: Cuando un gasto recurrente tenga información específica de un mes (una modificación o un pago), debe persistirse exactamente 1 instancia propia de ese gasto para ese mes.
- RNF-07: Toda referencia a la fecha actual (alerta de vencimiento, estados de vencimiento, fecha de pago y mes inicial) debe calcularse en la zona horaria America/Argentina/Buenos_Aires (UTC−3).
- RNF-08: La sesión autenticada debe expirar tras 30 días sin actividad; a partir de ese momento el sistema debe volver a solicitar email y contraseña.

## Criterios de Aceptación

### Alta de gastos
- AC-01 (RF-01, RF-02, RF-04): Dado que el usuario visualiza octubre de 2026, cuando crea un gasto con nombre "Alquiler", monto $300.000 y tipo recurrente, entonces el gasto debe aparecer en octubre de 2026 identificado como recurrente.
- AC-02 (RF-01, RF-04): Dado que el usuario visualiza octubre de 2026, cuando crea un gasto puntual con nombre "Regalo", monto $15.000, descripción "Cumpleaños", categoría "Varios" y vencimiento 20/10/2026, entonces el gasto debe aparecer en octubre de 2026 como puntual con exactamente esos valores.
- AC-03 (RF-01): Dado el formulario de alta, cuando el usuario intenta confirmar un gasto sin nombre o sin monto, entonces el sistema debe rechazar el alta y no debe crearse ningún gasto.
- AC-04 (RF-02): Dado que la fecha actual es 05/10/2026 y el usuario visualiza diciembre de 2026, cuando crea un gasto puntual, entonces el gasto debe aparecer en diciembre de 2026 y no en octubre de 2026.
- AC-05 (RF-03): Dado el formulario de alta, cuando el usuario ingresa como monto 0, -100 o 10,999, entonces el sistema debe rechazar el alta y no debe crearse ningún gasto.
- AC-06 (RF-03): Dado el formulario de alta, cuando el usuario ingresa como monto 1.234,56, entonces el gasto debe registrarse con un monto de $1.234,56.
- AC-07 (RF-05): Dado que el usuario visualiza octubre de 2026, cuando crea un gasto con vencimiento 05/11/2026, entonces el gasto debe aparecer en octubre de 2026 con vencimiento 05/11/2026.
- AC-08 (RF-05): Dado que el usuario visualiza octubre de 2026, cuando intenta crear un gasto con vencimiento 30/09/2026, entonces el sistema debe rechazar el alta y no debe crearse ningún gasto.

### Gastos recurrentes y puntuales
- AC-09 (RF-06): Dado un gasto recurrente activo, cuando el usuario consulta el mes siguiente, entonces el gasto debe visualizarse en ese período aunque previamente no se haya creado manualmente un gasto para ese mes.
- AC-10 (RF-07): Dado un gasto recurrente creado en enero de 2027 con vencimiento 30/01/2027, cuando el usuario consulta febrero y marzo de 2027, entonces el vencimiento debe ser 28/02/2027 en febrero y 30/03/2027 en marzo.
- AC-11 (RF-05, RF-07): Dado un gasto recurrente creado en octubre de 2026 con vencimiento 05/11/2026, cuando el usuario consulta noviembre y diciembre de 2026, entonces el vencimiento debe ser 05/11/2026 en noviembre y 05/12/2026 en diciembre.
- AC-12 (RF-07, RF-10): Dado un gasto recurrente que vence el día 10 de cada mes, cuando, visualizando septiembre de 2026, el usuario cambia su vencimiento a 15/09/2026 seleccionando "Este mes y los siguientes", entonces octubre de 2026 debe mostrar vencimiento 15/10/2026 y agosto de 2026 debe continuar mostrando 10/08/2026.
- AC-13 (RF-08): Dado un gasto puntual creado para un mes de un determinado año, cuando el usuario consulta el mes siguiente del mismo año, entonces dicho gasto no debe aparecer en ese período.

### Edición, baja y eliminación
- AC-14 (RF-09): Dado un gasto puntual existente, cuando el usuario edita su nombre, descripción, monto, categoría y fecha de vencimiento, entonces el gasto debe reflejar los nuevos valores.
- AC-15 (RF-10): Dado un gasto recurrente de $30.000, cuando, visualizando septiembre, el usuario cambia su monto a $35.000 seleccionando "Solo este mes", entonces septiembre debe mostrar $35.000 y octubre debe continuar mostrando $30.000.
- AC-16 (RF-10, RF-17): Dado un gasto recurrente cuyo monto hasta agosto es de $25.000, cuando, visualizando septiembre, el usuario lo modifica a $30.000 seleccionando "Este mes y los siguientes", entonces agosto debe continuar mostrando $25.000 y septiembre y los períodos posteriores deben mostrar $30.000.
- AC-17 (RF-11): Dado un gasto recurrente de $30.000 cuyo monto de noviembre fue modificado a $32.000 con "Solo este mes" y que está marcado como pagado en noviembre, cuando, visualizando septiembre, el usuario cambia el monto a $35.000 seleccionando "Este mes y los siguientes", entonces noviembre debe mostrar $35.000 y seguir marcado como pagado.
- AC-18 (RF-12): Dado un gasto puntual de octubre, cuando el usuario, visualizando octubre, lo cambia a recurrente, entonces el gasto debe aparecer en octubre y también en noviembre.
- AC-19 (RF-13): Dado un gasto recurrente activo desde agosto de 2026, cuando, visualizando octubre de 2026, el usuario lo cambia a puntual, entonces el gasto debe seguir apareciendo en agosto y septiembre, debe aparecer en octubre y no debe aparecer en noviembre de 2026.
- AC-20 (RF-13): Dado un gasto recurrente marcado como pagado en noviembre de 2026, cuando, visualizando octubre de 2026, el usuario lo cambia a puntual, entonces el gasto no debe aparecer en noviembre de 2026 y el total de noviembre no debe incluir su monto.
- AC-21 (RF-14, RF-15): Dado un gasto recurrente con registros entre enero y agosto de 2026, cuando, visualizando septiembre de 2026, el usuario lo da de baja, entonces el gasto debe seguir apareciendo entre enero y agosto de 2026 y no debe aparecer en septiembre de 2026 ni en los meses posteriores.
- AC-22 (RF-14): Dado un gasto recurrente marcado como pagado en septiembre de 2026, cuando, visualizando septiembre de 2026, el usuario lo da de baja, entonces el gasto no debe aparecer en septiembre de 2026 y el total del mes de septiembre no debe incluir su monto.
- AC-23 (RF-16): Dado un gasto puntual en octubre, cuando el usuario lo elimina, entonces el gasto no debe aparecer en octubre.
- AC-24 (RF-17, RF-27): Dado un gasto recurrente de $20.000 que vence el 15/09/2026, cuando, visualizando septiembre, el usuario lo marca como pagado el 10/09/2026, entonces septiembre debe mostrarse como pagado con fecha de pago 10/09/2026 y octubre debe mostrarse pendiente por $20.000.

### Categorías
- AC-25 (RF-18): Dado que el usuario ingresa el nombre de una categoría nueva, cuando confirma su creación, entonces la categoría debe quedar disponible para asociarla a gastos.
- AC-26 (RF-19): Dado que existe la categoría "Educación", cuando el usuario intenta crear una categoría llamada "Educación", "educacion" o "EDUCACIÓN", o renombrar otra categoría con alguno de esos nombres, entonces el sistema debe rechazar la operación y debe seguir existiendo una sola categoría "Educación".
- AC-27 (RF-19): Dado el formulario de categorías, cuando el usuario intenta crear una categoría llamada "Sin categoría" o "sin categoria", o renombrar una categoría existente con alguno de esos nombres, entonces el sistema debe rechazar la operación y no debe existir ninguna categoría con ese nombre.
- AC-28 (RF-20): Dada la categoría "Servicios" asociada a gastos de agosto y de octubre de 2026, cuando el usuario la renombra a "Servicios del hogar", entonces esos gastos deben mostrarse con la categoría "Servicios del hogar" tanto en agosto como en octubre de 2026.
- AC-29 (RF-21): Dada una categoría sin gastos asociados, cuando el usuario la elimina, entonces la categoría no debe aparecer entre las categorías disponibles.
- AC-30 (RF-21): Dada la categoría "Ocio" asociada a gastos de agosto y de octubre de 2026, cuando el usuario la elimina, entonces esos gastos deben mostrarse como "Sin categoría" tanto en agosto como en octubre de 2026.
- AC-31 (RF-22): Dado un gasto sin categoría asignada, cuando el usuario le asocia una categoría existente, entonces el gasto debe mostrarse con esa categoría.

### Resumen
- AC-32 (RF-23): Dados en septiembre gastos de "Vivienda" por $200.000 pagados y $100.000 pendientes, y gastos de "Servicios" por $100.000, cuando el usuario consulta el resumen de ese mes, entonces debe visualizar $300.000 en Vivienda y $100.000 en Servicios.
- AC-33 (RF-24): Dado un gasto de $50.000 sin categoría en septiembre, cuando el usuario consulta el resumen de ese mes, entonces debe visualizar $50.000 en el grupo "Sin categoría".
- AC-34 (RF-25, RF-26): Dados en octubre gastos pagados por $300.000 y pendientes por $100.000, cuando el usuario consulta octubre, entonces debe visualizar un total del mes de $400.000 y un total pendiente de $100.000.

### Pagos y vencimientos
- AC-35 (RF-27, RF-28): Dado que la fecha actual es 05/10/2026 y un gasto pendiente vence el 10/10/2026, cuando el usuario lo marca como pagado, entonces el sistema no debe solicitar una fecha de pago y el gasto debe mostrarse como pagado sin recargar la página y con fecha de pago 05/10/2026.
- AC-36 (RF-29): Dado que la fecha actual es 05/10/2026 y un gasto pendiente venció el 01/10/2026, cuando el usuario lo marca como pagado e indica que la fecha de pago no es la actual e ingresa 03/10/2026, entonces el gasto debe mostrarse como pagado con fecha de pago 03/10/2026.
- AC-37 (RF-29): Dado que la fecha actual es 05/10/2026 y un gasto pendiente venció el 01/10/2026, cuando el usuario lo marca como pagado y confirma que la fecha de pago es la actual, entonces el gasto debe mostrarse como pagado con fecha de pago 05/10/2026.
- AC-38 (RF-30): Dado que la fecha actual es 05/10/2026 y un gasto pendiente venció el 01/10/2026, cuando el usuario lo marca como pagado e ingresa como fecha de pago 06/10/2026, entonces el sistema debe rechazar la fecha y el gasto debe seguir pendiente.
- AC-39 (RF-31): Dado un gasto pagado con fecha de pago 05/10/2026, cuando el usuario lo desmarca, entonces el gasto debe mostrarse como pendiente y sin fecha de pago.
- AC-40 (RF-32): Dado un gasto pendiente cuya fecha de vencimiento coincide con la fecha actual, cuando el usuario inicia sesión, entonces debe visualizar una alerta indicando que posee al menos un gasto que vence ese día.
- AC-41 (RF-32): Dada una sesión autenticada vigente y un gasto pendiente cuya fecha de vencimiento coincide con la fecha actual, cuando el usuario abre la aplicación sin tener que iniciar sesión, entonces debe visualizar una alerta indicando que posee al menos un gasto que vence ese día.
- AC-42 (RF-32): Dado que el único gasto cuya fecha de vencimiento coincide con la fecha actual ya está marcado como pagado, cuando el usuario abre la aplicación, entonces no debe mostrarse ninguna alerta de vencimiento.
- AC-43 (RF-33): Dado que la fecha actual es 05/10/2026 y un gasto pendiente vence el 09/10/2026, cuando se visualiza el mes, entonces debe identificarse como "Al día".
- AC-44 (RF-33): Dado un gasto pendiente sin fecha de vencimiento, cuando se visualiza el mes, entonces debe identificarse como "Al día".
- AC-45 (RF-33): Dado que la fecha actual es 05/10/2026 y dos gastos pendientes vencen el 08/10/2026 y el 06/10/2026, cuando se visualiza el mes, entonces ambos deben identificarse como "Próximo a vencer".
- AC-46 (RF-33): Dado que la fecha actual es 05/10/2026 y un gasto pendiente vence el 05/10/2026, cuando se visualiza el mes, entonces debe identificarse como "Vence hoy".
- AC-47 (RF-33): Dado que la fecha actual es 05/10/2026 y un gasto pendiente venció el 04/10/2026, cuando se visualiza el mes, entonces debe identificarse como "Vencido".
- AC-48 (RF-33, RNF-07): Dado que son las 22:00 del 05/10/2026 en Buenos Aires (01:00 del 06/10/2026 UTC) y un gasto pendiente vence el 05/10/2026, cuando se visualiza el mes, entonces debe identificarse como "Vence hoy".

### Navegación
- AC-49 (RF-34): Dado que el usuario ingresa a la aplicación sin haber navegado previamente a otro período, cuando se carga la vista principal, entonces el sistema debe mostrar los gastos correspondientes al mes y año actuales.
- AC-50 (RF-35): Dado que el usuario está visualizando septiembre de 2026, cuando navega al mes anterior, entonces debe visualizar los gastos correspondientes a agosto de 2026.
- AC-51 (RF-35): Dado que el usuario está visualizando enero de 2027, cuando navega al mes anterior, entonces debe visualizar los gastos correspondientes a diciembre de 2026.

### Acceso
- AC-52 (RF-36): Dado el usuario creado por el seed, cuando inicia sesión con su email y su contraseña correctos, entonces debe acceder a la vista de gastos del mes actual.
- AC-53 (RF-36): Dado el usuario creado por el seed, cuando intenta iniciar sesión con su email y una contraseña incorrecta, entonces el sistema debe rechazar el acceso y no debe mostrar ningún dato de gastos ni categorías.
- AC-54 (RF-36): Dado un usuario que no se encuentra autenticado, cuando intenta acceder a la información de gastos o categorías, entonces el sistema no debe mostrar los datos y debe solicitar autenticación.
- AC-55 (RF-36): Dado que no existe una sesión autenticada válida, cuando se intenta consultar o modificar datos de gastos o categorías llamando directamente a la API, entonces el sistema debe responder HTTP 401 y el cuerpo de la respuesta no debe contener ningún dato de la aplicación (gastos, categorías ni usuario).
- AC-56 (RNF-08): Dada una sesión autenticada sin actividad durante 30 días, cuando el usuario intenta acceder a la información de gastos, entonces el sistema no debe mostrar los datos y debe solicitar email y contraseña.
- AC-57 (RF-37): Dado que existe el usuario creado por el seed, cuando alguien intenta crear otra cuenta desde la interfaz o llamando a la API, entonces no debe existir ninguna opción de registro en la interfaz, la API debe rechazar la solicitud y la cantidad de usuarios registrados debe seguir siendo 1.

### Exportación
- AC-58 (RF-38): Dado que la fecha actual es 05/10/2026, existe un gasto puntual en agosto de 2026 y un gasto recurrente activo desde agosto de 2026 sin instancias propias, cuando el usuario exporta el historial, entonces el CSV debe contener 4 filas de datos: el gasto puntual en `2026-08` y el recurrente en `2026-08`, `2026-09` y `2026-10`, y ninguna fila de `2026-11` o posterior.
- AC-59 (RF-39): Dado un gasto pendiente "Luz, gas" de $1.234,56 sin categoría con vencimiento 10/10/2026, cuando el usuario exporta el historial, entonces el CSV debe estar en UTF-8 con BOM, su primera fila debe ser `periodo,nombre,monto,categoria,vencimiento,estado,fecha_pago` y debe contener la fila `2026-10,"Luz, gas",1234.56,Sin categoría,2026-10-10,Pendiente,`.

### Restricciones técnicas
- AC-60 (RNF-05): Dado un gasto recurrente creado en octubre de 2026 sin modificaciones ni pagos posteriores, cuando el usuario consulta diciembre de 2026, entonces el gasto debe visualizarse en diciembre de 2026 y la base de datos no debe contener ningún registro mensual de ese gasto para noviembre ni para diciembre de 2026.
- AC-61 (RNF-06): Dado un gasto recurrente sin registro mensual propio en septiembre de 2026, cuando, visualizando septiembre de 2026, el usuario lo marca como pagado y luego modifica su monto con "Solo este mes", entonces la base de datos debe contener exactamente 1 registro mensual de ese gasto para septiembre de 2026.

## Fuera de Alcance
- Gestión de múltiples usuarios.
- Registro de usuarios desde la aplicación (el único usuario se crea por seed).
- Cierre de sesión.
- Recuperación de contraseña.
- Roles y permisos.
- Multi-tenant.
- Multimoneda: todos los montos son en ARS.
- Cuentas bancarias y sincronización bancaria.
- Gestión de ingresos.
- Filtrado del historial exportado por rango de fechas u otros criterios.
- Instalación como PWA en Firefox y Safari (se usan desde el navegador).
- Funcionamiento y edición de información sin conexión.
- Sincronización offline.
- Notificaciones push.
- Notificaciones por correo electrónico o WhatsApp.

## Riesgos y Dependencias
- Riesgo: ambigüedad al editar un gasto recurrente. El usuario podría querer modificar únicamente el mes visualizado o cambiar definitivamente el gasto → mitigación: la interfaz debe solicitar explícitamente el alcance de la modificación: "Solo este mes" o "Este mes y los siguientes".
- Riesgo: pérdida de historial al eliminar un gasto habitual → mitigación: utilizar una baja lógica para los gastos recurrentes, manteniendo sus registros de los meses anteriores a la baja (RF-15). El registro del mes visualizado al darlo de baja no se conserva, aunque esté pagado (RF-14).
- Riesgo: el servidor o el navegador usan una zona horaria distinta a la de Buenos Aires y los estados de vencimiento, la alerta o la fecha de pago cambian de día → mitigación: calcular la fecha actual explícitamente en America/Argentina/Buenos_Aires (RNF-07) y cubrirlo con AC-48.
- Riesgo: la instalación como PWA no está soportada de forma estándar en Firefox de escritorio ni en Safari → mitigación: exigir la instalación solo en Chrome, Edge y Chrome para Android, y garantizar el uso completo desde el navegador en Firefox y Safari (RNF-03).
- Riesgo (aceptado): al no existir cierre de sesión, si la aplicación se abre en un dispositivo ajeno o compartido, ese dispositivo conserva el acceso hasta que la sesión expire → mitigación: usar la aplicación solo en dispositivos propios; la sesión expira tras 30 días sin actividad (RNF-08).
- Dependencia: PostgreSQL para persistencia de los datos.
- Dependencia: backend desarrollado con ASP.NET Core, Entity Framework Core y .NET.
- Dependencia: frontend desarrollado con React y TypeScript.
