# Bloque A · Lo que hay que cablear y lo que hay que comprobar en la sala

Ajustes posteriores a las pruebas con estudiantes del 29 de septiembre de 2026.
Plan avalado el 14 de octubre de 2026. Ver `handshake_ajustes_tras_pruebas.md`.

El bloque A cierra lo que obligó a detener las pruebas. Son tres arreglos y dos
de ellos **no necesitan que usted toque nada**.

| | Qué se arregló | Qué le toca a usted |
|---|---|---|
| **A1** | El aro del suelo que no se veía con las gafas | Nada |
| **A2** | El gatillo, también en el cuestionario | Nada |
| **A3** | Que la dispersión no viaje de un reto al siguiente | La parte 2 de este documento |

Todo el código compila. Verificado con el compilador de Unity contra los
ensamblados reales del proyecto, los de VIROO incluidos.

---

# Parte 1 · Lo que ya está hecho

## A1 · El aro del suelo

**Qué pasaba.** Con las gafas puestas se veía el cartel y no se veía el aro. O
sea que el componente sí corría: lo que fallaba era cómo se dibujaba el disco.

**Por qué.** El aro era un cilindro creado en tiempo de ejecución al que se le
modificaba el material a mano para volverlo translúcido. El proyecto renderiza en
**Single Pass Instanced**, modo en el que el material tiene que saber dibujarse en
los dos ojos de una sola pasada. Un material manipulado por código es justo lo
que se ve bien en la vista mono del editor y falla en el visor. El cartel no
sufre el problema porque es un lienzo de interfaz.

**Qué se hizo.** El aro se construye ahora igual que el cartel: un lienzo en
espacio de mundo tendido en el suelo, con una textura generada por código. Una
textura son datos, no una variante de shader: no se elimina al compilar y no
depende del modo estéreo.

**No hay nada que cablear.** La guía sigue creándose sola.

## A2 · El gatillo

**Qué pasaba.** El cuestionario seguía exigiendo el botón lateral del mando, pese
a que `GatilloComoSelect` ya estaba en la escena.

**Por qué.** Ese componente solo reasignaba `selectInput`, que es lo que escuchan
los ocho `XRSimpleInteractable` de la escena (casillero, maniquí, lavamanos,
logos, pausa). Los botones del cuestionario son interfaz de Unity y van por
`uiPressInput`, que nunca se tocaba. La escena tiene **diez** lienzos con
`TrackedDeviceGraphicRaycaster`, o sea diez sitios que dependían de esa entrada.

**Qué se hizo.** Ahora reasigna las tres entradas: `selectInput`, `activateInput`
y `uiPressInput`. Se le quitó la ventana de 25 segundos, que dejaba sin reasignar
a quien entrara tarde a la sala, y se le añadió registro del binding original.

**No hay nada que cablear.**

---

# Parte 2 · A3, lo único que hay que cablear

## Qué pasaba

Esto es lo mismo que A1, visto dos retos más tarde:

1. No se ve la marca, así que el participante no va a ella.
2. Se agota la espera y el código avisa: *"Se agotaron los N s de espera a X m de
   la marca. Se avanza igualmente."*
3. El teletransporte de VIROO conserva las posiciones relativas, de modo que esa
   desviación viaja al reto siguiente y se suma a la del salto anterior.

**Una sospecha que resultó falsa.** Se pensaba que el contenido del reto 3 estaba
demasiado lejos. Medido sobre la escena, el tablero queda **entre 0,72 m y
1,87 m** de los puntos de llegada. No está mal colocado.

**Qué se hizo en el código.** `ReunionEnElCentro` tiene una casilla nueva,
**Solo Reunir**. Con ella marcada, la reunión no hace saltar a nadie: solo pide a
cada uno que vaya a su marca y, al llegar, dispara **Al Terminar La Reunión**.

**Qué va a cablear usted.** Una reunión de ese tipo en el destino del salto al
reto 3, enganchada a lo que ocurre justo después de aterrizar.

**Por qué no cuesta tiempo.** Si nadie viene desviado, cada uno está ya sobre su
marca al aterrizar y la reunión termina en el acto.

---

## Lo que hay en la escena ahora mismo

Leído del archivo de escena, no de memoria. Le sirve para reconocer el terreno
antes de tocarlo.

| Objeto en la jerarquía | Qué lleva |
|---|---|
| `Root / Avance_Reto1` | Un `ReunionEnElCentro` y un `AvanceEnGrupo`, los del paso del reto 1 al 2 |
| `Root / Avance_Reto2` | Un `ReunionEnElCentro` y un `AvanceEnGrupo`, los del paso del reto 2 al 3 |

Los cuatro puntos de llegada del reto 3 están en:

```
Root / Planta_Piloto / Punto_Inicio_Escena3
Root / Planta_Piloto / Punto_Inicio_Escena3 / Punto_Inicio_Escena3_B
Root / Planta_Piloto / Punto_Inicio_Escena3 / Punto_Inicio_Escena3_C
Root / Planta_Piloto / Punto_Inicio_Escena3 / Punto_Inicio_Escena3_D
```

Fíjese en que **B, C y D son hijos del primero**. Hay que desplegar
`Punto_Inicio_Escena3` con la flechita para verlos.

---

## El orden de las marcas importa, y no es el que parece

**Esto es lo que había que mirar antes de escribir la guía, y la versión anterior
de este documento lo tenía mal.**

`ReunionEnElCentro` reparte las marcas por orden: al primer participante le toca
el elemento 0 del array, al segundo el 1. Con dos visores solo se usan los dos
primeros. Así que el orden decide a dónde se le pide ir a cada uno.

Dónde aterriza cada participante, calculado sobre las posiciones reales de la
escena:

- En el reto 2 están sobre `Marca_Reto2_A` y `Marca_Reto2_B`, que están
  desplazadas **1,02 m a la izquierda** y **0,69 m a la derecha** del punto que
  ancla el salto.
- El salto conserva esos desplazamientos, de modo que aterrizan **1,02 m a la
  izquierda** y **0,69 m a la derecha** de `Punto_Inicio_Escena3`.
- `Punto_Inicio_Escena3_B` está 0,90 m a la izquierda y `_C` 0,90 m a la derecha.

| Participante | Aterriza a | Punto más cercano | Distancia |
|---|---|---|---|
| Primero | 1,02 m a la izquierda | **`_B`** | **0,12 m** |
| Segundo | 0,69 m a la derecha | **`_C`** | **0,21 m** |

**Por eso los dos primeros elementos del array tienen que ser `_B` y `_C`.** Cada
uno se encuentra ya prácticamente encima de su marca y la reunión termina sola.

**Y por eso el orden obvio sería un error.** Si pusiera primero
`Punto_Inicio_Escena3` y luego `_B`, al primero se le pediría caminar 1,02 m hacia
la derecha y al segundo 1,59 m hacia la izquierda: **se cruzarían**, dos personas
a ciegas dentro de sendos visores, que es exactamente lo que el diseño de marcas
separadas existe para evitar.

Una salvedad honesta: este cálculo da por supuesto que el teletransporte de VIROO
conserva el desplazamiento de cada persona respecto al punto que ancla el salto.
Es lo que dice la documentación del proyecto y lo que explica lo que usted vio,
pero no lo he podido ejecutar. La consola de la sala lo confirmará, porque imprime
a cuántos metros de su marca está cada uno.

---

## Los pasos

### Paso 0 · Preparar

1. Abra Unity y cargue **`Assets/Scenes/Escena 1.unity`**.
2. Abra la consola: menú **Window → General → Console**. Déjela visible.
3. Compruebe que no hay errores rojos de compilación. Si los hay, pare y avíseme.

### Paso 1 · Encontrar el sitio

1. En la ventana **Hierarchy**, escriba `Avance_Reto2` en el buscador de arriba.
2. Debe aparecer un objeto llamado **`Avance_Reto2`**, hijo de `Root`.
3. Haga clic en él y mire el **Inspector**. Debe ver dos componentes:
   **Reunión en el centro** y **Avance en grupo**.

> **Si no aparece**, borre el texto del buscador y despliegue `Root` a mano. El
> buscador de la jerarquía a veces no encuentra objetos desactivados.

4. Borre el texto del buscador antes de seguir, o no podrá arrastrar nada.

### Paso 2 · Crear el objeto nuevo

1. Clic derecho sobre **`Avance_Reto2`** en la jerarquía.
2. Elija **Create Empty**. Aparece un hijo llamado `GameObject`.
3. Con él seleccionado, pulse **F2** y escríbale el nombre exacto:

   ```
   Reunion_Llegada_Reto3
   ```

   Sin tildes y con guiones bajos, igual que el resto de la escena.

> **Su posición da igual.** Este componente no usa su propia transformada para
> nada: solo mira las marcas que le asigne. No hace falta que lo ponga en cero.

### Paso 3 · Añadir el componente

1. Con `Reunion_Llegada_Reto3` seleccionado, en el Inspector pulse el botón
   **Add Component**, abajo del todo.
2. Escriba **`Reuni`** en el buscador. Escriba solo eso: si teclea la palabra
   entera con tilde puede que no la encuentre.
3. Elija **Reunión en el centro**. También puede llegar por el menú
   **ViroLab → Multiusuario → Reunión en el centro**.

> **No lo añada sobre `Avance_Reto2` directamente.** Ese objeto ya tiene una
> reunión, y además el componente busca un `AvanceEnGrupo` en su mismo objeto y
> lo asignaría solo. Queremos que ese campo quede vacío, y por eso va en un hijo.

### Paso 4 · Rellenar los campos

Rellénelos en este orden. Los que no aparecen en la tabla, déjelos como vengan.

**4.1 · Marcas.** Es lo único delicado.

1. Despliegue **Marcas** con la flechita.
2. En **Size** escriba **4** y pulse Intro. Aparecen cuatro huecos, de
   `Element 0` a `Element 3`.
3. En la jerarquía, despliegue `Root` → `Planta_Piloto` → `Punto_Inicio_Escena3`
   para ver sus tres hijos.
4. Arrastre **en este orden exacto**:

   | Hueco | Qué arrastrar |
   |---|---|
   | `Element 0` | **`Punto_Inicio_Escena3_B`** |
   | `Element 1` | **`Punto_Inicio_Escena3_C`** |
   | `Element 2` | `Punto_Inicio_Escena3` (el padre) |
   | `Element 3` | `Punto_Inicio_Escena3_D` |

   El orden está explicado arriba. **Los dos primeros son los que se usan con dos
   visores**, y son `_B` y `_C`, no el padre.

> **Compruébelo:** al soltar cada uno, el hueco debe mostrar el nombre del objeto
> seguido de `(Transform)`. Si muestra `None (Transform)`, no se soltó dentro.

**4.2 · El resto.**

| Campo en el Inspector | Valor | Por qué |
|---|---|---|
| **Radio** | `0.8` | El mismo que usan las otras dos reuniones |
| **Mirar Hacia** | dejar **vacío** (`None`) | Ya llegan mirando bien: el salto no gira a nadie y en el reto 2 ya se les exigió la orientación |
| **Esperar A Que Termine** | dejar **vacío** (`None`) | No hay locución que respetar al aterrizar |
| **Espera Adicional** | `0` | |
| **Caminar Mientras Habla** | **desmarcado** | No hay locución, así que no aplica |
| **Avance** | dejar **vacío** (`None`) | Esta reunión no hace saltar a nadie. Si pone algo aquí, saltarán otra vez |
| **Espera Maxima** | `20` | Como mucho están a 0,21 m. Veinte segundos es de sobra y evita atascar la sesión |
| **Solo Reunir** | **MARCADO** | Es la casilla nueva. Sin ella el componente dará error por no tener Avance |
| **Guia Automatica** | **marcado** | Es lo que dibuja el aro y el cartel |
| **Log** | **marcado** | Déjelo encendido para la sesión en la sala |

> **Si «Solo Reunir» no aparece**, Unity no ha recompilado. Vuelva a la ventana de
> Unity y espere a que termine la barrita de abajo a la derecha.

### Paso 5 · Engancharlo al salto

1. Seleccione otra vez **`Avance_Reto2`** en la jerarquía.
2. En el Inspector busque el componente **Avance en grupo**.
3. Baje hasta la sección **Avisos para la interfaz**. Dentro verá varios eventos;
   el que buscamos es **`Despues Del Salto`**, el último de la lista.
4. Pulse el botón **+** de ese evento. Aparece una fila con `None (Object)` a la
   izquierda y un desplegable que dice `No Function` a la derecha.
5. Arrastre **`Reunion_Llegada_Reto3`** desde la jerarquía hasta el hueco que dice
   `None (Object)`.
6. Abra el desplegable **`No Function`** y elija:
   **`ReunionEnElCentro` → `Convocar ()`**.

> **Ojo con el desplegable.** Aparecerán varias secciones: `GameObject`,
> `Transform` y `ReunionEnElCentro`. Tiene que ser la de `ReunionEnElCentro`. Y
> dentro, `Convocar ()`, no `Convocar` bajo el encabezado *Static Parameters*.

7. La fila debe quedar leyéndose así:

   ```
   Runtime Only  |  Reunion_Llegada_Reto3  |  ReunionEnElCentro.Convocar
   ```

### Paso 6 · Guardar

**Ctrl+S.** Si no guarda, nada de esto llega al build.

---

## Comprobación sin visor, antes de ir al laboratorio

Esto se puede hacer usted solo, en el editor, y detecta casi cualquier error de
cableado.

1. Pulse **Play**.
2. Avance hasta el reto 2 o fuerce el avance con **Ctrl+Shift+F**.
3. Cuando el grupo salte al reto 3, mire la consola. Debe ver, **en este orden**:

   ```
   [ReunionEnElCentro] Anunciando el fin (...)          <- la reunión del reto 2
   [AvanceEnGrupo] ...                                   <- el salto
   [ReunionEnElCentro] Reunidos en el destino (...)      <- la reunión nueva
   ```

4. La tercera línea, **"Reunidos en el destino"**, es la prueba de que quedó bien.

| Lo que ve | Qué significa | Qué hacer |
|---|---|---|
| Aparece "Reunidos en el destino" | Cableado correcto | Siga al laboratorio |
| No aparece la tercera línea | El evento `Despues Del Salto` no quedó enganchado | Repita el paso 5 |
| *"Falta el AvanceEnGrupo: el grupo no avanzará"* | Se le olvidó marcar **Solo Reunir** | Vuelva al paso 4.2 |
| Salta dos veces al reto 3 | Puso algo en el campo **Avance** | Vacíelo |
| *"No hay AvanceEnGrupo asignado"* como aviso amarillo | Lo mismo: falta **Solo Reunir** | Vuelva al paso 4.2 |

**Lo que NO hace falta cablear:** el reinicio entre tandas. `ReinicioDeExperiencia`
busca por su cuenta todo lo que sepa reiniciarse, así que el componente nuevo
entra solo. No lo añada a ninguna lista.

---

## Las esperas, ya alineadas

Había **dos esperas distintas** con el mismo nombre en componentes vecinos. Cuando
usted pidió bajar la espera a 45, la bajé en el **Avance en grupo**, pero el
`ReunionEnElCentro` de al lado seguía en **60 segundos**: si alguien no encontraba
su marca, la reunión lo esperaba un minuto entero antes de rendirse. Eso era parte
de la transición larga que usted midió.

El 27 de octubre de 2026 las puse en 45 en los dos sitios. Estado actual de las
tres esperas de la escena:

| Objeto | Componente | Espera Maxima | Por qué |
|---|---|---|---|
| `Root / Avance_Reto1` | `ReunionEnElCentro` | **45 s** | alineada con su avance |
| `Root / Avance_Reto1` | `AvanceEnGrupo` | 45 s | ya estaba |
| `Root / Avance_Reto2` | `ReunionEnElCentro` | **45 s** | alineada con su avance |
| `Root / Avance_Reto2` | `AvanceEnGrupo` | 45 s | ya estaba |
| `Root / Reunion_Llegada_Reto3` | `ReunionEnElCentro` | 20 s | es una recolocación, no una puerta: nadie se queda atrás por ella |

Las dos de 45 están cambiadas en el archivo de escena. Si Unity tenía la escena
abierta sin guardar, descártela y vuelva a abrirla para que el Inspector muestre
el valor nuevo.

---

# Parte 3 · Lista de comprobación en la sala

Esto es lo único que yo no puedo hacer. **Nada del bloque A está verificado hasta
que se compruebe con las gafas puestas**, porque el defecto de A1 consistía
precisamente en verse bien en el editor y no verse en el visor.

Basta una sesión corta, con dos visores y usted en el escritorio. Deje la consola
visible.

### 1. El aro, que es lo que detuvo las pruebas

- [ ] Al terminar el reto 1, con las gafas puestas, **¿se ve el aro en el suelo?**
- [ ] ¿Cambia de color, de amarillo a verde, al pisarlo?
- [ ] ¿Se sigue viendo el cartel con los metros que faltan?

**Si el aro sigue sin verse**, apunte si el cartel sí se ve. Esa pareja de datos
distingue entre un problema de dibujo y uno de posición, y sin ella volveríamos a
adivinar.

### 2. El gatillo

- [ ] Casillero, maniquí y lavamanos responden **al gatillo**.
- [ ] Los botones **A, B y C del cuestionario** responden **al gatillo**.
- [ ] El botón lateral **ya no hace falta** en ningún sitio.
- [ ] Copie de la consola las líneas que empiezan por `[GatilloComoSelect]`. Dicen
      qué entrada usa de verdad el rig de VIROO, y es la primera vez que lo vamos
      a saber.

### 3. La llegada al reto 3

- [ ] La transición al reto 3 **ya no es caótica**.
- [ ] Copie la línea de la consola que dice a cuántos metros de su marca está cada
      uno al aterrizar. **Es el dato que confirma o desmiente el cálculo de arriba.**
- [ ] Nadie tiene que salirse de la zona por la que se camina para llegar a su
      marca.
- [ ] El tablero queda al alcance desde donde cada uno acaba.

### 4. Lo que conviene anotar aunque no sea del bloque A

- [ ] ¿Se oye la voz por encima del ruido de la planta? (Es el bloque B, y la peor
      nota del instrumento: 4,5 sobre 7.)
- [ ] ¿Cuánto tarda la transición del reto 1 al reto 2, de principio a fin?
- [ ] ¿Alguien refiere molestia, y de qué tipo?

---

## Si algo sale mal

Apunte **qué vio**, no qué cree que falla. En la sesión del 29 de septiembre el
dato que más sirvió fue uno pequeño: que el cartel se veía y el aro no. Eso solo
se sabe mirando.
