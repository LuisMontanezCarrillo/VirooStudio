# Bloque A · Lo que hay que cablear y lo que hay que comprobar en la sala

Ajustes posteriores a las pruebas con estudiantes del 29 de septiembre de 2026.
Plan avalado el 14 de octubre de 2026. Ver `handshake_ajustes_tras_pruebas.md`.

El bloque A cierra lo que obligó a detener las pruebas. Son tres arreglos, y dos
de ellos **no necesitan que usted toque nada**.

---

## Resumen de en qué quedó cada cosa

| | Qué se arregló | Qué le toca a usted |
|---|---|---|
| **A1** | El aro del suelo que no se veía con las gafas | Nada |
| **A2** | El gatillo, también en el cuestionario | Nada |
| **A3** | Que la dispersión no viaje de un reto al siguiente | 10 minutos de cableado |

Todo el código compila. Se verificó con el compilador de Unity contra los
ensamblados reales del proyecto, los de VIROO incluidos.

---

## A1 · El aro del suelo

**Qué pasaba.** Con las gafas puestas se veía el cartel y no se veía el aro. O
sea que el componente sí corría: lo que fallaba era solo cómo se dibujaba el
disco.

**Por qué.** El aro era un cilindro creado en tiempo de ejecución al que se le
modificaba el material a mano para volverlo translúcido. El proyecto renderiza en
**Single Pass Instanced**, modo en el que el material tiene que saber dibujarse en
los dos ojos de una sola pasada. Un material manipulado por código es justo lo
que se ve bien en la vista mono del editor y falla en el visor. El cartel no
sufre el problema porque es un lienzo de interfaz.

**Qué se hizo.** El aro se construye ahora igual que el cartel: un lienzo en
espacio de mundo tendido en el suelo, con una textura generada por código. Una
textura son datos, no una variante de shader: no se puede eliminar al compilar y
no depende del modo estéreo.

**No hay nada que cablear.** La guía sigue creándose sola.

---

## A2 · El gatillo

**Qué pasaba.** El cuestionario seguía exigiendo el botón lateral del mando, pese
a que `GatilloComoSelect` ya estaba en la escena.

**Por qué.** Ese componente solo reasignaba `selectInput`, que es lo que escuchan
los ocho `XRSimpleInteractable` de la escena (casillero, maniquí, lavamanos,
logos, pausa). Los botones del cuestionario son interfaz de Unity y van por
`uiPressInput`, que nunca se tocaba. La escena tiene **diez** lienzos con
`TrackedDeviceGraphicRaycaster`, o sea diez sitios que dependían de esa entrada.

**Qué se hizo.** Ahora reasigna las tres entradas: `selectInput`, `activateInput`
y `uiPressInput`. Además se le quitó la ventana de 25 segundos, que dejaba sin
reasignar a quien entrara tarde a la sala, y se le añadió registro del binding
original.

**No hay nada que cablear.**

---

## A3 · Que la dispersión no se acumule

**Qué pasaba.** Esto es lo mismo que A1, visto dos retos más tarde. La cadena:

1. No se ve la marca, así que el participante no va a ella.
2. Se agota la espera y el código avisa: *"Se agotaron los N s de espera a X m de
   la marca. Se avanza igualmente."*
3. El teletransporte de VIROO conserva las posiciones relativas, de modo que esa
   desviación viaja al reto siguiente, y se suma a la del salto anterior.

**Ojo con una idea que resultó falsa.** Se sospechaba que el contenido del reto 3
estaba demasiado lejos. Medido sobre la escena, el tablero está **entre 0,72 m y
1,87 m** de los puntos de llegada. No está mal colocado. El problema era la
dispersión acumulada.

**Qué se hizo.** `ReunionEnElCentro` tiene una casilla nueva, **Solo Reunir**. Con
ella marcada, la reunión no hace saltar a nadie: solo pide a cada uno que vaya a
su marca y, al llegar, dispara **Al Terminar La Reunión**.

**Por qué no cuesta tiempo.** Si nadie viene desviado, todos están ya sobre su
marca al aterrizar y la reunión termina en el acto. Solo cuesta segundos en el
caso en que hoy la experiencia se rompe.

### Lo que hay que cablear

1. Abra `Assets/Scenes/Escena 1.unity`.
2. Localice el objeto que lleva el **`AvanceEnGrupo` del reto 2**, el que salta al
   reto 3.
3. Cree un objeto vacío hijo suyo y llámelo **`Reunion_Llegada_Reto3`**.
4. Añádale el componente **ViroLab / Multiusuario / Reunión en el centro**.
5. Rellene así:

   | Campo | Valor |
   |---|---|
   | **Marcas** | tamaño 4, y arrastre `Punto_Inicio_Escena3`, `_B`, `_C` y `_D` |
   | **Radio** | 0.8 |
   | **Solo Reunir** | **marcado** |
   | **Avance** | déjelo **vacío** |
   | **Guia Automatica** | marcado |
   | **Espera Maxima** | 20 |
   | **Mirar Hacia** | déjelo vacío: ya llegan mirando bien |

6. En el **`AvanceEnGrupo` del reto 2**, busque el evento **`Después Del Salto`**,
   pulse **+**, arrastre `Reunion_Llegada_Reto3` al hueco del objeto y elija en la
   lista **ReunionEnElCentro → Convocar ()**.

**Cómo saber que quedó bien, sin visor.** Entre en Play y fuerce el avance del
reto 2. En la consola debe aparecer `[ReunionEnElCentro]` dos veces: la del salto
y, justo después de aterrizar, la de la reunión de llegada. Al completarla debe
leerse *"Reunidos en el destino"*.

---

## Lista de comprobación en la sala

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
es la que distingue entre un problema de dibujo y uno de posición, y sin ella
volveríamos a adivinar.

### 2. El gatillo

- [ ] Casillero, maniquí y lavamanos responden **al gatillo**.
- [ ] Los botones **A, B y C del cuestionario** responden **al gatillo**.
- [ ] El botón lateral **ya no hace falta** en ningún sitio.
- [ ] Copie de la consola las líneas que empiezan por `[GatilloComoSelect]`. Dicen
      qué entrada usa de verdad el rig de VIROO, y es la primera vez que lo vamos
      a saber.

### 3. La llegada al reto 3

- [ ] La transición al reto 3 **ya no es caótica**.
- [ ] Cada participante aparece **en su sitio o cerca**, y si no, se le pide ir a
      su marca y puede llegar **sin salirse de la zona por la que se camina**.
- [ ] El tablero queda al alcance desde donde cada uno acaba.

### 4. Lo que conviene anotar aunque no sea del bloque A

- [ ] ¿Se oye la voz por encima del ruido de la planta? (Es el bloque B, y la
      peor nota del instrumento: 4,5 sobre 7.)
- [ ] ¿Cuánto tarda la transición del reto 1 al reto 2, de principio a fin?
- [ ] ¿Alguien refiere molestia, y de qué tipo?

---

## Si algo sale mal

Apunte **qué vio**, no qué cree que falla. En la sesión del 29 de septiembre el
dato que más sirvió fue uno pequeño: que el cartel se veía y el aro no. Eso solo
se sabe mirando.
