using UnityEngine;
using UnityEngine.Events; // Vital para poder ver las listas en el Inspector

public class ValidadorSecuencia : MonoBehaviour
{
    [Tooltip("¿El estudiante ya terminó el reto anterior?")]
    public bool permisoParaAvanzar = false;

    [Tooltip("Coloca aquí lo que pasa si hace el orden CORRECTO")]
    public UnityEvent AlTenerExito;

    [Tooltip("Coloca aquí lo que pasa si se equivoca de orden")]
    public UnityEvent AlTenerError;

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
}