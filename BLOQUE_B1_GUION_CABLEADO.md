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

**La posición de este objeto sí importa.** Es donde se clava el tablero: el
componente lo pone ahí y lo gira en yaw para que mire al usuario, sin
desplazarlo nunca.

En el Transform, ponga de partida **X = −2,4 · Y = 0 · Z = −6,3**. Es el
centroide de los tres objetos de la zona —`locker room (3)`, `Maniqui` y
`Lavamanos`—, calculado desde sus cadenas de Transform en la escena, de modo que
el punto más alejado queda a unos 3,4 m. La Y se deja en 0 porque la altura la
pone `Desplazamiento Del Ancla`, que de serie sube el centro del tablero a
1,80 m.

**No lo coloque a ciegas: el tablero se dibuja en la vista de escena.** Con
`Guion_Reto1` seleccionado verá un rectángulo naranja del tamaño real del
tablero, un poste que va del objeto hasta el centro del tablero, y una rayita
verde a su izquierda que mide **una letra a tamaño real**. Mueva el objeto hasta
que el rectángulo quede donde quiere, sin tapar los lockers, el maniquí ni el
lavamanos, y sin atravesar el techo. Eso se resuelve en la vista de escena, sin
entrar al visor.

> **Por qué el centroide y no el punto donde se para el estudiante.** La escena
> no trae XR Origin: el rig lo pone VIROO en ejecución, así que desde el
> proyecto no hay manera de saber dónde queda parado el participante. El
> centroide de los tres objetos es la mejor aproximación verificable, y de ahí
> en adelante manda lo que vea con las gafas puestas.

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

## Paso 4 — Avisarle al panel cuando un paso se cumple

### La idea, en un párrafo

`GuionDePasos` no adivina nada. No sabe que el estudiante pulsó Play, ni que
abrió el casillero. **Alguien tiene que avisarle**, y ese aviso es una sola
llamada: `Cumplir(0)` para el primer paso, `Cumplir(1)` para el segundo, y así
hasta `Cumplir(4)`.

Lo bueno es que ya hay dónde ponerla. Los cinco objetos que el estudiante toca
tienen cada uno, en el Inspector, una lista de **«qué hacer cuando esto pase»**:
el logo de YouTube arranca el video, el casillero le da permiso al maniquí, el
lavamanos convoca el siguiente reto. Esas listas ya existen y ya funcionan.

El paso 4 es añadir **una línea más al final de cada una de esas cinco listas**:

> «…y además avísale a `Guion_Reto1` de que el paso N está hecho.»

Cinco objetos, cinco listas, una línea nueva en cada una. **El gesto es idéntico
las cinco veces.** Lo único que cambia son tres datos: en qué objeto, en cuál de
sus listas, y qué número escribe.

Así que lo hacemos **uno completo y despacio** (4.1), y los otros cuatro son
copia de ese con los tres datos cambiados (4.2).

---

### 4.1 · Hágalo una vez: el paso 0, el video

#### ① Encontrar el objeto

En la Hierarchy, arriba del panel, hay un **campo de búsqueda con una lupa**.
Escriba ahí:

```
Youtube
```

Sale un solo objeto: **`Youtube Logo`**.

> **Por qué hay que buscarlo y no se le da una ruta:** este objeto es raíz de
> una instancia de prefab. No cuelga de `Root`; aparece suelto en el árbol. No
> hay ruta que darle.

> **No lo confunda con `Pause`.** Hay un objeto hermano parecido que solo
> contiene `PantallaLed.Pause()`. El de Play es `Youtube Logo`.

Al escribir en la lupa, la Hierarchy deja de mostrar el árbol y pasa a una lista
plana. Es normal.

#### ② Abrir la lista correcta en el Inspector

Con `Youtube Logo` seleccionado, mire el Inspector (panel derecho). Baje hasta
encontrar el componente **`XR Simple Interactable`**.

Dentro de ese componente hay varias secciones plegadas. Busque la que dice
**`Interactable Events`** y pulse su triangulito para abrirla.

Se despliegan varias sublistas con nombres parecidos:

```
  First Hover Entered
  Hover Entered
  Hover Exited
  Select Entered      <-- ESTA
  Select Exited
  Activated
  Deactivated
```

Abra **`Select Entered`**. «Select» aquí significa *clic*: es lo que pasa cuando
el estudiante dispara el gatillo apuntando al objeto.

#### ③ Comprobar que está en el objeto correcto

Antes de tocar nada, cuente lo que ya hay en esa lista.

**Debe tener exactamente 1 entrada, y es `PantallaLed.Play()`.**

Si ve otra cosa, está en otro objeto. Vuelva a ①. Esta cuenta es la comprobación
más barata que tiene y le ahorra el 90 % de los errores posibles.

#### ④ Añadir la fila

Abajo a la derecha de la lista hay un **`+`** pequeño. Púlselo.

Aparece una fila vacía **al final**, con esta forma:

```
  [ Runtime Only  v ]  [ No Function  v ]
  [ None (Object)    ]
```

**No toque el `Runtime Only`.** Así está bien.

#### ⑤ Arrastrar el objeto

Para poder arrastrar `Guion_Reto1` tiene que verlo en la Hierarchy, y ahora la
lupa lo está escondiendo. **Borre el texto de la lupa** (el árbol vuelve), y
arrastre **`Guion_Reto1`** desde la Hierarchy hasta el hueco que dice
`None (Object)`.

> Si al borrar la lupa pierde la selección del Inspector, no pasa nada: el
> Inspector se queda mostrando `Youtube Logo` mientras no pulse otra cosa. Si lo
> pierde, use el candadito del Inspector (arriba a la derecha) para fijarlo antes
> de borrar la búsqueda.

#### ⑥ Elegir el método — aquí está la única trampa real

Pulse el desplegable que dice **`No Function`**. Se abre un menú **partido en
dos bloques**:

```
  Dynamic SelectEnterEventArgs
     ...                      <-- aquí NO está Cumplir
  ─────────────────────────────
  Static Parameters
     GuionDePasos
        Cumplir (int)         <-- ESTE
        Mostrar ()
        Ocultar ()
        ReiniciarAlInicio ()
```

Elija **`Cumplir (int)`**, el del bloque **`Static Parameters`**.

Es el único sitio donde aparece. El bloque de arriba («Dynamic») solo lista
métodos que sepan recibir lo que el evento entrega, y `Cumplir` recibe un número
entero, no un `SelectEnterEventArgs`.

**Esto importa porque equivocarse aquí no da ningún aviso.** No hay error rojo,
no hay warning: simplemente el paso no se marca nunca y usted se queda buscando
el fallo en otra parte.

**Cómo saber que acertó:** debajo del desplegable aparece **una casilla numérica
con un `0`**. Si esa casilla no aparece, eligió otra cosa. Repita ⑥.

#### ⑦ Escribir el número

En esa casilla numérica escriba **`0`** (es el primer paso de la lista, y la
lista cuenta desde cero).

En este caso el `0` ya estaba puesto por defecto. Compruébelo igual.

#### ⑧ Cómo se ve la fila terminada

```
  [ Runtime Only  v ]  [ GuionDePasos.Cumplir  v ]
  [ Guion_Reto1 (GuionDePasos) ]
  [ 0 ]
```

Tres señales de que está bien: dice `Runtime Only`, el método incluye
`.Cumplir`, y hay casilla con número.

Y la lista completa de `Select Entered` debe tener ahora **2 entradas**:
`PantallaLed.Play()` y la suya.

#### ⑨ Guardar

**`Ctrl+S`.** Unity no guarda solo. Perder el cableado por esto sería absurdo.

> **Una decisión que conviene que revise.** Así el paso se marca al *pulsar*
> Play, no al terminar el video. Es literal respecto a su instrucción
> («reproduce el video»), pero tiene un efecto: en cuanto pulsan, el panel ya
> señala el paso siguiente, y alguien podría irse con el video a medias.
> Marcarlo al terminar no se puede cablear desde el Inspector, porque
> `VideoPlayer` no expone ese evento; haría falta un componente pequeño que se
> suscriba a `loopPointReached`. Son veinte líneas. Dígame si lo prefiere.

---

### 4.2 · Los otros cuatro, uno por uno

Los nueve puntos de §4.1 se repiten cuatro veces. Lo que cambia en cada vuelta
son tres datos: **qué objeto**, **en qué lista** y **qué número**. Pero en dos de
los cuatro cambia también el componente, y eso altera lo que verá en el
Inspector y en el menú del punto ⑥. Por eso aquí va cada uno desarrollado
completo, sin remitirle a §4.1.

**De dónde salen los números.** `Cumplir` recibe el índice del paso dentro de la
lista `pasos`, y esa lista cuenta desde cero. El cero es el vídeo, que ya está
cableado. Así que a estos cuatro les corresponde 1, 2, 3 y 4, en el mismo orden
en que los escribió en el panel:

| N.º | Texto del paso, tal como está en `Guion_Reto1` | Objeto que lo cumple |
|---|---|---|
| 0 | Acércate a la pantalla y reproduce el video | `Youtube Logo` — **ya hecho** |
| 1 | Acércate a las gafas RV y haz clic | `Logo Simulador VR` |
| 2 | Guarda tus objetos personales | `locker room (3)` |
| 3 | Equipa tu dotación de bioseguridad | `Maniqui` |
| 4 | Realiza la higienización de manos y botas | `Lavamanos` |

**El objeto que va a arrastrar es siempre el mismo:** `Guion_Reto1`, que está en
`Root / Guion_Reto1`. No cambia en ninguna de las cuatro vueltas.

---

#### N.º 1 · `Logo Simulador VR` → número **1**

##### ① Encontrar el objeto

En la lupa de la Hierarchy escriba:

```
Logo Simulador
```

Salen **dos** resultados y los dos se llaman exactamente igual:

```
Logo Simulador VR (Missing Prefab with guid: f599cdf4f1433fa4a8c2135edd24a7b3)
```

El sufijo `(Missing Prefab…)` es correcto y no es un fallo nuevo: el prefab
original no está en el proyecto, pero el objeto conserva su propio collider y
sigue funcionando (ver paso 0.1). Lo que hay que resolver es cuál de los dos.

##### ①b Distinguir el bueno del otro

Pulse uno de los dos y mire el Inspector. Los dos tienen **tres componentes**,
en este orden: `Transform`, `Capsule Collider`, `XR Simple Interactable`. El
criterio es la **casilla a la izquierda del nombre `XR Simple Interactable`**:

| Si esa casilla está… | Es… | Cuelga de | ¿Lo cablea? |
|---|---|---|---|
| **marcada** | el bueno | `Root / Planta_Piloto / Logo proyecto 3D` | **sí** |
| **desmarcada** | el otro | `Root / Planta_Piloto / Logo proyecto 3D (Esc2)` | no |

Esa es la única diferencia visible: las dos listas de eventos tienen las mismas
dos entradas, así que mirar la lista no le sirve para elegir. Si quiere una
segunda confirmación, borre la lupa y mire de qué padre cuelga el que tiene
seleccionado: `Logo proyecto 3D` es el bueno; `Logo proyecto 3D (Esc2)`, no.

> **El padre del bueno no se llama como el modelo.** Es una instancia de
> `Meshy_AI_A_3D_stylized_icon_of_0504212831_texture.fbx` renombrada en la escena
> a `Logo proyecto 3D`. Es el nombre que verá en la Hierarchy.

> **Y hay un tercer objeto de nombre parecido**, `Logo proyecto 3D (Esc2)`, que
> es el padre del otro y enciende `Canvas_Cuestionario`. Ese tampoco es.

##### ② Abrir la lista correcta

Con el bueno seleccionado, en el componente **`XR Simple Interactable`** abra
la sección **`Interactable Events`** y, dentro, **`Select Entered`**.

##### ③ Comprobar que está en el objeto correcto

`Select Entered` debe tener **exactamente 2 entradas**:

```
  1   VozOff2 (AudioSource) . Play ()
  2   Canvas . SetActive (bool)      con la casilla MARCADA
```

Si ve otra cosa, está en otro objeto. Vuelva a ①.

##### ④ Añadir la fila

Pulse el **`+`** de abajo a la derecha de `Select Entered`. Sale una fila vacía
al final. **No toque el `Runtime Only`.**

##### ⑤ Arrastrar el objeto

Borre el texto de la lupa para que vuelva el árbol y arrastre **`Guion_Reto1`**
(está en `Root / Guion_Reto1`) al hueco que dice `None (Object)`. Si al borrar la
lupa teme perder la selección, fije el Inspector con el candadito de arriba a la
derecha antes de borrarla.

##### ⑥ Elegir el método

Pulse `No Function`. El menú **viene partido en dos bloques**, porque
`Select Entered` entrega un `SelectEnterEventArgs`:

```
  Dynamic SelectEnterEventArgs
     ...                      <-- aquí NO está Cumplir
  ─────────────────────────────
  Static Parameters
     GuionDePasos
        Cumplir (int)         <-- ESTE
```

Elija `Cumplir (int)` del bloque **`Static Parameters`**. Señal de acierto:
debajo aparece **una casilla numérica**.

##### ⑦ Escribir el número

La casilla sale con `0` por defecto. **Cámbielo a `1`.** Este es el único punto
donde se separa del paso 0, y equivocarse aquí no da ningún aviso: el paso 1 no
se marcaría nunca y el 0 se marcaría dos veces.

##### ⑧ Cómo queda

```
  [ Runtime Only  v ]  [ GuionDePasos.Cumplir  v ]
  [ Guion_Reto1 (GuionDePasos) ]
  [ 1 ]
```

`Select Entered` debe tener ahora **3 entradas**.

##### ⑨ Guardar

**`Ctrl+S`.**

---

#### N.º 2 · `locker room (3)` → número **2**

##### ① Encontrar el objeto

Hay **ocho** objetos `locker room` en la escena y solo el **(3)** es
interactivo. Escriba en la lupa el nombre completo, con el paréntesis:

```
locker room (3)
```

Sale uno solo. Su ruta real es `Root / Planta_Piloto / Lockers / locker room (3)`.

##### ② Abrir la lista correcta

Tiene **tres componentes**: `Transform`, `Box Collider`,
`XR Simple Interactable`. En el tercero abra `Interactable Events` →
**`Select Entered`**.

##### ③ Comprobar que está en el objeto correcto

`Select Entered` debe tener **exactamente 4 entradas**, en este orden:

```
  1   Maniqui (ValidadorSecuencia) . DarPermiso ()
  2   (un hijo del propio locker) . SetActive (bool)   casilla DESMARCADA
  3   SND_Click (AudioSource) . Play ()
  4   locker room (3) (XRSimpleInteractable) . enabled   casilla DESMARCADA
```

La cuarta es el propio interactable apagándose, para que el clic no se pueda
repetir. La segunda apunta a un hijo que viene del modelo `locker room.fbx`, por
lo que su nombre lo pone el modelo y no la escena.

##### ④ Añadir la fila

**`+`** de `Select Entered`. **No toque el `Runtime Only`.**

##### ⑤ Arrastrar el objeto

Borre la lupa y arrastre **`Guion_Reto1`** al hueco `None (Object)`.

##### ⑥ Elegir el método

Menú **partido en dos bloques**, igual que en el N.º 1. `Cumplir (int)` está en
**`Static Parameters`**, bajo `GuionDePasos`. Señal de acierto: aparece la
casilla numérica.

##### ⑦ Escribir el número

Escriba **`2`**.

##### ⑧ Cómo queda

`Select Entered` debe tener ahora **5 entradas**, y la quinta es
`Guion_Reto1 (GuionDePasos) . Cumplir` con un `2`.

##### ⑨ Guardar

**`Ctrl+S`.**

---

#### N.º 3 · `Maniqui` → número **3** · aquí cambia el componente

##### ① Encontrar el objeto

En la lupa:

```
Maniqui
```

Su ruta real es `Root / Planta_Piloto / Vitrina / Maniqui`. La lupa saca también
`SND_Click_Maniqui` y `Canvas_ErrorManiqui`: el que busca es el que se llama
`Maniqui` a secas.

##### ② Abrir la lista correcta — **no es el interactable**

El Inspector muestra **seis componentes**, en este orden:

```
  1  Transform
  2  Mesh Filter
  3  Mesh Renderer
  4  Box Collider
  5  XR Simple Interactable      <-- NO es este
  6  Validador Secuencia         <-- ES este, el último
```

Baje hasta **`Validador Secuencia`**. Tiene tres cosas visibles: la casilla
`Permiso Para Avanzar`, la lista **`Al Tener Exito`** y la lista
`Al Tener Error`.

- Trabaje en **`Al Tener Exito`**.
- **No toque `Al Tener Error`**, que hoy tiene una entrada.
- **No toque `Permiso Para Avanzar`**, que hoy está **desmarcada**. Así debe
  quedar: la marca el `locker room (3)` en tiempo de ejecución, con el
  `DarPermiso()` que usted acaba de ver en el N.º 2.

> **Por qué no va en el clic.** El `XR Simple Interactable` de este objeto tiene
> una sola entrada en `Select Entered`: `Maniqui.ValidarClic()`. Ese método
> distingue el acierto del error. Si el paso se marcara en el clic, se marcaría
> también cuando el estudiante se equivoca de orden.

##### ③ Comprobar que está en el objeto correcto

`Al Tener Exito` debe tener **exactamente 4 entradas**, en este orden:

```
  1   Lavamanos (ValidadorSecuencia) . DarPermiso ()
  2   SND_Click_Maniqui (AudioSource) . Play ()
  3   (un hijo del propio maniquí) . SetActive (bool)   casilla DESMARCADA
  4   Maniqui (XRSimpleInteractable) . enabled           casilla DESMARCADA
```

Y `Al Tener Error`, **1 entrada**. Si no coincide, pare: está en otro objeto o
en la lista equivocada.

##### ④ Añadir la fila

Hay **dos `+`** en este componente, uno por lista. Use el de **`Al Tener
Exito`**, que es el de arriba. Si duda, pliegue `Al Tener Error` con su
triangulito antes de pulsar; así solo queda un `+` visible.

##### ⑤ Arrastrar el objeto

Borre la lupa y arrastre **`Guion_Reto1`** al hueco `None (Object)`.

##### ⑥ Elegir el método — **aquí el menú NO viene partido**

`Al Tener Exito` es un `UnityEvent` sin argumento, así que no hay bloque
`Dynamic` y no hay encabezado `Static Parameters`. Al pulsar `No Function` verá
directamente la lista de componentes del objeto que arrastró:

```
  No Function
  ─────────────────────────────
  GameObject        >
  GuionDePasos      >   Cumplir (int)        <-- ESTE
                        Mostrar ()
                        Ocultar ()
                        ReiniciarAlInicio ()
```

Elija **`Cumplir (int)`**. La señal de acierto es la misma que en los otros:
debajo del desplegable aparece **una casilla numérica**. Si no aparece, eligió
`Mostrar`, `Ocultar` o `ReiniciarAlInicio`; repita ⑥.

##### ⑦ Escribir el número

Escriba **`3`**.

##### ⑧ Cómo queda

```
  [ Runtime Only  v ]  [ GuionDePasos.Cumplir  v ]
  [ Guion_Reto1 (GuionDePasos) ]
  [ 3 ]
```

`Al Tener Exito` debe tener ahora **5 entradas**; `Al Tener Error` sigue con
**1**.

##### ⑨ Guardar

**`Ctrl+S`.**

---

#### N.º 4 · `Lavamanos` → número **4** · mismo cambio, y un componente más que confunde

##### ① Encontrar el objeto

En la lupa:

```
Lavamanos
```

Su ruta real es `Root / Planta_Piloto / Lavamanos`. La lupa saca también
`SND_Click_Lavamanos`; el que busca es `Lavamanos` a secas.

##### ② Abrir la lista correcta — hay dos componentes con listas de eventos

El Inspector muestra **siete componentes**:

```
  1  Transform
  2  Mesh Filter
  3  Mesh Renderer
  4  Box Collider
  5  XR Simple Interactable      <-- NO
  6  Validador Secuencia         <-- ES este
  7  Fade Out VR                 <-- NO, aunque también tenga una lista
```

El séptimo, `Fade Out VR`, tiene una lista llamada
`On Teletransporte Ejecutado` con cuatro entradas. **No es esa.** La diferencia
está en el nombre de la lista: la que busca se llama **`Al Tener Exito`** y está
en `Validador Secuencia`, el sexto.

Igual que en el N.º 3: no toque `Al Tener Error` ni `Permiso Para Avanzar`, que
hoy está desmarcada y la marca el `Maniqui` en ejecución.

##### ③ Comprobar que está en el objeto correcto

`Al Tener Exito` debe tener **exactamente 5 entradas**, en este orden:

```
  1   SND_Click_Lavamanos (AudioSource) . Play ()
  2   (un hijo del propio lavamanos) . SetActive (bool)   casilla DESMARCADA
  3   Lavamanos (XRSimpleInteractable) . enabled           casilla DESMARCADA
  4   Avance_Reto1 (ReunionEnElCentro) . Convocar ()
  5   Bisagra_Puerta (ControladorPuerta) . AbrirPuerta ()
```

Y `Al Tener Error`, **1 entrada**. Las entradas 4 y 5 son la señal de que está
en el objeto correcto: es el único de los cinco que convoca la reunión y abre la
puerta, porque es el último paso del reto.

##### ④ Añadir la fila

El **`+`** de `Al Tener Exito`. Si duda entre los dos `+`, pliegue
`Al Tener Error` antes de pulsar.

##### ⑤ Arrastrar el objeto

Borre la lupa y arrastre **`Guion_Reto1`** al hueco `None (Object)`.

##### ⑥ Elegir el método

Menú **sin partir**, igual que en el N.º 3: `GuionDePasos` →
**`Cumplir (int)`**. Señal de acierto: aparece la casilla numérica.

##### ⑦ Escribir el número

Escriba **`4`**.

##### ⑧ Cómo queda

`Al Tener Exito` debe tener ahora **6 entradas**, y la sexta es
`Guion_Reto1 (GuionDePasos) . Cumplir` con un `4`. `Al Tener Error` sigue con
**1**.

##### ⑨ Guardar

**`Ctrl+S`.**

---

#### Las cuatro diferencias, en una tabla

Si prefiere trabajar con una sola vista, esto es todo lo que cambia:

| N.º | Objeto | Componente | Lista | Entradas antes → después | Menú ⑥ | Número |
|---|---|---|---|---|---|---|
| 1 | `Logo Simulador VR` (el de casilla marcada) | XR Simple Interactable | `Select Entered` | 2 → 3 | partido, use `Static Parameters` | **1** |
| 2 | `locker room (3)` | XR Simple Interactable | `Select Entered` | 4 → 5 | partido, use `Static Parameters` | **2** |
| 3 | `Maniqui` | **Validador Secuencia** | **`Al Tener Exito`** | 4 → 5 | sin partir | **3** |
| 4 | `Lavamanos` | **Validador Secuencia** | **`Al Tener Exito`** | 5 → 6 | sin partir | **4** |

**Si la cuenta de ③ no coincide con lo que ve en pantalla, pare: está en el
objeto equivocado o en la lista equivocada.** Esa cuenta es la comprobación más
barata que tiene y le ahorra el reproceso.

---

### Resumen para ir marcando

| | Objeto | Componente | Lista | N.º | Entradas antes → después |
|---|---|---|---|---|---|
| ☐ | `Youtube Logo` | XR Simple Interactable | Select Entered | **0** | 1 → 2 |
| ☐ | `Logo Simulador VR` (el marcado) | XR Simple Interactable | Select Entered | **1** | 2 → 3 |
| ☐ | `locker room (3)` | XR Simple Interactable | Select Entered | **2** | 4 → 5 |
| ☐ | `Maniqui` | **Validador Secuencia** | **Al Tener Exito** | **3** | 4 → 5 |
| ☐ | `Lavamanos` | **Validador Secuencia** | **Al Tener Exito** | **4** | 5 → 6 |

---

### Por qué los dos últimos NO van en el clic

El maniquí y el lavamanos pasan por `ValidadorSecuencia`, que distingue el
acierto del error:

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

**5.3 · Se lee desde lejos.** Camine hasta el punto más alejado de la zona y
lea el paso resaltado sin acercarse. Si no se resuelve, suba `Tamaño De Letra`:
el tablero se ensancha con la letra y la lista sigue cabiendo en un renglón por
paso. El alto real de la letra, en metros, es `Tamaño De Letra` × `Escala`, y
por debajo de medio grado de ángulo visual deja de leerse con las gafas
puestas. **Puede ajustarlo con la experiencia en marcha:** la placa se rehace
sola en cuanto cambia el número.

**5.4 · El tablero no se mueve, pero gira.** Camine de un lado a otro: el
tablero debe quedarse clavado en su sitio y girar en yaw para quedar siempre de
frente. No debe inclinarse cuando mire hacia arriba ni cuando se agache. Si el
giro le parece perezoso, suba `Giro Suavizado`; con `0` gira al instante.

**5.4b · El tablero no estorba.** Compruebe dos cosas que solo se ven dentro:
que no tapa los lockers, el maniquí ni el lavamanos desde ningún punto de paso,
y que no atraviesa el techo. Si estorba, suba la Y de `Desplazamiento Del
Ancla` o mueva `Guion_Reto1` hacia una pared libre. El tablero no tiene
collider, así que el participante lo atraviesa sin chocar: es incómodo de ver,
no un bloqueo.

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

- **El reto 2** tiene su propia guía: `BLOQUE_B2_GUION_RETO2.md`. Allí se
  encadena el panel de la planta al salto de grupo del reto 1 y se cierra el
  atajo del cuestionario que se describe más abajo.
- **El reto 3** sigue sin disparadores identificados.
- **Encadenarlos.** El campo `Al Terminar` de un guion puede llamar a
  `Mostrar()` del siguiente. En el reto 2 se resolvió por el otro lado, desde
  `Despues Del Salto` del `AvanceEnGrupo`, que es el instante en que el grupo
  pisa la zona nueva.
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

  **Resuelto en el reto 2**, por esa misma vía: se desmarca el
  `XR Simple Interactable` y se enciende en `Despues Del Salto` del
  `AvanceEnGrupo` de `Avance_Reto1`, que es cuando el grupo llega a la planta.
  El procedimiento está en el paso 3 de `BLOQUE_B2_GUION_RETO2.md`.
