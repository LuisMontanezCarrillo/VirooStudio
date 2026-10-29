# Bloque B1 — El indicador de «qué hago ahora»

Guía de cableado del componente `GuionDePasos`, para el reto 1 en la zona de
alistamiento.

**Reparto:** el componente ya está escrito y compila. Lo que queda es Inspector,
y es suyo. Son seis pasos y unos quince minutos.

---

## Por qué existe esto

Fue la observación más repetida en las pruebas del 29 de septiembre: los
participantes se pierden y no saben qué sigue. La experiencia habla una sola
vez, por voz, al entrar en cada zona. Quien se distrae en esa frase no tiene a
dónde volver a mirar.

Una locución es un mensaje que se emite. Una lista es un mensaje que se queda.

---

## Paso 0 — Antes de tocar nada, dos comprobaciones

### 0.1 Las gafas RV — comprobado, se ven

El objeto **`Logo Simulador VR`** apunta a un prefab que **no existe en el
proyecto** (`f599cdf4f1433fa4a8c2135edd24a7b3`). Lo encontré al mapear la
escena y temí que el paso 2 pidiera acercarse a algo invisible.

**No es el caso.** Las gafas se ven: la geometría la pone el prefab padre
`Logo proyecto 3D`, que es un FBX que sí está en el proyecto
(`Assets/3D Laboratory Environment with Appratus/Tripo-Meshy/Meshy Logo 3D/`).
El prefab que falta solo aportaba el objeto hijo que lleva el collider y el
interactable, y esos dos sobrevivieron serializados en la escena.

Queda como deuda menor, no como bloqueo: la escena arrastra una referencia rota
que Unity avisa en cada apertura.

### 0.2 ¿Se leen las dos marcas?

El panel usa `✓` para lo hecho y `▶` para lo que toca. No todas las fuentes de
TextMeshPro traen esos dos signos en su atlas; cuando falta uno, se dibuja un
cuadrito vacío.

Lo comprobará en el paso 5. Si sale el cuadrito, se cambian los dos campos
`Marca Hecho` y `Marca Actual` del Inspector por `OK` y `>`. No hay que tocar
código.

---

## Paso 1 — Crear el objeto

En la Hierarchy, clic derecho sobre **`Root`** → `Create Empty`.

Nómbrelo **`Guion_Reto1`**.

Déjelo en la posición que salga: el componente no usa la posición del objeto
para nada, porque el panel se coloca solo respecto a la cámara.

---

## Paso 2 — Añadir el componente

Con `Guion_Reto1` seleccionado: `Add Component` → busque **`Guion de pasos`**
(aparece bajo `ViroLab/Guía/`).

---

## Paso 3 — Llenar la lista de pasos

En el campo **`Titulo`** escriba:

```
ZONA DE ALISTAMIENTO
```

En **`Pasos`**, ponga `Size` en **5** y rellene así. **El orden importa**: el
número de cada paso es el que se cablea después, y contando desde **0**.

| # | Texto | Sangrado |
|---|---|---|
| 0 | `Acércate a la pantalla y reproduce el video` | ☐ |
| 1 | `Acércate a las gafas RV y haz clic` | ☐ |
| 2 | `Guarda tus objetos personales` | ☑ |
| 3 | `Equipa tu dotación de bioseguridad` | ☑ |
| 4 | `Realiza la higienización de manos y botas` | ☑ |

**Dos cosas que decidí y conviene que revise:**

Acorté su paso 2, de «da clic» a «haz clic», por coherencia con el resto, que va
en segunda persona. Si prefiere su redacción, cámbiela: el texto es suyo.

Su paso 3 era uno solo con tres apartados («primero…, segundo…, tercero…»). Lo
partí en tres entradas sangradas en lugar de poner una cabecera «Sigue las
instrucciones de las gafas RV» más tres hijos. La razón es mecánica: el panel
resalta el primer paso sin cumplir, y una cabecera que nunca se marca se
quedaría resaltada para siempre, señalando una línea que no se puede hacer. La
sangría transmite que los tres van juntos sin romper eso.

---

## Paso 4 — Cablear los cinco disparadores

Esta es la parte que hay que hacer con cuidado. En los cinco casos el gesto es
el mismo:

1. Seleccione el objeto.
2. Busque la lista de eventos que se indica.
3. Pulse **`+`** para añadir una entrada **al final**.
4. Arrastre **`Guion_Reto1`** al hueco del objeto.
5. En el desplegable elija **`GuionDePasos`** → **`Cumplir (int)`**.
6. Escriba el número en la casilla que aparece.

**El orden dentro de cada lista no importa** —los UnityEvent se ejecutan todos—,
pero añada siempre al final para no desordenar lo que ya hay.

### 4.1 · Paso 0 — El video

| | |
|---|---|
| **Objeto** | el botón de Play, dentro del prefab **`Youtube Logo`** |
| **Componente** | `XR Simple Interactable` |
| **Lista** | `Interactable Events` → **`Select Entered`** |
| **Cómo saber que es el correcto** | esa lista ya tiene una entrada: `PantallaLed.Play()` |
| **Añadir** | `Guion_Reto1` → `GuionDePasos.Cumplir` → **`0`** |

> **Decisión pendiente.** Así el paso se marca al *pulsar* Play, no al terminar
> el video. Es literal respecto a su instrucción («reproduce el video»), pero
> tiene un efecto: en cuanto pulsan, el panel ya señala el paso siguiente, y
> alguien podría irse con el video a medias. Marcarlo al terminar el video no se
> puede cablear desde el Inspector, porque `VideoPlayer` no expone ese evento;
> haría falta un componente pequeño que se suscriba a `loopPointReached`. Son
> veinte líneas. Dígame si lo prefiere.

### 4.2 · Paso 1 — Las gafas RV

| | |
|---|---|
| **Objeto** | **`Logo Simulador VR`** |
| **Componente** | `XR Simple Interactable` |
| **Lista** | `Interactable Events` → **`Select Entered`** |
| **Cómo saber que es el correcto** | esa lista ya tiene dos entradas: `VozOff2.Play()` y `Canvas.SetActive()` |
| **Añadir** | `Guion_Reto1` → `GuionDePasos.Cumplir` → **`1`** |

> **Cuidado: hay dos objetos con este nombre, y un tercero parecido.**
>
> | Objeto | Qué hace | ¿Es este? |
> |---|---|---|
> | `Logo proyecto 3D` → hijo `Logo Simulador VR` | `VozOff2.Play()` + `Canvas.SetActive()`, interactable **activo** | **sí** |
> | `Logo proyecto 3D (Esc2)` → hijo `Logo Simulador VR` | lo mismo, pero interactable **desmarcado** (`m_Enabled: 0`) | no |
> | `Logo proyecto 3D (Esc2)` | `Canvas_Cuestionario.SetActive()` | no, ese es el cuestionario |
>
> El bueno es el que cuelga de `Logo proyecto 3D` **sin** el `(Esc2)`, y es el
> único de los dos cuyo `XR Simple Interactable` está marcado.

### 4.3 · Paso 2 — El casillero

| | |
|---|---|
| **Objeto** | `Root/Planta_Piloto/Lockers/**locker room (3)**` |
| **Componente** | `XR Simple Interactable` |
| **Lista** | `Interactable Events` → **`Select Entered`** |
| **Cómo saber que es el correcto** | esa lista ya tiene cuatro entradas, la primera `Maniqui.DarPermiso()` |
| **Añadir** | `Guion_Reto1` → `GuionDePasos.Cumplir` → **`2`** |

> Hay ocho objetos `locker room` en la escena. Solo el **(3)** es interactivo.

### 4.4 · Paso 3 — La dotación

| | |
|---|---|
| **Objeto** | `Root/Planta_Piloto/Vitrina/**Maniqui**` |
| **Componente** | **`Validador Secuencia`** ← *no* el `XR Simple Interactable` |
| **Lista** | **`Al Tener Exito`** |
| **Cómo saber que es el correcto** | esa lista ya tiene cuatro entradas, la primera `Lavamanos.DarPermiso()` |
| **Añadir** | `Guion_Reto1` → `GuionDePasos.Cumplir` → **`3`** |

### 4.5 · Paso 4 — La higienización

| | |
|---|---|
| **Objeto** | `Root/Planta_Piloto/**Lavamanos**` |
| **Componente** | **`Validador Secuencia`** ← *no* el `XR Simple Interactable` |
| **Lista** | **`Al Tener Exito`** |
| **Cómo saber que es el correcto** | esa lista ya tiene cinco entradas, entre ellas `Avance_Reto1.Convocar()` |
| **Añadir** | `Guion_Reto1` → `GuionDePasos.Cumplir` → **`4`** |

### Por qué los dos últimos NO van en el clic

El maniquí y el lavamanos pasan por `ValidadorSecuencia`, que distingue el acierto
del error:

```
Maniqui [clic] → ValidarClic()
                 éxito → Lavamanos.DarPermiso() + sonido + se deshabilita
                 error → Canvas_ErrorManiqui
```

Si el paso se marcara en `Select Entered`, se marcaría **también cuando el
estudiante se equivoca de orden**. Le diríamos «ya equipaste tu dotación» justo
a quien acaba de fallar, que es el que más perdido está. Por eso van en
`Al Tener Exito`.

Los pasos 0, 1 y 2 sí van en `Select Entered`, porque esos tres objetos no
tienen validador: no hay forma de equivocarse al pulsarlos.

---

## Paso 5 — Comprobar en Play, antes del laboratorio

Esto no se puede dar por bueno sin verlo correr. Dé Play y recorra la lista:

**5.1 · El panel aparece.** Nada más dar Play debe verse el panel flotante, con
los cinco pasos y el primero resaltado en blanco con `▶`.

**5.2 · Los signos se leen.** Compruebe que `✓` y `▶` salen como signos y no
como cuadritos vacíos. Si salen cuadritos, cambie `Marca Hecho` a `OK` y
`Marca Actual` a `>` en el Inspector.

**5.3 · Se lee cómodo.** El panel está a 1,2 m, algo por debajo del eje visual.
Si tapa demasiado, baje `Escala`; si no se lee, súbala. Si molesta que esté tan
abajo o tan arriba, mueva `Altura` (negativo baja).

**5.4 · El seguimiento no marea.** Gire la cabeza despacio: el panel debe
quedarse quieto. Gire más de unos 14°: debe ir a buscar la mirada con retardo
suave, sin tirones. Si le parece que persigue demasiado, suba `Holgura`; si
tarda en aparecer cuando se gira, baje `Suavizado`.

**5.5 · Los pasos se marcan.** Haga la secuencia completa. Cada acción debe
dejar su línea en verde con `✓` y mover el `▶` a la siguiente. En la Console
verá una línea por paso:

```
[GuionDePasos] Paso 2 cumplido: Guarda tus objetos personales
```

Si alguna acción no marca su paso, el cableado de esa entrada está mal.

**5.6 · El error no marca.** Pruebe a hacer clic en el **lavamanos antes que en
el maniquí**. Debe salir `Canvas_ErrorLavamanos` y el panel **no** debe marcar
nada. Si lo marca, esa entrada está en `Select Entered` en vez de en
`Al Tener Exito`.

**5.7 · Al terminar.** Cumplidos los cinco, el panel se queda cuatro segundos
con todo en verde y se apaga. Si prefiere que no se apague, desmarque
`Ocultar Al Terminar`.

**5.8 · El reinicio.** Dispare el reinicio de la experiencia. El panel debe
volver a los cinco pasos sin marcar. No hay que cablear nada para esto:
`GuionDePasos` implementa `IReiniciable` y `ReinicioDeExperiencia` lo encuentra
solo.

---

## Resumen

| Qué | Quién | Cuánto |
|---|---|---|
| Escribir el componente | hecho | — |
| Comprobar si se ven las gafas RV | usted | 1 min |
| Crear `Guion_Reto1` y añadir el componente | usted | 2 min |
| Llenar los cinco pasos | usted | 3 min |
| Cablear los cinco disparadores | usted | 6 min |
| Comprobar en Play | usted | 5 min |

---

## Lo que queda después

- **Las otras zonas.** El componente sirve para cualquiera: un `Guion_` por zona,
  con su título y su lista. Hace falta que me dicte los pasos de los retos 2 y 3,
  igual que hizo con este.
- **Encadenarlos.** El campo `Al Terminar` de un guion puede llamar a
  `Mostrar()` del siguiente, para que el panel de la zona nueva aparezca solo.
- **El prefab perdido.** `f599cdf4f1433fa4a8c2135edd24a7b3` sigue sin estar en el
  proyecto, lo arregle o no el indicador.

- **El cuestionario, abierto desde el minuto uno.** `Logo proyecto 3D (Esc2)`
  arranca con `m_IsActive: 1`, tiene `MeshRenderer` y `BoxCollider`, y su
  `XR Simple Interactable` está marcado. O sea que se ve, se puede apuntar y,
  al hacer clic, enciende `Canvas_Cuestionario` — en cualquier momento de la
  sesión, incluido antes de empezar el reto 1.

  Puede ser deliberado. Pero si el cuestionario es el cierre de la experiencia,
  tenerlo accesible desde el principio es un accidente esperando: cuarenta
  estudiantes en veinte tandas, y basta que uno apunte ahí por curiosidad.

  No lo toco porque no sé si es intencionado. Si no lo es, la solución es de una
  línea: desmarcar el `XR Simple Interactable` en el Inspector y añadir
  `set_enabled(true)` sobre él en el evento que cierre el último reto.
