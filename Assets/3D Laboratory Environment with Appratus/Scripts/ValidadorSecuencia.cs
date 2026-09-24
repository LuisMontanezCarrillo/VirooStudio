using UnityEngine;
using UnityEngine.Events; // Vital para poder ver las listas en el Inspector

public class ValidadorSecuencia : MonoBehaviour, IReiniciable
{
    [Tooltip("¿El estudiante ya terminó el reto anterior?")]
    public bool permisoParaAvanzar = false;

    [Tooltip("Coloca aquí lo que pasa si hace el orden CORRECTO")]
    public UnityEvent AlTenerExito;

    [Tooltip("Coloca aquí lo que pasa si se equivoca de orden")]
    public UnityEvent AlTenerError;

    // Valor con el que arranca la experiencia, leído del Inspector. Hay dos
    // validadores en la escena (Maniquí y Lavamanos) y no tienen por qué empezar
    // igual, así que el reinicio devuelve lo que cada uno tuviera en vez de
    // imponer un falso a los dos.
    private bool _permisoDePartida;

    void Awake()
    {
        _permisoDePartida = permisoParaAvanzar;
    }

    // Esta función la conectaremos al clic del láser
    public void ValidarClic()
    {
        if (permisoParaAvanzar == true)
        {
            AlTenerExito.Invoke(); // Ejecuta la lista de éxito
        }
        else
        {
            AlTenerError.Invoke(); // Ejecuta la alerta
        }
    }

    // Esta función la disparará el Casillero para darle permiso al Maniquí
    public void DarPermiso()
    {
        permisoParaAvanzar = true;
    }

    /// <summary>
    /// Vuelve a cerrar el paso. Sin esto, la tanda siguiente encontraría el
    /// maniquí ya desbloqueado por la anterior y se saltaría el casillero.
    /// </summary>
    public void ReiniciarAlInicio()
    {
        permisoParaAvanzar = _permisoDePartida;
    }
}
