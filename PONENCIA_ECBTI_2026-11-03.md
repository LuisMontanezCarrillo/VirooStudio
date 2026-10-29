# Ponencia · II Encuentro de Grupos y Semilleros de Investigación ECBTI

**La Realidad Virtual como herramienta para potenciar el componente práctico simulado**
Prototipo de un simulador de Realidad Virtual para la apropiación del proceso de
pasteurización de alimentos líquidos · Proyecto PIZCBC292025

Martes 3 de noviembre de 2026 · 9:50 a.m. · Auditorio primer piso, Sede Nacional
José Celestino Mutis · Transmisión en vivo · **10 minutos**

---

## Instrucciones para quien diagrame (Cowork)

**Identidad gráfica.** Se respeta la plantilla institucional
`plantilla_presentaciones_sede_nacional.pptx` sin alterarla. Datos extraídos del
tema de esa plantilla:

| | |
|---|---|
| Lienzo | 13,33 × 7,5 pulgadas (16:9) |
| Tipografía de títulos | Arial |
| Tipografía de cuerpo | Helvetica |
| Azul institucional | `#005681` |
| Azul secundario | `#00739B` |
| Amarillo | `#F4BE00` |
| Naranja | `#EE7203` |
| Morado | `#74189B` |
| Grises | `#D5D5D5` · `#A7A7A7` · `#535353` |

**Principio de diseño.** Más imagen que texto. Ninguna lámina debe pasar de
unas 25 palabras en pantalla. Lo que está bajo "Narración" **no va en la
lámina**: es lo que dice el ponente.

**Legibilidad.** El evento se transmite en vivo. Cuerpo mínimo 24 pt, y las
cifras que importan en gran tamaño. Es coherente con un hallazgo del propio
proyecto: la legibilidad fue uno de los problemas reportados en la validación.

**Nota sobre el tiempo.** Los minutos indicados por lámina suman exactamente 10.
Las láminas 4, 5 y 6 llevan **un minuto de video cada una**, tres minutos en
total, que es justo el bloque asignado al prototipo. No queda margen: lo que el
ponente diga sobre esas láminas tiene que caber encima del video, no después.

**Los videos ya están editados y listos**, en
`C:\Users\MSI\Videos\Captures\Ponencia ECBTI 2026-11-03\`

| Archivo | Contenido |
|---|---|
| `Reto 1 - Zona de alistamiento (1 min).mp4` | del 0:00 al 1:00 del original |
| `Reto 2 - Reconocimiento del proceso (1 min).mp4` | el último minuto del original |
| `Reto 3 - Simulacion del proceso (1 min).mp4` | del 0:40 al 1:40 del original, con la subida de 20 a 72 °C |

Se les quitó la interfaz del editor de Unity que rodeaba la grabación original
(`crop=1640:812:180:116`). Quedan a **1640 × 812**, relación 2,02:1, más
panorámicos que el 16:9 de la lámina: al colocarlos a todo lo ancho dejarán una
franja arriba y abajo. Aprovecharla para el rótulo en vez de estirar la imagen.

---

## Lámina 1 · Portada

**Tiempo:** 0:00 (se muestra mientras presentan al ponente)

**Visual:** `imagenes para laminas/L1 - pasteurizador completo.png` a sangre
completa. Render limpio del pasteurizador de placas con los dos tanques, el
banco de válvulas azules y el tablero. Lee de inmediato como planta de
alimentos. Degradado oscuro en la parte inferior para que el texto se lea
encima.

**En pantalla:**

> **La Realidad Virtual como herramienta para potenciar el componente práctico simulado**
> Prototipo de simulador de RV para la apropiación del proceso de pasteurización de alimentos líquidos
>
> Luis Arcesio Montañez Carrillo · Investigador principal
> Clara Sánchez · Cesar David Monroy · Coinvestigadores
>
> Proyecto PIZCBC292025 · Convocatoria N.º 13 de 2024
> ECBTI · Grupos BYTE IN DESIGN y Etno-farmacología, Productos Naturales y Alimentos
> Semilleros ARCADE y VITAL ZCBOY · Aliado: Universidad de La Salle

**Nota:** las fotografías de participantes reales ya están disponibles y se
usan en las láminas 3, 7 y 9.

---

## Lámina 2 · El problema

**Tiempo:** 1 minuto

**Visual dominante:** Un mapa de Colombia muy esquemático, con puntos dispersos
que representan los centros de la UNAD, y uno solo de esos puntos marcado con el
icono de un laboratorio. La imagen debe decir por sí sola: muchos sitios, un
laboratorio.

**En pantalla (tres cifras grandes, nada más):**

> **1** · única IES del país que ofrece Ingeniería de Alimentos con metodología virtual
>
> **USD 250.000 a 1.000.000** · costo de instalar un laboratorio de pasteurización
>
> **600.000.000** · personas afectadas al año por enfermedades transmitidas por alimentos

**Narración:**

> La UNAD es la única institución del país que ofrece Ingeniería de Alimentos con
> metodología virtual. Dieciocho universidades lo ofrecen de forma presencial.
>
> Pero es un programa con componente práctico obligatorio, y montar un
> laboratorio de pasteurización cuesta entre doscientos cincuenta mil y un millón
> de dólares. En los centros con baja matrícula esa inversión no se justifica, y
> el estudiante que está lejos no se puede desplazar.
>
> Mientras tanto, las enfermedades transmitidas por alimentos afectan a
> seiscientos millones de personas cada año. La pasteurización es exactamente la
> competencia que previene eso.

**Fuentes:** ACOFI 2019 · NMPF 2022 · OMS 2020, todas citadas en la ficha del
proyecto.

---

## Lámina 3 · La propuesta

**Tiempo:** 1 minuto

**Visual dominante:** `imagenes para laminas/L3 - dos participantes en la sala.jpeg`,
fotografía real de dos participantes con visor dentro de la sala VIROO, con las
marcas del piso visibles. Es vertical (1200 × 1600), así que va a media lámina;
en la otra mitad, las medidas de la sala y los tres iconos numerados que
anticipan los retos.

Esta foto hace el trabajo que ningún render puede hacer: son personas de pie
en una sala, con visor y mandos, no alguien sentado ante una pantalla.

**En pantalla:**

> **Objetivo.** Desarrollar un prototipo de simulador de RV que facilite la
> comprensión del proceso de pasteurización de alimentos líquidos.
>
> **1** Alistamiento  ·  **2** Reconocimiento  ·  **3** Operación
>
> Hasta 4 participantes simultáneos · mapeo físico 1:1 · desplazamiento caminando

**Narración:**

> El objetivo del proyecto es desarrollar un prototipo de simulador de realidad
> virtual que facilite la comprensión del proceso de pasteurización.
>
> ¿Por qué realidad virtual y no un software de escritorio? Porque la decisión no
> fue tecnológica sino pedagógica. La experiencia ocurre en una sala con mapeo
> físico uno a uno: hasta cuatro participantes entran a la vez y se desplazan
> caminando de verdad, no con un joystick. Eso permite percibir la escala real de
> los equipos, que es justamente lo que una imagen o una definición no transmiten.
>
> Y está organizada en tres retos encadenados, en el orden en que se entra a una
> planta de verdad.

---

## Lámina 4 · El prototipo · Reto 1

**Tiempo:** 1 minuto de video

**Visual:** Video a pantalla completa:
`Reto 1 - Zona de alistamiento (1 min).mp4` · ya editado

**Fotograma de portada del video:** `imagenes para laminas/L4 - zona de alistamiento con maniqui.png`,
render limpio con el maniquí de dotación completa, el lavamanos, el botiquín, el
lavador de botas y los casilleros.

**En pantalla (franja inferior discreta, sobre el video):**

> **Reto 1 · Zona de alistamiento**
> Resolución 2674 de 2013 · INVIMA

**Narración (breve, sobre el video):**

> El primer reto no es la máquina. Es la puerta.
>
> Guardar los objetos personales, equipar la dotación de bioseguridad e
> higienizar manos y botas, en ese orden, según la Resolución 2674 del INVIMA.
> Sin completarlo no se entra al área de proceso. Más adelante veremos por qué
> esta decisión resultó ser la más importante del diseño.


---

## Lámina 5 · El prototipo · Reto 2

**Tiempo:** 1 minuto de video

**Visual:** Video a pantalla completa:
`Reto 2 - Reconocimiento del proceso (1 min).mp4` · ya editado

**En pantalla:**

> **Reto 2 · Reconocimiento del proceso**
> HTST 72 °C / 15 s · LTLT 63 °C / 30 min · UHT 135 °C / 2 s

**Narración:**

> El segundo reto es conceptual. Un carrusel explica los fundamentos, los tres
> métodos de pasteurización y el flujo de la línea de producción, y después un
> cuestionario de cinco preguntas verifica la comprensión antes de dejar operar.
>
> El error no castiga: devuelve al estudiante a intentarlo. Lo que importa no es
> el puntaje, es que nadie llegue a la máquina sin entender qué va a pasar.


---

## Lámina 6 · El prototipo · Reto 3

**Tiempo:** 1 minuto de video

**Visual:** Video a pantalla completa:
`Reto 3 - Simulacion del proceso (1 min).mp4` · ya editado

Corresponde al tramo 0:40 a 1:40 del original, no al primer minuto. Se eligió
así porque es donde ocurre el ciclo térmico: el marcador pasa de 20,0 °C a
34,2 °C y llega a 72,0 °C. El primer minuto del original se queda en 20,0 °C y
dejaría sin imagen justo lo que dice la narración.

**Fotograma de portada del video:** `imagenes para laminas/L6 - tablero a 72 grados.png`,
captura limpia del HMI: 72,0 °C, circuito en rojo, estado NORMAL y las métricas
de lote (26,8 L procesados, 99,6 % de rendimiento).

**En pantalla:**

> **Reto 3 · Operación y evaluación**
> Ciclo térmico completo · válvula de desviación

**Narración:**

> El tercer reto es la operación. El estudiante arranca el equipo y observa el
> ciclo térmico completo: el ascenso de temperatura, los quince segundos de
> retención y el enfriamiento rápido.
>
> Y cierra con una pregunta de planta: si la temperatura cayera a sesenta y cinco
> grados en el tubo de retención, ¿qué debería hacer el sistema? La respuesta es
> la válvula de desviación, que devuelve el producto al tanque de balance. Es el
> mecanismo que impide que leche sin pasteurizar llegue al consumidor.


---

## Lámina 7 · Avances · la validación

**Tiempo:** 1 minuto

**Visual dominante:** A la izquierda, el gráfico de barras pareadas, antes y
después, para las dos preguntas que se movieron: azul institucional `#005681`
el "antes", amarillo `#F4BE00` el "después", con la fracción sobre cada par.
A la derecha, `imagenes para laminas/L7 - estudiante operando en la sala.png`,
una estudiante con dotación completa y visor operando el simulador. Los números
y la gente, juntos en la misma lámina.

**En pantalla:**

> **Validación en dos rondas · septiembre de 2026**
> 11 docentes entrevistados · 11 estudiantes, encuesta previa y posterior
>
> | | antes | después |
> |---|---|---|
> | Orden de las tres etapas térmicas | 4/11 | **8/11** |
> | HTST: 72 °C durante 15 s | 6/11 | **10/11** |
> | Aciertos totales | 30/44 · 68 % | **40/44 · 91 %** |

**Narración:**

> La validación se hizo en dos rondas. Primero once docentes de las cadenas de
> Alimentos, Multimedia y Audio, en entrevistas semiestructuradas de ocho
> preguntas después de usar el simulador. Dos semanas después, once estudiantes
> de Ingeniería de Alimentos, con encuesta previa y posterior.
>
> Seis de los once estudiantes nunca se habían puesto unas gafas de realidad
> virtual.
>
> En las dos preguntas que medían conocimiento técnico fino, los aciertos pasaron
> de cuatro a ocho y de seis a diez sobre once. En el total, del sesenta y ocho al
> noventa y uno por ciento.
>
> Es una muestra pequeña y un prototipo. No afirmamos más de lo que eso permite.

**Advertencia de precisión:** las cifras están contadas respuesta por respuesta
sobre los dos instrumentos. No usar las que circularon antes (5→9 y 7→10): son
incorrectas.

---

## Lámina 8 · Aprendizajes · lo que no funcionó

**Tiempo:** 1 minuto

**Visual dominante:** Dos columnas enfrentadas. A la izquierda, en gris, los tres
problemas con un icono cada uno. A la derecha, en azul institucional, la
corrección correspondiente, unidas por flechas.

**En pantalla:**

> | Lo que falló | Qué estamos haciendo |
> |---|---|
> | Se perdían: no sabían qué seguía | Indicador permanente de paso · en integración |
> | Sonido: 4,55 / 7, el ítem peor valorado | Mezclador con bus de voz priorizado · ya en la escena |
> | 7 de 11 reportaron molestia física | Sin resolver |

**Narración:**

> Y esta es la parte que más nos sirvió.
>
> El hallazgo más repetido de toda la validación no fue sobre pasteurización. Fue
> que la gente se perdía. Un estudiante lo escribió así: "más señalización para
> moverse dentro del simulador, ya que se siente perdido al no conocer para dónde
> dirigirse o qué hacer". Cinco de los once docentes pidieron lo mismo por su
> cuenta. Estamos integrando un indicador permanente que dice en qué paso va y
> qué sigue.
>
> El segundo: el sonido fue el ítem peor valorado, cuatro cincuenta y cinco sobre
> siete, y cuatro docentes sencillamente no lograron escuchar la maquinaria. Un
> docente de audio diagnosticó la causa: "los niveles estaban casi iguales,
> entonces se perdió". Ya hay un mezclador en la escena que baja la planta cuando
> habla la voz, y falta ajustarlo a oído con el visor puesto.
>
> Y el tercero, que no hemos resuelto: siete de los once estudiantes reportaron
> alguna molestia física. Seis leve, una intensa. Eso también hay que decirlo.

**Cita exacta para la lámina si se quiere destacar una:**
"Más señalización para moverse dentro del simulador, ya que se siente perdido al
no conocer para dónde dirigirse o qué hacer." · estudiante, encuesta posterior,
29 de septiembre de 2026

---

## Lámina 9 · La pregunta ética

**Tiempo:** 2 minutos · **es la lámina más importante de la ponencia**

**Visual dominante:** `imagenes para laminas/L9 - participante de perfil con el visor.jpeg`.
Dos participantes de perfil, con dotación de bioseguridad real puesta, visor y
mandos, y detrás la pantalla que muestra lo que ellos están viendo dentro de la
planta virtual.

Es vertical, así que va a un lado de la lámina y las dos frases al otro, en
tipografía grande. Nada más. Esta lámina no lleva viñetas.

**No interpretar la dotación.** Los participantes llevan bata, cofia, tapabocas
y cubre-calzado porque ese día venían de una práctica de laboratorio en la
planta piloto física. Es circunstancial, no una decisión de diseño de la
experiencia, y no debe mencionarse ni insinuarse en la narración.

**En pantalla:**

> **No reemplaza la práctica. Es lo que va antes.**
>
> *Y el modelo representa el 80 %. Hay que decir cuál es el otro 20 %.*

**Narración:**

> Nos invitaron a reflexionar sobre si estas tecnologías sustituyen o complementan
> la práctica real. Nuestra respuesta no la trajimos de entrada: salió de la
> validación.
>
> Un docente de Mecánica de Sólidos y Fluidos lo dijo así: "es la primera vez que
> el estudiante va a tener el equipo en frente. Aunque sea una simulación, va a
> saber cómo funciona el equipo y para qué sirve. Por más de que yo le muestre
> una imagen, es algo muy plano. Aquí el estudiante ve las dimensiones del
> equipo".
>
> Una docente de Toxicología explicó por qué eso importa: "los estudiantes en
> práctica con un pasteurizador van a tener miedo de mover principalmente
> válvulas cuando se trabaja con presión, temperatura". Y en el simulador, dijo,
> "no van a tener miedo, no van a tener limitación".
>
> Un tercero lo cerró: ahí el estudiante "puede cometer errores, puede reiniciar
> el sistema sin que se lleven pérdidas de lotes, de producto o de materia
> prima".
>
> Eso reduce dos riesgos reales: que el estudiante se lesione, y que dañe un
> equipo costoso por desconocimiento. Y explica por qué la experiencia empieza en
> la zona de alistamiento y no en la máquina: el protocolo de bioseguridad es
> parte de lo que hay que aprender antes de entrar.
>
> Ahora el límite, y también nos lo dijeron ellos. Un docente cifró la fidelidad
> del modelo en un ochenta por ciento, y nombró el veinte por ciento que falta:
> las características previas de la leche, y hacer más visible el tiempo de
> retención. Presentamos ese dato en vez de esconderlo. Un modelo que no declara
> lo que recorta no es un modelo honesto.
>
> Y hay un segundo límite que nos interpela directamente. Un docente nos contó que
> tuvo estudiantes sordos que no podían desplazarse al componente práctico
> presencial. Hoy nuestra experiencia depende de la voz en off. A la vez, un
> docente con astigmatismo nos dijo que el sonido fue su canal principal porque no
> alcanzaba a leer los textos.
>
> Cada canal es la única vía de acceso para alguien. Si la realidad virtual se
> justifica por ampliar el acceso, no puede construir una exclusión nueva mientras
> resuelve otra.

**Citas verificadas contra la transcripción literal**, no contra el resumen del
entrevistador. Se eliminaron muletillas y repeticiones propias del habla; no se
cambió ninguna palabra con contenido.

| Cita | Quién | Transcripción |
|---|---|---|
| "Es la primera vez que el estudiante va a tener el equipo en frente." | docente de Mecánica de Sólidos y Fluidos | Grabación entrevista 4 |
| "Yo diría que en un ochenta por ciento lo que pasa en una planta de pasteurización de leches." | el mismo | Grabación entrevista 4 |
| "Van a tener miedo de mover principalmente válvulas cuando se trabaja con presión, temperatura." | docente de Toxicología y Alérgenos | Grabación entrevista 3 |
| "No van a tener miedo, no van a tener limitación." | la misma | Grabación entrevista 3 |
| "Puede cometer errores, puede reiniciar el sistema sin que se lleven pérdidas de lotes, de producto o de materia prima." | docente de Termodinámica | Grabación entrevista 1 |
| "Si en eso pues no dejaba entrar a la planta, que es una de las realidades que vivimos en una planta de alimentos." | docente de Toxicología y Alérgenos | Grabación entrevista 3 |
| "Tuve un par de estudiantes sordos que me decían yo no puedo ir... me queda muy lejos ir al componente práctico." | Harvey Andrés Milquez, Ingeniería de Alimentos | transcripción con nombre |
| "Para mí, que yo no tengo buena vista, fue muy útil el sonido." | Gustavo Bejarano, Ingeniería Multimedia | transcripción con nombre |

Si se decide poner una sola cita grande en la lámina, la recomendada es la
primera: es la que condensa toda la tesis en diez palabras.

---

## Lámina 10 · Próximos pasos

**Tiempo:** 1 minuto

**Visual dominante:** Tres bloques horizontales, cada uno con un icono y una
línea, sobre fondo azul institucional. Al pie, el dato de cierre del proyecto.

**En pantalla:**

> **Una plataforma, no un producto terminado**
>
> **01** · El andamiaje sirve para otros procesos, no solo pasteurización
> **02** · Línea conjunta: diseño de RV para perfiles con poca experiencia digital
> **03** · Aspiración: llevarlo a los CEAD como recurso institucional
>
> El proyecto cierra en diciembre de 2026

**Narración:**

> Cierro con tres invitaciones concretas a los grupos y semilleros que están hoy
> acá.
>
> Primero: lo que construimos no es específico de la pasteurización. La sala
> multiusuario, la narración por voz, la validación de secuencias y la evaluación
> formativa son un andamiaje que sirve para cualquier práctica de proceso. No les
> ofrecemos un video, les ofrecemos una base sobre la que montar lo suyo.
>
> Segundo: el hallazgo más transferible de este proyecto no es el simulador. Es lo
> que aprendimos sobre cómo se diseña realidad virtual para alguien que nunca se
> ha puesto unas gafas. Eso da para una línea conjunta, y conecta con el foco
> estratégico de competencias digitales.
>
> Y tercero, como aspiración y no como plan, porque depende de decisiones que no
> controlamos: llevarlo a los CEAD, que es donde está el estudiante que no tiene
> laboratorio.
>
> El proyecto cierra en diciembre. Gracias.

**Verificar antes del 3 de noviembre:** la ficha fija el cierre el 08/06/2026 y
se solicitó una prórroga de seis meses hasta el 08/12/2026. No tengo constancia
de la aprobación formal del Comité. Si está aprobada, diga la fecha exacta; si
no, deje "diciembre".

---

## Anexo para los organizadores

Los tres videos completos (7:19 en total) se envían como anexo, tal como los
solicitaron. En sala solo se proyectan los recortes de las láminas 4, 5 y 6.

## Procedencia de cada dato

| Dato en la ponencia | Fuente verificada |
|---|---|
| Única IES virtual, 18 presenciales | Ficha PIZCBC292025, citando ACOFI 2019 |
| USD 250.000 a 1.000.000 | Ficha PIZCBC292025, citando NMPF 2022 |
| 600 millones de personas al año | Ficha PIZCBC292025, citando OMS 2020 |
| Objetivo general | Ficha PIZCBC292025 |
| Sala 6,95 × 13,10 m, 4 participantes, 1:1 | Repositorio VirooStudio |
| Contenido de los tres retos | Guion del simulador VR versión 2 |
| 11 docentes entrevistados | Carpeta Drive de transcripciones, 11 archivos |
| 11 estudiantes, pre y post | Instrumentos 1 y 2, 11 respuestas cada uno |
| 6 de 11 nunca habían usado RV | Instrumento 1, pregunta 3 |
| 4→8 y 6→10, 30/44 a 40/44 | Instrumentos 1 y 2, contado respuesta por respuesta |
| Sonido 4,55 / 7 | Instrumento 2, pregunta 8, media de 11 respuestas |
| 7 de 11 con molestia física | Instrumento 2, pregunta 9 |
| Fidelidad del 80 % y el 20 % faltante | Entrevista E-04, pregunta 2 |
| Citas de docentes | Transcripciones literales, carpeta Drive |

## Auditoría de veracidad

Se revisó cada afirmación de la ponencia contra su fuente. Resultado:

**Correcciones aplicadas durante la auditoría**

1. **Eran 11 estudiantes, no 40.** La cifra de 40 circulaba desde un comentario
   de código que describía un plan de jornada en tandas de dos, no lo ejecutado.
   Los dos instrumentos tienen 11 respuestas cada uno, de las mismas personas.
2. **Las cifras de aprendizaje correctas son 4→8 y 6→10**, no 5→9 y 7→10 como
   apareció en una presentación anterior. Contadas respuesta por respuesta.
3. **Las citas de docentes se verificaron contra la transcripción literal.** Las
   primeras versiones de esta ponencia citaban el resumen en estilo indirecto
   del entrevistador, que no es lo que la persona dijo.
4. **La fecha de cierre se dejó en "diciembre"**, sin día, porque no hay
   constancia de que el Comité aprobara la prórroga solicitada.
5. **Se corrigió el estado de las dos mejoras.** Decían "ya está implementado".
   El mezclador sí está en la escena; el indicador de pasos está desarrollado
   pero todavía no integrado ni probado.

**Advertencias que quedan abiertas**

- "Hasta 4 participantes simultáneos" es la capacidad de diseño. Las pruebas de
  septiembre se hicieron de a dos, y la auditoría interna del proyecto registra
  que la configuración con cuatro visores no se ha verificado. La lámina 3
  enuncia capacidad, que es correcto, pero no debe decirse que se probó con
  cuatro.
- La muestra comprometida en la ficha es de 45 participantes. Entre docentes y
  estudiantes se llevan 22. Si alguien lo pregunta, ese es el dato.
- Dos docentes de Alimentos detectaron afirmaciones imprecisas en el contenido
  del simulador: que la pasteurización elimina totalmente los microorganismos
  (hay esporas) y que no altera los nutrientes (las vitaminas sí se afectan).
  No entran en la ponencia, pero conviene tenerlas presentes por si preguntan
  por la validación de contenido.

**Interpretación retirada durante la auditoría**

- Una versión de la lámina 9 afirmaba que los participantes "se vistieron de
  verdad para entrar a una planta que no existe", leyendo la dotación de
  bioseguridad de las fotografías como una decisión pedagógica. No lo es: ese
  día venían de una práctica de laboratorio en la planta piloto física. La
  frase se eliminó. Las fotografías se usan, la interpretación no.

**Afirmaciones que NO se incluyeron, por decisión del autor**

- Que la planta piloto física de la UNAD no cuenta con un intercambiador de
  placas, dicho de forma independiente por dos docentes de la cadena. Se omite
  para no convertir la ponencia en un reclamo de dotación ante la institución
  que invita.
