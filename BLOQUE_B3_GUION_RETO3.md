# Bloque B3 · El guion de pasos del reto 3

Guía de cableado para el tablero del reto 3. Sigue a
[BLOQUE_B2_GUION_RETO2.md](BLOQUE_B2_GUION_RETO2.md) y usa la misma mecánica de
nueve puntos por disparador.

Todos los datos de este documento salen de inspeccionar el repositorio, no de
ningún documento de planeación. Donde aparece una medida, está calculada con los
valores que hay hoy en `Assets/Scenes/Escena 1.unity` y en los scripts.

---

## Por qué el reto 3 lleva tres pasos y no seis

El reto 3 **ya tiene** un guion de pasos, y está dentro del simulador. El
tablero trae una fila `SECUENCIA` con seis puntos y seis rótulos —`Start`,
`Llenado`, `Calefactor`, `Refrig.`, `B.Prod.`, `Válido`— que se repintan en cada
fotograma leyendo el estado real del motor, con tres estados: apagado, en curso
y hecho. Está en `RefreshSteps()`, en
`Assets/Scripts/Pasteurizador/Simulator/PasteurizerSimDashboard.cs:865`.

Además los botones de control están numerados del 1 al 6 en el orden real del
procedimiento (`OrdenarYNumerarBotones()`, línea 404), la barra inferior dice
qué falta con mensajes como «Falta paso 2», «Faltan 12 °C» o «Falta nivel: 30 L»,
y hay una guía por voz con trece locuciones de hito y cuatro de corrección.

Repetir esos seis pasos en un segundo tablero sería decir lo mismo, peor y más
lejos. El del simulador reacciona al estado del equipo; un `GuionDePasos` solo
reacciona a que alguien pulsó algo.

Lo que nadie registraba es el avance **grueso** del reto: si el estudiante vio el
video del procedimiento, si arrancó el simulador y si llegó a cerrar un lote. De
hecho `CloseBatch()` era privado y lo único que hacía al cerrar el lote era
escribir el resultado en la barra de avisos
(`PasteurizerSimEngine.cs:228`). Eso son los tres pasos de este tablero.

| N.º | Paso | Se marca en |
|---|---|---|
| 0 | Mira el video del procedimiento | `Manager_Escena3` → `Al Dar Por Visto El Video` |
| 1 | Pulsa EMPEZAR en el simulador | `_StartOverlay` → `Al Pulsar Empezar` |
| 2 | Procesa un lote hasta vaciar el tanque | `Simulador` → `Al Cerrar El Lote` |

---

## Lo que hubo que añadir antes, y por qué

En los cuatro scripts del simulador no había **ni un solo `UnityEvent`**, así que
no había nada que cablear. Se añadieron tres, y un cuarto cambio que no estaba
previsto.

**`GestorEscena3.AlDarPorVistoElVideo`.** Se dispara una vez cuando el video se
da por visto. Reinicia su marca en `ReiniciarAlInicio()`.

**`PasteurizerStartOverlay.AlPulsarEmpezar`.** Una vez por tanda. La marca se
suelta en `ReiniciarAlInicio()` y **no** en `ShowAgain()`, porque a `ShowAgain()`
la llama también el botón «Reiniciar lote»: dentro de la misma tanda el arranque
ya se avisó y no hay por qué repetirlo.

**`PasteurizerSimEngine.AlCerrarElLote`.** Este **no** lleva marca de una sola
vez, a propósito: el estudiante puede procesar varios lotes seguidos con el botón
«Reiniciar lote», y que avise en cada uno es lo correcto. `GuionDePasos.Cumplir`
ignora los repetidos, de modo que el paso no se desmarca ni se duplica.

**El cuarto cambio, que es el que importa.** `ControlesVideoEsc3.Cerrar()` deja
al estudiante cerrar el video con el botón CERRAR, y paraba el reproductor sin
avisar a `GestorEscena3`. Si el aviso dependiera solo de que el video llegue al
final, quien lo cierre en el segundo tres se quedaría con el paso 0 sin marcar
**y sin manera de arreglarlo**, porque el video lo arranca el cuestionario al
llegar y no hay botón que lo repita. Ahora `Cerrar()` también lo da por visto.
Para eso `ControlesVideoEsc3` tiene un campo nuevo, `Gestor`, que se busca solo
al arrancar: **no hay que cablearlo**.

---

## Lo que este tablero NO lleva

**No lleva una fila `Mostrar()`.** En el reto 2 hubo que encender el panel desde
`Avance_Reto1`. Aquí no, y es mejor así: `Cumplir` ya llama a `Mostrar()` por
dentro, de modo que el tablero aparece solo en el momento en que se marca el
paso 0, es decir cuando acaba el video. Eso tiene dos ventajas. El panel no
compite con el video mientras se reproduce, y no hay que tocar
`Avance_Reto2 → Despues Del Salto`, de la que seguimos sin saber si alguien la
invoca.

El efecto es que el tablero aparece con su primer paso ya marcado. Es lo que se
quiere: lo que el panel tiene que decir es lo que falta.

**No hay salto posterior que cablear.** En la escena existen `Avance_Reto1` y
`Avance_Reto2`, pero **no hay `Avance_Reto3`**: el reto 3 es el último.

---

## Antes de empezar

Deje que Unity recompile y confirme en el Inspector que aparecen los tres campos
nuevos: `Al Dar Por Visto El Video` en `Manager_Escena3`, `Al Pulsar Empezar` en
`_StartOverlay` y `Al Cerrar El Lote` en `Simulador`. Si alguno no está, no
recompiló y lo demás no tiene sentido. Mire la Console antes de seguir.

---

## Paso 1 — Crear el objeto

En la jerarquía, clic derecho sobre **`Root`** → `Create Empty`. Nómbrelo
**`Guion_Reto3`**.

Deje su `Transform` en **Position 0 · 0 · 0**, **Rotation 0 · 0 · 0** y
**Scale 1 · 1 · 1**. Aquí la posición no importa, al contrario que en el reto 1,
porque este tablero va **anclado** a la pantalla del simulador y toma de ella su
sitio. La escala del `Transform` **tiene que quedarse en 1**: la escala del
tablero es un campo del componente, no del `Transform`.

---

## Paso 2 — Añadir el componente y llenarlo

`Add Component` → `Guion De Pasos`.

| Campo | Valor |
|---|---|
| `Titulo` | `SIMULADOR HTST` |
| `Pasos` → Size | **3** |

| N.º | `Texto` | `Sangrado` |
|---|---|---|
| 0 | `Mira el video del procedimiento` | sin marcar |
| 1 | `Pulsa EMPEZAR en el simulador` | sin marcar |
| 2 | `Procesa un lote hasta vaciar el tanque` | sin marcar |

**Colocación**

| Campo | Valor | Por qué |
|---|---|---|
| `Colocacion` | `Fijo Mirando Al Usuario` | igual que los otros dos |
| `Escala` | **0.0018** | la misma que usa la pantalla del simulador |
| `Ancla` | arrastre **`_SimDashboard`** | el tablero cuelga de la pantalla |
| `Desplazamiento Del Ancla` | **2.75 · 1.2 · -1** | a la derecha de la pantalla, despegado de la pared y por encima del equipo |
| `Giro Suavizado` | 6 | el de siempre |
| `Tamano De Letra` | **44** | la misma letra que los retos 1 y 2 |
| `Cumplir Marca Los Anteriores` | **marcado** | los tres pasos son una secuencia real |
| `Empezar Oculto` | **marcado** | lo enciende el paso 0 |
| `Ocultar Al Terminar` | marcado | |
| `Segundos Antes De Ocultar` | 2 | |

**De dónde sale el 2,75.** La pantalla del simulador mide 1600 × 900 px con una
escala efectiva de 0,0019, es decir **3,07 × 1,73 m**, con el centro en
(−2,68 · 1,06 · 2,92) y sin inclinación ni alabeo. Este tablero, con tres pasos y
letra 44, mide **1,98 × 0,78 m**. El 2,75 son 1,54 de media pantalla más 0,99 de
medio tablero más 22 cm de separación, de modo que el tablero queda centrado en
x ≈ 0,07 y a la misma altura que la pantalla.

**Por qué a la derecha y no encima ni a la izquierda.** Encima quedaba con el
centro a 2,46 m: el estudiante lee desde `Punto_Inicio_Escena3`
(−2,72 · 0 · 1,30), a 1,6 m de la pantalla, y desde ahí mirar a 2,46 m son 28°
de cuello hacia arriba. A la izquierda el tablero caería en x ≈ −5,43, pasado
`Puerta de vidrio (bodega)` (x = −4,49), que es el objeto más a la izquierda de
toda la planta junto con la puerta de salida: ahí ya es pared. A la derecha cae
en x ≈ 0,07, y los dos únicos vecinos en esa zona son `Canvas_Esc3` y
`Canvas_VideoEsc3`, los dos en (0 · 2,53) y los dos **apagados en la escena**: el
splash se desactiva a sí mismo justo después de arrancar el video, y el video se
apaga al terminar, que es el instante en que aparece este tablero.

El desplazamiento se aplica en metros y respeta la orientación del ancla, pero
**no su escala**. Como `_SimDashboard` tiene giro 0 en mundo, el eje x del
desplazamiento coincide con el x del mundo: positivo es la derecha del
estudiante.

Entre el borde derecho de la pantalla (x = −1,14) y el borde izquierdo del
tablero (x = −0,92) quedan 22 cm. Si lo quiere más separado, suba el 2,75; si lo
quiere más pegado, bájelo, y mírelo con el gizmo antes de dejarlo.

**De dónde sale el −1, y es el número más importante de esta tabla.** La pared
del fondo de la planta es una fila de cuatro segmentos `wall01` en **z = 3,02**,
y la pantalla del simulador está colgada de ella, a 10 cm. Si el tablero se deja
a la misma profundidad que la pantalla hereda esos 10 cm, y ahí aparece el
problema: **este tablero gira sobre su centro para mirar al usuario**. Con 1,98 m
de ancho, su semiancho es 0,99, de modo que a 10 cm de la pared le bastan
**5,9° de giro** para que una esquina se clave en la obra. El estudiante lee desde
la izquierda, así que el tablero gira mucho más que eso y se ve atravesado.

La regla, para cualquier tablero de estos: **el centro tiene que quedar a más de
un semiancho de cualquier pared**, porque gira. Con el −1 el centro se va a
z ≈ 1,92, a 1,10 m de la pared: por encima de los 0,99, de modo que ya no se
clava en ningún ángulo.

**Compruebe el signo.** El desplazamiento se aplica en los ejes del ancla. Al
escribir el −1, la caja naranja del gizmo tiene que moverse **hacia el
pasteurizador**, es decir hacia el interior de la planta. Si se mueve hacia la
pared, ponga `1` en vez de `-1`.

**De dónde sale el 1,4, y por qué hicieron falta tres intentos.** Con el
desplazamiento en `2.75 · 0 · -1` el tablero dejó de clavarse en la pared y pasó
a clavarse en el modelo 3D del pasteurizador, que ocupa justo esa franja de
suelo. No es que el −1 estuviera mal elegido: **ese corredor no existe**. El
tablero necesita su centro a más de 0,99 m de la pared, lo que lo empuja a
z ≤ 2,03, y hacia ahí está el equipo. En el plano del suelo, a la altura de la
pantalla, no hay ningún punto que cumpla las dos condiciones a la vez.

La huella exacta del pasteurizador no se puede medir desde los archivos: su
prefab trae cuatro colliders de caja de 10 × 3,5 × 4,6 que son una envolvente y
no el equipo. Por eso la salida fue subirlo. El 1,2 es el valor al que se llegó
con el gizmo, bajando desde 1,4 hasta dejar el tablero rozando por encima de las
tuberías: el centro queda a **2,26 m**, y el estudiante lo mira con unos **19° de
cuello** desde `Punto_Inicio_Escena3`, frente a los 28° de la primera versión.
Sigue estando arriba, pero menos, porque ahora está 2,9 m más lejos de él y no
encima de su cabeza. La z no se toca: el 1,10 m de separación de la pared es la
condición que no se puede romper.

**Si todavía roza las tuberías**, suba la Y de 0,2 en 0,2 con el gizmo. La otra
salida es estrecharlo: con `Tamano De Letra` en 36 el tablero mide 1,62 m y su
semiancho baja a 0,81, con lo que admite bajar y acercarse más a la pared. Es la
última opción, porque la letra pequeña fue justo lo que hubo que corregir en el
tablero del reto 1.

**Hágalo con el gizmo y con Play detenido.** Con `Guion_Reto3` seleccionado, la
vista de escena dibuja la caja naranja al tamaño real del tablero y el poste
desde el ancla. Gire la cámara alrededor del pasteurizador y verá el cruce sin
ponerse el casco. Y recuerde que lo que se escribe en el Inspector **con Play
activo se descarta al salir**.

---

## Paso 3 — Los tres disparadores, uno por uno

Las tres listas están **vacías**: son campos nuevos. Si alguna no lo está, pare:
está en el objeto equivocado.

---

### N.º 0 · `Manager_Escena3` → número **0**

#### ① Encontrar el objeto

En la lupa de la jerarquía escriba `Manager_Escena3`. Su ruta es
`Root / Planta_Piloto / Manager_Escena3`. Está activo.

#### ② Abrir la lista correcta

En el Inspector baje hasta **`Gestor Escena3`**. Tiene dos apartados:

```
  Pantalla de Video
     Canvas Video
     Reproductor Tutorial
  Aviso de video visto
     Al Dar Por Visto El Video    <-- ESTA
```

#### ③ Comprobar que está en el sitio correcto

`Al Dar Por Visto El Video` debe estar **vacía**: `List is Empty`.

#### ④ Añadir la fila

El **`+`** de la lista.

#### ⑤ Arrastrar el objeto

Borre el texto de la lupa para que `Guion_Reto3` sea visible, y arrástrelo al
hueco `None (Object)`.

#### ⑥ Elegir el método

`AlDarPorVistoElVideo` es un `UnityEvent` **sin argumento**, de modo que el menú
sale **sin partir**: no hay bloque `Dynamic` ni `Static Parameters`, los
componentes aparecen directamente. Elija `GuionDePasos` → **`Cumplir (int)`**.
Señal de acierto: aparece la casilla numérica.

#### ⑦ Escribir el número

Déjelo en **`0`**, que es el valor por omisión. Compruébelo igual.

#### ⑧ Cómo queda

```
  [ Runtime Only  v ]  [ GuionDePasos.Cumplir  v ]
  [ Guion_Reto3 (GuionDePasos) ]
  [ 0 ]
```

`Al Dar Por Visto El Video`: **1 entrada**.

#### ⑨ Guardar

`Ctrl+S`.

---

### N.º 1 · `_StartOverlay` → número **1**

#### ① Encontrar el objeto

En la lupa escriba `_StartOverlay`. Es **hijo de `_SimDashboard`**, que a su vez
cuelga de `Simulador`. La ruta completa es
`Root / Planta_Piloto / Simulador / _SimDashboard / _StartOverlay`.

Es la ventana emergente con el botón EMPEZAR que cubre el tablero del simulador
al llegar. Si no la encuentra en la jerarquía, despliegue `Simulador`: es una
instancia de prefab y por eso aparece cerrada.

#### ② Abrir la lista correcta

Baje hasta **`Pasteurizer Start Overlay`** y busque el apartado
`Aviso de arranque`, con la lista **`Al Pulsar Empezar`**.

#### ③ Comprobar que está en el sitio correcto

`Al Pulsar Empezar` debe estar **vacía**.

#### ④–⑤ Añadir la fila y arrastrar

El **`+`**, borrar la lupa, arrastrar **`Guion_Reto3`**.

#### ⑥ Elegir el método

Menú **sin partir**: `GuionDePasos` → **`Cumplir (int)`**.

#### ⑦ Escribir el número

Escriba **`1`**. El `0` que sale por omisión hay que cambiarlo.

#### ⑧ Cómo queda

`Al Pulsar Empezar`: **1 entrada**, con un `1` en la casilla.

#### ⑨ Guardar

`Ctrl+S`.

---

### N.º 2 · `Simulador` → número **2**

#### ① Encontrar el objeto

En la lupa escriba `Simulador`. Su ruta es `Root / Planta_Piloto / Simulador`.
Es una instancia del prefab
`Assets/3D Laboratory Environment with Appratus/Models/Smart led TV.fbx`,
renombrada en la escena: el simulador 2D vive sobre el modelo de un televisor de
pared. El componente está en **la raíz de esa instancia**, no en la pantalla.

**No lo confunda con `_SimDashboard`**, que es el lienzo de la pantalla y cuelga
de este. El motor está en el padre.

#### ② Abrir la lista correcta

Baje hasta **`Pasteurizer Sim Engine`**. Es un componente largo, con todo el
estado del proceso a la vista —temperaturas, volúmenes, válvulas, CIP—. El
apartado nuevo es `Aviso de lote cerrado`, con la lista **`Al Cerrar El Lote`**,
y está **arriba, justo después del campo `Mode`**, no al final.

#### ③ Comprobar que está en el sitio correcto

`Al Cerrar El Lote` debe estar **vacía**.

#### ④–⑤ Añadir la fila y arrastrar

El **`+`**, borrar la lupa, arrastrar **`Guion_Reto3`**.

#### ⑥ Elegir el método

Menú **sin partir**: `GuionDePasos` → **`Cumplir (int)`**.

#### ⑦ Escribir el número

Escriba **`2`**.

#### ⑧ Cómo queda

`Al Cerrar El Lote`: **1 entrada**, con un `2` en la casilla.

#### ⑨ Guardar

`Ctrl+S`.

---

## Resumen para ir marcando

| | Objeto | Componente | Lista | N.º | Entradas antes → después |
|---|---|---|---|---|---|
| ☐ | `Manager_Escena3` | Gestor Escena3 | `Al Dar Por Visto El Video` | **0** | 0 → 1 |
| ☐ | `_StartOverlay` | Pasteurizer Start Overlay | `Al Pulsar Empezar` | **1** | 0 → 1 |
| ☐ | `Simulador` | Pasteurizer Sim Engine | `Al Cerrar El Lote` | **2** | 0 → 1 |

Las tres listas son campos nuevos, de modo que las tres tienen que estar vacías
antes de empezar. **Si alguna no lo está, pare: está en el objeto equivocado.**

Los tres menús del punto ⑥ salen **sin partir**, porque los tres eventos son
`UnityEvent` sin argumento.

---

## Paso 4 — Comprobar en Play, antes del laboratorio

**4.1 · Los campos existen.** Antes de nada, los tres apartados nuevos tienen que
verse en el Inspector. Si no, Unity no recompiló.

**4.2 · El tablero no se ve al principio.** Entre a Play. Durante los retos 1 y 2
el tablero del reto 3 **no debe aparecer**, ni de lejos. Si aparece,
`Empezar Oculto` quedó sin marcar.

**4.3 · Aparece al acabar el video.** Llegue al reto 3. El video arranca solo.
Déjelo terminar: al acabar debe aparecer el tablero con el paso 0 **ya marcado**.
En la Console:

```
[GuionDePasos] Paso 0 cumplido: Mira el video del procedimiento
```

**4.4 · Y también si lo cierra.** Repita la prueba, pero esta vez pulse CERRAR a
los pocos segundos. El tablero debe aparecer igual, con el paso 0 marcado. Si no
aparece, el campo `Gestor` de `ControlesVideoEsc3` no encontró el gestor: mire si
hay un aviso en la Console.

**4.5 · Se lee desde donde se opera.** Colóquese delante de la pantalla del
simulador, a la distancia a la que se pulsan los botones, y lea el tablero. Mire
también que no tape nada de la pantalla: la fila `SECUENCIA` y la barra de avisos
de abajo son las dos cosas que no puede estorbar.

**4.5b · El tablero del reto 2 ya no está.** Al llegar al reto 3, el tablero de
la planta piloto debe haber desaparecido, aunque le quedaran pasos sin marcar.
Y debe seguir desaparecido después: toque el pasteurizador y el carrusel durante
el reto 3 y compruebe que **no reaparece**. Eso es lo que hace `Cerrar()` y lo
que `Ocultar()` no lograba.

**4.6 · EMPEZAR marca el paso 1.** Pulse EMPEZAR. Debe marcarse el paso 1 y
arrancar la narración. Pulse «Reiniciar lote» y vuelva a pulsar EMPEZAR: **no**
debe pasar nada nuevo en el tablero.

**4.7 · El lote marca el paso 2.** Haga la secuencia completa: Energía, Iniciar,
Llenado, Calefactor, Refrigerador y Bomba de producto. Deje que el tanque de
entrada se vacíe. Al vaciarse, la barra de avisos dice «Lote cerrado. Procesado…»
y ahí debe marcarse el paso 2. Con los tres marcados el tablero se oculta a los
2 segundos.

**4.8 · El reinicio.** Dispare el reinicio de la experiencia. El tablero debe
**apagarse** y volver a los tres pasos sin marcar, el video debe quedar rebobinado
y la ventana EMPEZAR debe estar otra vez delante del simulador. No hay que
cablear nada: los cuatro componentes implementan `IReiniciable` y
`ReinicioDeExperiencia` los encuentra solo.

---

## Lo que queda después

- Confirmar en Play que `Avance_Reto2` se invoca de verdad. Hoy el salto al
  reto 3 lo encadena `GestorCuestionario` por código y ningún evento del
  Inspector lo llama. Este tablero no depende de ello, pero el salto sí.
- El modo **CIP** del motor (`Mode.Cip`, con sus cinco etapas de limpieza) no
  entra en ningún paso del guion. Si el reto 3 debe incluir la limpieza del
  equipo, eso es un cuarto paso y haría falta un aviso más en el motor.
- Los avisos de TextMeshPro sobre `enableWordWrapping`, que son obsolescencia y
  no error.
