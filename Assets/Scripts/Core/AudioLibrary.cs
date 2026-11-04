using UnityEngine;

/// <summary>
/// Every sound of the game in one place. Open Assets/Resources/AudioLibrary.asset and drag
/// your clips into the slots. Empty slots are simply silent, nothing breaks.
/// </summary>
[CreateAssetMenu(fileName = "AudioLibrary", menuName = "Kai/Audio Library")]
public class AudioLibrary : ScriptableObject
{
    [Header("Musica (se elige sola segun la escena)")]
    [Tooltip("Splash, menu principal y menu de actividad")]
    public AudioClip menuMusic;
    public AudioClip level1Music;
    public AudioClip level2Music;

    [Header("Interfaz")]
    [Tooltip("Se reproduce al hacer clic en CUALQUIER boton de cualquier menu")]
    public AudioClip buttonClick;

    [Header("Kai")]
    [Tooltip("Al lanzar el golpe")]
    public AudioClip playerAttack;
    public AudioClip playerJump;
    [Tooltip("Cuando un enemigo le quita vida a Kai")]
    public AudioClip playerHurt;
    public AudioClip playerDeath;

    [Header("Enemigos")]
    [Tooltip("Cuando Kai le pega a un enemigo")]
    public AudioClip enemyHit;
    public AudioClip enemyDeath;

    [Header("Objetos")]
    [Tooltip("Corazon recogido (suma vida)")]
    public AudioClip heartPickup;
    public AudioClip gemFragmentPickup;
    public AudioClip staffPickup;
    public AudioClip magicBolt;

    [Header("Estados del juego")]
    public AudioClip victory;
    public AudioClip gameOver;
    public AudioClip pause;
}
