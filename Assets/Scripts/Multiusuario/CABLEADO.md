# Cableado del bloque C en Unity

Las acciones de VIROO llevan identificador, inyección de dependencias y validadores de
editor. **No se pueden crear editando el archivo de escena a mano**, así que esta parte hay
que hacerla desde el Inspector. Este documento es la lista completa.

Al terminar hay **tres transiciones** cableadas igual (reto 1 → reto 2, reto 2 → reto 3) y
**un reinicio**.

---

## 0. Dónde van las marcas de reunión

`RoomCenterStart` está en **(-2.08, 0, -7.26)**. Ese es el punto del mundo virtual donde cae
el centro de la sala física al arrancar la experiencia.

La sala mide 10,71 × 6,53 m, así que desde ese punto hay 5,35 m de holgura a cada lado en X
y 3,26 m en Z. Dos marcas a un metro del centro caben de sobra:

| Marca | Posición sugerida | Para |
|---|---|---|
| `Marca_A_Reto1` | (-3.08, 0, -7.26) | participante de identificador menor |
| `Marca_B_Reto1` | (-1.08, 0, -7.26) | el otro |

Quedan a **2 metros** la una de la otra. Esa es exactamente la separación con la que los dos
participantes llegarán al reto siguiente, porque el teletransporte de VIROO conserva las
posiciones relativas.

> **Regla para las demás reuniones:** las marcas de cada reunión van alrededor del **destino
> del salto anterior**, no alrededor de `RoomCenterStart`. Tras el primer salto, el centro de
> la sala ya no está en (-2.08, 0, -7.26), sino en el destino al que saltó el grupo.

Con el componente seleccionado, la vista de escena dibuja un círculo por marca y una línea
entre ellas: esa línea es la separación de llegada.

---

## 1. Transición de reto (repetir para cada una)

### 1.1 Crear el objeto

Objeto vacío, por ejemplo `Avance_Reto1`.

### 1.2 Añadirle los componentes de VIROO

| Componente | Menú | Configuración |
|---|---|---|
| `UnityEventAction` | Viroo / Actions | Es el aviso de «ya terminé». Se difunde a todos los clientes. |
| `TeleportAllAction` | Viroo / Actions | Dejar **Teleport Position vacío**: lo fija el código antes de ejecutarlo. |

### 1.3 Añadirle `ViroLab/Multiusuario/Avance en grupo`

| Campo | Valor |
|---|---|
| Anuncio de fin | el `UnityEventAction` de arriba |
| Salto de grupo | el `TeleportAllAction` de arriba |
| Destino | `Punto_Destino_Planta` (o el punto de llegada que corresponda) |
| Esperar a todos | marcado |
| Espera máxima | 180 |

### 1.4 Añadirle `ViroLab/Multiusuario/Reunión en el centro`

| Campo | Valor |
|---|---|
| Marcas | `Marca_A_Reto1`, `Marca_B_Reto1` (en ese orden) |
| Radio | 0.8 |
| Avance | el `AvanceEnGrupo` del paso anterior |
| Espera máxima | 60 |

### 1.5 Cerrar el circuito de eventos

1. En el **`UnityEventAction`** (paso 1.2), en su evento expuesto, añadir una entrada que
   llame a **`AvanceEnGrupo.RegistrarAnuncio`** de este mismo objeto.

2. En **`ReunionEnElCentro`**:
   - `AlConvocar` → encender el cartel de «ve a tu marca».
   - `AlAsignarMarca` → resaltar la marca recibida (recibe el `Transform`).
   - `AlLlegarALaMarca` → apagar el cartel.

3. En **`AvanceEnGrupo`**:
   - `AlQuedarEnEspera` → cartel de «esperando al resto del grupo».
   - `AntesDelSalto` → `FundidoDePantalla.OscurecerAhora`.
   - `AlCambiarLaCuenta` → marcador, si se quiere.

4. En el **`TeleportAllAction`**, en `OnActionExecuted`:
   - `FundidoDePantalla.AclararAhora`
   - lo que deba encenderse al llegar (el splash del reto, el vídeo…).

### 1.6 Apuntar la salida del reto al nuevo camino

Donde hoy se dispara la transición, llamar a **`ReunionEnElCentro.Convocar`** en lugar de lo
que hubiera.

Además, asignar el `AvanceEnGrupo` en el campo `avanceEnGrupo` de:

- **`FadeOutVR`** (objeto `Lavamanos`), para la salida del reto 1.
- **`GestorCuestionario`** (objeto `Canvas_Cuestionario`), para la salida del reto 2.

Mientras esos campos estén vacíos, los dos conservan el teletransporte individual antiguo y
avisan por consola cada vez que lo usan. **No hay día de corte:** la experiencia sigue
funcionando durante todo el proceso de cableado.

---

## 2. Reinicio entre tandas

### 2.1 Crear el objeto

Objeto vacío, `Reinicio_Experiencia`.

### 2.2 Añadirle los componentes de VIROO

| Componente | Configuración |
|---|---|
| `UnityEventAction` | El aviso de reinicio. Se difunde: los dos visores se reinician a la vez. |
| `TeleportAllAction` | Teleport Position vacío. |

### 2.3 Añadirle `ViroLab/Multiusuario/Reinicio de la experiencia`

| Campo | Valor |
|---|---|
| Aviso de reinicio | el `UnityEventAction` |
| Salto al inicio | el `TeleportAllAction` |
| Punto de inicio | `RoomCenterStart` |
| Duración fundido | 1.5 |
| Margen tras el salto | 0.5 |
| Margen antirrebote | 3 |

### 2.4 Cerrar el circuito

1. En el `UnityEventAction`, añadir una entrada que llame a
   **`ReinicioDeExperiencia.ReiniciarAqui`** de este mismo objeto.

2. `AlEmpezarElReinicio` → **apagar** lo que pertenece a los retos 2 y 3:
   - `Canvas_Cuestionario`
   - `Canvas_VideoEsc3`
   - el tablero del simulador 2D
   - las puertas que hayan quedado abiertas

3. `AlTerminarElReinicio` → **encender** el estado de partida:
   - el lienzo del reto 1
   - los casilleros cerrados y llenos
   - la puerta de entrada cerrada

> Lo que cada componente sabe reiniciar por su cuenta **no hay que ponerlo aquí**. Son doce
> componentes (cuestionario, carrusel, vídeo del reto 3, motor del simulador, guía por voz,
> efectos de sonido, cartel de EMPEZAR, los dos validadores de secuencia, el avance en grupo,
> la reunión y el reasignador de gatillo) y se reinician solos. Estos dos eventos son
> únicamente para lo que vive en la jerarquía de la escena y no en un script.

### 2.5 El botón del moderador

Poner un botón que llame a **`ReinicioDeExperiencia.PedirReinicio`**, y colocarlo **en el
cliente de escritorio del moderador, fuera del alcance de los estudiantes**. El margen
antirrebote evita el doble clic, pero no evita un clic ajeno.

---

## 3. Lo que hay que retirar, y cuándo

**Solo después de haber probado lo anterior con dos visores.** Hasta entonces, dejarlo.

| Qué | Dónde | Por qué |
|---|---|---|
| El teletransporte manual | `FadeOutVR`, bloque del camino antiguo | Mueve al `PlayerRoot` de uno solo |
| El teletransporte manual | `GestorCuestionario.FinalizarCuestionario`, bloque del camino antiguo | Igual |
| El componente entero | `DestinoEscena3Multiusuario` (objeto `Ajuste_DestinosEscena3`) | Repartir a cada participante a un punto distinto es lo contrario de lo que exige una sala compartida |

---

## 4. Comprobación en el ensayo

Con dos visores, en este orden:

1. **Reunión.** Al terminar el reto 1, ¿aparece el cartel y se resalta una marca distinta en
   cada visor?
2. **Salto.** Tras pisar las dos marcas, ¿saltan los dos a la vez y siguen a 2 m el uno del
   otro?
3. **Aviso de proximidad.** Ya en el reto 2, que uno camine hacia el otro de espaldas.
   ¿Suena el aviso y aparece el anillo rojo? Ésta es la prueba que decide si podemos pasar de
   dos estudiantes.
4. **Voz.** En el simulador, operar deprisa. ¿Se corta alguna frase a media palabra?
5. **Reinicio.** Pulsar el botón del moderador. ¿Vuelven los dos al inicio, con los casilleros
   llenos, el cuestionario sin empezar y el simulador apagado?
6. **Segunda pasada.** Repetir la experiencia entera sin cerrar la aplicación. Cronometrar:
   ese número, dividiendo los 480 minutos útiles, dice cuántas tandas caben el 29.
