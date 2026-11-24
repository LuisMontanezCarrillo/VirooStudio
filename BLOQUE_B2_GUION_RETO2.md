# Bloque B2 — El guion de pasos del reto 2

Hermano de `BLOQUE_B1_GUION_CABLEADO.md`, que cubre el reto 1. Todo lo que aquí
se afirma sobre la escena está leído de `Assets/Scenes/Escena 1.unity` y de los
scripts, no de ningún documento de planeación.

---

## Qué es el reto 2, según el proyecto

El reto 2 ocurre en la misma `Escena 1`. `Escena 2.unity` existe en el proyecto
pero está **desactivada en el build**, de modo que «Escena 2» es el nombre de la
etapa narrativa, no de una escena de Unity.

Cuando el lavamanos cierra el reto 1, `Avance_Reto1` teletransporta al grupo a
`Punto_Destino_Planta`, en **(−0,11 · 0 · −0,36)**. Alrededor de ese punto queda
todo lo del reto 2:

| Objeto | Dónde está | Para qué |
|---|---|---|
| `Pasteurizador` | (−0,18 · 0 · 0,71) | el equipo que hay que recorrer |
| `Canvas_carrusel` | (0,00 · 0 · −1,01) | cuatro láminas con locución |
| `Canvas_Cuestionario` | (0,00 · 0 · −2,78) | las cinco preguntas |
| `Logo proyecto 3D (Esc2)` | (−4,04 · 1,19 · −1,32) | el clic que abre el cuestionario |

La pantalla de bienvenida de `GestorCuestionario` lo dice con todas las letras:
«*Antes de iniciar el cuestionario, se recomienda explorar detalladamente el
carrusel interactivo y después el pasteurizador 3D, ambos ubicados en la planta
piloto*». De ahí salen los cuatro pasos del guion, y no de una lista inventada.
Esa frase se reescribió el 24 de noviembre de 2026 para que el orden que anuncia
sea el mismo que muestra el tablero; antes nombraba primero el pasteurizador.

Luego se pulsa «Comenzar Cuestionario» y se responden **cinco preguntas** de
opción A/B/C. Fallar no penaliza: el feedback dice «*Analiza el equipo e intenta
de nuevo*» y se puede reintentar. Acertadas las cinco, `OnTransitionComplete`
enciende `Canvas_Esc3` y `Avance_Reto2` salta al reto 3.

---

## Lo que hubo que añadir antes, y por qué

En el reto 1 cada paso tenía ya su disparador: un `Select Entered` o un
`Al Tener Exito`. En el reto 2 no era así, y dos de los cuatro pasos no tenían
ningún evento que se pudiera cablear desde el Inspector. Se añadieron.

**`Controlador Carrusel` → `Al Llegar A La Ultima`.** Se dispara la primera vez
que se llega a la cuarta lámina. Se mide por haber llegado al final y no por
haber pulsado cuatro veces, porque el estudiante puede ir y volver. Avisa una
sola vez por tanda.

**`Pasteurizer Hover Handler` → `Al Explorar Lo Suficiente`**, con un campo
`Partes Para Dar Por Explorado` que de serie vale **3**. Se cuentan partes
**distintas**, no clics: pulsar diez veces el mismo intercambiador no es haber
recorrido el equipo, y marcarle el paso por insistir le diría que ya vio lo que
no ha visto. Con el valor en 1 equivale al `On Part Pinned` que ya existía.

El mismo componente pasa a implementar `IReiniciable`, de modo que entre una
tanda y la siguiente suelta la parte fijada y olvida el recorrido. Sin eso, una
pieza teñida sobreviviría a la tanda y el estudiante nuevo la encontraría
marcada sin saber por qué.

**`Guion De Pasos` → `Empezar Oculto`.** El panel del reto 1 se enciende solo
porque su reto empieza cuando empieza la experiencia. El del reto 2 no: si se
encendiera solo, el participante vería en el minuto cero la lista de una zona en
la que todavía no está. Con la casilla marcada, el panel espera a que algo llame
a `Mostrar()`.

---

## Paso 1 — Crear el objeto

En la Hierarchy, clic derecho sobre **`Root`** → `Create Empty`. Nómbrelo
**`Guion_Reto2`**.

En el Transform ponga de partida **X = 2,0 · Y = 0 · Z = −1,0**, y **deje la
Scale en 1 · 1 · 1**. La Y va en 0 porque la altura la pone
`Desplazamiento Del Ancla`, que de serie sube el centro del tablero a 1,80 m.

> **Este es el error que ya costó una vuelta en el reto 1:** el valor de la
> escala del tablero **no va en el Scale del Transform**. Va en el campo
> `Escala` del componente, más abajo.

**Colóquelo con el gizmo, no con mis números.** Con `Guion_Reto2` seleccionado,
la vista de escena dibuja el tablero a tamaño real: rectángulo naranja, el poste
desde el objeto hasta el centro del tablero, y una rayita verde que mide una
letra a tamaño real. Muévalo hasta que no tape el pasteurizador ni el carrusel
y no quede dentro de una pared.

> **Por qué aquí no hay un buen centro.** La zona del reto 2 se estira unos 4 m
> en X, porque el logo que abre el cuestionario está lejos del equipo. Ningún
> punto queda cómodo para los cuatro sitios a la vez: desde el logo, a unos 6 m,
> una letra de 7,9 cm cae a 0,75°, que es el límite. Si al probar no se lee desde
> allí, suba `Tamaño De Letra` a 52.

---

## Paso 2 — Añadir el componente y llenarlo

Con `Guion_Reto2` seleccionado, `Add Component` → busque **`Guion de pasos`**.

**Contenido**

| Campo | Valor |
|---|---|
| `Titulo` | `PLANTA PILOTO` |
| `Pasos` → Size | **4** |

| N.º | `Texto` | `Sangrado` |
|---|---|---|
| 0 | `Revisa el carrusel hasta la última lámina` | sin marcar |
| 1 | `Recorre el pasteurizador y consulta sus partes` | sin marcar |
| 2 | `Abre el cuestionario en el logo del proyecto` | sin marcar |
| 3 | `Responde las cinco preguntas` | sin marcar |

**Colocación**

| Campo | Valor |
|---|---|
| `Colocacion` | `Fijo Mirando Al Usuario` |
| `Escala` | **0.0018** |
| `Ancla` | vacío (usa el propio objeto) |
| `Desplazamiento Del Ancla` | 0 · 1.8 · 0 |
| `Giro Suavizado` | 6 |

**Letra**

| Campo | Valor |
|---|---|
| `Tamaño De Letra` | **44** |

**Al empezar**

| Campo | Valor |
|---|---|
| `Empezar Oculto` | **marcado** |

Esa casilla es la diferencia de fondo con el reto 1. Si se queda sin marcar, el
tablero del reto 2 aparece desde el principio de la experiencia, en la zona de
alistamiento, junto al del reto 1.

**Al terminar**: deje `Ocultar Al Terminar` marcado y
`Segundos Antes De Ocultar` en **2**. El último paso se marca cuando el
cuestionario termina, y segundos después el grupo salta al reto 3: con los
cuatro segundos de serie, el panel se iría con ellos.

`Ctrl+S`.

---

## Paso 3 — Encenderlo al llegar a la planta, y cerrar el atajo

Un paso que no existe en el reto 1 y sin el cual no se ve nada. Aquí se añaden
**dos** filas a la misma lista.

> **Por qué la segunda fila.** `Logo proyecto 3D (Esc2)` arranca con su
> `XR Simple Interactable` **marcado**, de modo que hoy se puede pulsar en
> cualquier momento de la sesión, incluido antes de empezar el reto 1, y abre el
> cuestionario. Al cablearle `Cumplir (2)` eso deja de ser un detalle: quien lo
> pulse durante el reto 1 marcaría un paso del reto 2 **y encendería su
> tablero**, porque `Cumplir` enciende el panel. La solución es la que ya estaba
> anotada en la guía del reto 1: dejar el interactable apagado y encenderlo al
> llegar a la planta.

### ⓪ Antes de empezar: apagar el logo

En la lupa escriba `Logo proyecto`. Seleccione **`Logo proyecto 3D (Esc2)`**,
que es el padre, y en el Inspector **desmarque la casilla a la izquierda del
nombre `XR Simple Interactable`**. No toque el hijo
`Logo Simulador VR (Missing Prefab…)`, que ya está desmarcado y es del reto 1.

Hecho esto, el cuestionario no se puede abrir hasta que el grupo llegue a la
planta.

### ① Encontrar el objeto

En la lupa de la Hierarchy escriba `Avance_Reto1`. Su ruta es `Root /
Avance_Reto1`.

### ② Abrir la lista correcta

Este objeto tiene **dos** componentes con listas de eventos: `Avance En Grupo` y
`Reunion En El Centro`. El que busca es **`Avance En Grupo`**, y dentro, la
lista **`Despues Del Salto`**. **No es `Antes Del Salto`**, que tiene una sola
entrada y oscurece la pantalla.

### ③ Comprobar que está en el sitio correcto

`Despues Del Salto` debe tener **exactamente 5 entradas**, en este orden:

```
  1   Canvas_Esc1 . SetActive (bool)
  2   Canvas _Esc2 . SetActive (bool)
  3   Bisagra_Puerta (ControladorPuerta) . CerrarInstante ()
  4   RoomTone_PlantaPiloto (AudioSource) . Play ()
  5   Fundido (FundidoDePantalla) . AclararAhora ()
```

Si no coincide, está en otro componente o en otra lista.

### ④ Añadir la fila

El **`+`** de `Despues Del Salto`. **No toque el `Runtime Only`.**

### ⑤ Arrastrar el objeto

Borre la lupa y arrastre **`Guion_Reto2`** al hueco `None (Object)`.

### ⑥ Elegir el método

`Despues Del Salto` es un `UnityEvent` sin argumento, así que **el menú no viene
partido**: verá los componentes directamente. Elija `GuionDePasos` →
**`Mostrar ()`**.

**Aquí no es `Cumplir`.** Este evento no marca ningún paso: enciende el panel.
Si elige `Cumplir (int)` aparecerá una casilla numérica; si aparece, se
equivocó.

### ⑦ No hay número

`Mostrar ()` no recibe nada. La fila queda sin casilla.

### ⑧ Cómo queda

```
  [ Runtime Only  v ]  [ GuionDePasos.Mostrar  v ]
  [ Guion_Reto2 (GuionDePasos) ]
```

`Despues Del Salto` debe tener ahora **6 entradas**.

### ⑨ La segunda fila: encender el logo

Sin salir de `Despues Del Salto`, pulse el **`+`** otra vez.

Esta vez arrastre al hueco **`Logo proyecto 3D (Esc2)`**, el padre. En el
desplegable, menú sin partir, elija **`XRSimpleInteractable`** →
**`enabled (bool)`**, y **marque la casilla** que aparece debajo.

```
  [ Runtime Only  v ]  [ XRSimpleInteractable.enabled  v ]
  [ Logo proyecto 3D (Esc2) (XRSimpleInteractable) ]
  [x]  <-- MARCADA
```

> **Ojo con el desplegable aquí.** El objeto que arrastró tiene dos
> `XRSimpleInteractable` en juego: el suyo y el del hijo. El menú solo lista los
> componentes del objeto arrastrado, así que si arrastró el padre, el que sale es
> el correcto. Y **la casilla tiene que quedar marcada**: desmarcada haría lo
> contrario de lo que busca.

`Despues Del Salto` queda en **7 entradas**.

### ⑩ Guardar

`Ctrl+S`.

---

## Paso 4 — Los cuatro disparadores, uno por uno

Mismo gesto que en el reto 1. Lo que cambia es el objeto, el componente, la
lista y el número.

---

### N.º 0 · `Canvas_carrusel` → número **0**

#### ① Encontrar el objeto

En la lupa escriba `Canvas_carrusel`. Su ruta es
`Root / Planta_Piloto / Canvas_carrusel`.

#### ② Abrir la lista correcta

Baje hasta **`Controlador Carrusel`** y busque el apartado
`Aviso de recorrido completo`, con la lista **`Al Llegar A La Ultima`**.

Compruebe de paso que `Diapositivas` tiene **4 elementos** y `Audios Voz`
también **4**, con el primero vacío: así está hoy, y la primera lámina sale en
silencio a propósito.

#### ③ Comprobar que está en el sitio correcto

`Al Llegar A La Ultima` debe estar **vacía**. Es un campo nuevo.

#### ④–⑤ Añadir la fila y arrastrar

El **`+`**, borrar la lupa, arrastrar **`Guion_Reto2`**.

#### ⑥ Elegir el método

Menú sin partir: `GuionDePasos` → **`Cumplir (int)`**.

#### ⑦ Escribir el número

Déjelo en **`0`**, que es el valor por omisión. Compruébelo igual.

#### ⑧ Cómo queda

`Al Llegar A La Ultima`: **1 entrada**, con un `0` en la casilla.

#### ⑨ Guardar

`Ctrl+S`.

---

### N.º 1 · `Pasteurizador` → número **1**

#### ① Encontrar el objeto

En la lupa escriba `Pasteurizador`. Su ruta es
`Root / Planta_Piloto / Pasteurizador`. Es una instancia del prefab
`Assets/Prefabs/Pasteurizador_HTST/Pasteurizador_HTST.prefab`, renombrada en la
escena; el componente está en **la raíz del prefab**, no en una de sus piezas.

#### ② Abrir la lista correcta

En el Inspector baje hasta **`Pasteurizer Hover Handler`**. Tiene dos apartados
de eventos:

```
  Events
     On Hover Enter
     On Hover Exit
     On Part Pinned
     On Pin Cleared
  Aviso de recorrido suficiente
     Partes Para Dar Por Explorado   [ 3 ]
     Al Explorar Lo Suficiente       <-- ESTA
```

Trabaje en **`Al Explorar Lo Suficiente`**. **No use `On Part Pinned`**: se
dispara con cada pieza, de modo que el paso se marcaría con la primera.

#### ③ Comprobar que está en el sitio correcto

`Al Explorar Lo Suficiente` debe estar **vacía**: `List is Empty`. Es un campo
nuevo y nadie lo ha cableado todavía. Y justo encima debe verse
`Partes Para Dar Por Explorado` con un **3**.

Si ese campo no aparece, Unity no recompiló: vuelva al editor, espere a que
termine la barra de progreso y mire la Console.

#### ④ Añadir la fila

El **`+`** de `Al Explorar Lo Suficiente`.

#### ⑤ Arrastrar el objeto

Borre la lupa y arrastre **`Guion_Reto2`** al hueco `None (Object)`.

#### ⑥ Elegir el método

Menú **sin partir**: `GuionDePasos` → **`Cumplir (int)`**. Señal de acierto:
aparece la casilla numérica.

#### ⑦ Escribir el número

Escriba **`1`**. El `0` que sale por omisión hay que cambiarlo.

#### ⑧ Cómo queda

```
  [ Runtime Only  v ]  [ GuionDePasos.Cumplir  v ]
  [ Guion_Reto2 (GuionDePasos) ]
  [ 1 ]
```

`Al Explorar Lo Suficiente`: **1 entrada**.

#### ⑨ Guardar

`Ctrl+S`.

---

### N.º 2 · `Logo proyecto 3D (Esc2)` → número **2**

#### ① Encontrar el objeto

En la lupa escriba `Logo proyecto`. Salen **dos** cosas que importan y hay que
quedarse con la de arriba:

| Lo que sale | Qué es | ¿Lo cablea? |
|---|---|---|
| `Logo proyecto 3D (Esc2)` | el padre, el que abre el cuestionario | **sí** |
| `Logo Simulador VR (Missing Prefab…)` | su hijo, el del reto 1 **desactivado** | no |

El hijo tiene su `XR Simple Interactable` con la **casilla desmarcada**: es el
gemelo del que usted cableó en el reto 1. Si acaba ahí, lo delata que su
`Select Entered` tiene dos entradas y no una.

**Desde el paso 3, el padre también tiene su casilla desmarcada**, porque la
apagó usted en ⓪ y la enciende el salto a la planta. Eso no impide cablearlo: la
casilla apaga el componente en ejecución, no su lista de eventos.

#### ② Abrir la lista correcta

En `Logo proyecto 3D (Esc2)`, componente **`XR Simple Interactable`**, abra
`Interactable Events` → **`Select Entered`**.

#### ③ Comprobar que está en el objeto correcto

`Select Entered` debe tener **exactamente 1 entrada**:

```
  1   Canvas_Cuestionario . SetActive (bool)   con la casilla MARCADA
```

Una sola entrada, y es la que enciende el cuestionario. Si ve dos, está en el
hijo.

#### ④–⑤ Añadir la fila y arrastrar

El **`+`**, borrar la lupa, arrastrar **`Guion_Reto2`**.

#### ⑥ Elegir el método

Aquí el menú **sí viene partido en dos bloques**, porque `Select Entered`
entrega un `SelectEnterEventArgs`:

```
  Dynamic SelectEnterEventArgs
     ...                      <-- aquí NO está Cumplir
  ─────────────────────────────
  Static Parameters
     GuionDePasos
        Cumplir (int)         <-- ESTE
```

#### ⑦ Escribir el número

Escriba **`2`**.

#### ⑧ Cómo queda

`Select Entered`: **2 entradas**, la segunda con un `2`.

#### ⑨ Guardar

`Ctrl+S`.

---

### N.º 3 · `Canvas_Cuestionario` → número **3**

#### ① Encontrar el objeto

En la lupa escriba `Canvas_Cuestionario`. Su ruta es
`Root / Planta_Piloto / Canvas_Cuestionario`. **Aparece desactivado**, con el
nombre en gris: es normal, lo enciende el clic del N.º 2. Se cablea igual.

#### ② Abrir la lista correcta

Baje hasta **`Gestor Cuestionario`** y busque, al final, el apartado
`Eventos de Red (VIROO)` con la lista **`On Transition Complete`**.

#### ③ Comprobar que está en el sitio correcto

`On Transition Complete` debe tener **exactamente 1 entrada**:

```
  1   Canvas_Esc3 . SetActive (bool)   con la casilla MARCADA
```

#### ④–⑤ Añadir la fila y arrastrar

El **`+`**, borrar la lupa, arrastrar **`Guion_Reto2`**.

#### ⑥ Elegir el método

Menú sin partir: `GuionDePasos` → **`Cumplir (int)`**.

#### ⑦ Escribir el número

Escriba **`3`**.

#### ⑧ Cómo queda

`On Transition Complete`: **2 entradas**, la segunda con un `3`.

#### ⑨ Guardar

`Ctrl+S`.

---

## Resumen para ir marcando

| | Objeto | Componente | Lista | N.º | Entradas antes → después | Menú ⑥ |
|---|---|---|---|---|---|---|
| ☐ | `Logo proyecto 3D (Esc2)` | XR Simple Interactable | — | **desmarcar la casilla** | — | — |
| ☐ | `Avance_Reto1` | Avance En Grupo | `Despues Del Salto` | — `Mostrar()` | 5 → 6 | sin partir |
| ☐ | `Avance_Reto1` | Avance En Grupo | `Despues Del Salto` | — `enabled(true)` del logo | 6 → 7 | sin partir |
| ☐ | `Canvas_carrusel` | Controlador Carrusel | `Al Llegar A La Ultima` | **0** | 0 → 1 | sin partir |
| ☐ | `Pasteurizador` | Pasteurizer Hover Handler | `Al Explorar Lo Suficiente` | **1** | 0 → 1 | sin partir |
| ☐ | `Logo proyecto 3D (Esc2)` | XR Simple Interactable | `Select Entered` | **2** | 1 → 2 | **partido** |
| ☐ | `Canvas_Cuestionario` | Gestor Cuestionario | `On Transition Complete` | **3** | 1 → 2 | sin partir |

**Si la cuenta de ③ no coincide con lo que ve en pantalla, pare: está en el
objeto equivocado o en la lista equivocada.**

---

## Paso 5 — Comprobar en Play, antes del laboratorio

**5.1 · El panel no aparece al principio.** Dé Play en la zona de alistamiento:
debe verse **solo** el tablero del reto 1. Si ve dos, `Empezar Oculto` quedó sin
marcar.

**5.2 · Aparece al llegar.** Complete el reto 1. Tras el salto a la planta, el
tablero del reto 2 debe estar ahí, con los cuatro pasos y el primero resaltado.

**5.3 · Se lee desde los cuatro sitios.** Léalo desde el pasteurizador, desde el
carrusel, desde el cuestionario y desde el logo. El logo es el más lejano, a
unos 6 m. Si desde allí no se resuelve, suba `Tamaño De Letra`; puede hacerlo en
Play, la placa se rehace sola.

**5.4 · No tapa nada.** Compruebe que el tablero no se interpone entre el
estudiante y el pasteurizador ni el carrusel desde ningún punto de paso, y que
no atraviesa pared ni techo.

**5.5 · El pasteurizador marca a la tercera.** Fije una pieza: el paso **no**
debe marcarse. Fije una segunda: tampoco. Fije una tercera distinta: ahí sí. En
la Console:

```
[GuionDePasos] Paso 1 cumplido: Recorre el pasteurizador y consulta sus partes
```

Si se marca con la primera, `Partes Para Dar Por Explorado` quedó en 1.

**5.6 · El carrusel marca al final.** Pulse `Siguiente` hasta la cuarta lámina.
El paso 0 debe marcarse ahí, no antes. Vuelva atrás y avance otra vez: **no**
debe volver a avisar.

**5.6b · El logo no se puede pulsar antes.** Durante el reto 1, apunte al logo
del proyecto en la planta y dispare: **no debe pasar nada**. Ni se abre el
cuestionario, ni se marca ningún paso, ni aparece el tablero del reto 2. Si pasa
algo, la casilla del paso ⓪ quedó marcada.

**5.7 · El logo marca y abre.** Ya en la planta, un clic debe encender
`Canvas_Cuestionario` y marcar el paso 2 a la vez.

**5.8 · El cuestionario marca al superarlo.** Responda las cinco. Al terminar
debe marcarse el paso 3, encenderse `Canvas_Esc3` y saltar al reto 3. Falle a
propósito una pregunta antes: el fallo **no** debe marcar nada.

**5.9 · El reinicio.** Dispare el reinicio de la experiencia. El tablero del
reto 2 debe **apagarse** y volver a los cuatro pasos sin marcar, y el
pasteurizador debe quedar sin pieza teñida. No hay que cablear nada: los tres
componentes implementan `IReiniciable` y `ReinicioDeExperiencia` los encuentra
solo, incluso desactivados.

---

## Lo que queda después

- La lista de pasos del **reto 3**, que todavía no tiene disparadores
  identificados.
- `Avance_Reto2` no lo invoca nadie desde un evento: hoy el salto al reto 3 lo
  encadena `GestorCuestionario` por código. Conviene confirmarlo en Play.
- Los avisos de TextMeshPro sobre `enableWordWrapping`, que son obsolescencia y
  no error.
