/// <summary>
/// Lo implementa todo componente que guarda estado de avance de la experiencia y
/// que, por tanto, tiene que saber volver al principio.
///
/// POR QUE EXISTE
/// --------------
/// El 29 de septiembre pasan cuarenta estudiantes en tandas de dos: veinte
/// pasadas completas en una jornada. Hoy la unica forma de dejar la experiencia
/// lista para la tanda siguiente es cerrar la aplicacion y volver a lanzarla
/// desde el Room Player, porque en el proyecto no hay ninguna recarga de escena
/// en tiempo de ejecucion: todas las llamadas a SceneManager estan en scripts de
/// editor. Veinte relanzamientos, cada uno con su carga de escena, su
/// emparejamiento de visores y su calibracion, no caben en el dia.
///
/// La alternativa es devolver el estado en sitio. Recargar la escena dentro de
/// una sesion de VIROO volveria a disparar el arranque de red, asi que cada
/// componente se reinicia a si mismo y ReinicioDeExperiencia los orquesta.
///
/// REGLA PARA QUIEN LO IMPLEMENTE
/// ------------------------------
/// ReiniciarAlInicio() deja el componente como estaba al abrir la experiencia,
/// y NO reproduce nada: se ejecuta con la pantalla en negro y con el grupo aun
/// sin colocar. Lo que deba sonar o verse al llegar a cada reto se dispara
/// cuando se llegue a ese reto, no aqui.
/// </summary>
public interface IReiniciable
{
    void ReiniciarAlInicio();
}
