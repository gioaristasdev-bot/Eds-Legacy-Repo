using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossZone2D : MonoBehaviour
{
    public GameObject boss;               // Referencia al Boss
    public GameObject[] enemies;          // Referencia a los Enemigos
    public GameObject[] walls;            // Array de muros a aparecer/desaparecer
    public AudioSource levelMusic;        // AudioSource de la música del nivel
    public AudioClip bossMusic;           // Clip de la música del Boss
    private AudioSource audioSource;      // AudioSource para reproducir la música del Boss

    private bool bossSpawned = false;     // Verifica si el Boss ya ha aparecido
    private bool bossDefeated = false;    // Verifica si el Boss ha sido derrotado

    void Start()
    {
        // Crear un AudioSource independiente para la música del Boss
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;  // Para que la música del Boss se repita
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica si el objeto que entró tiene la etiqueta "Player"
        if (other.CompareTag("Player") && !bossSpawned)
        {
            // Aparece el Boss y los muros
            boss.SetActive(true);

            foreach (GameObject wall in walls)
            {
                wall.SetActive(true);
            }

            foreach (GameObject enemi in enemies)
            {
                enemi.SetActive(true);
            }
            // Cambiar la música de fondo por la del Boss
            levelMusic.Pause();          // Pausa la música del nivel
            audioSource.clip = bossMusic;
            audioSource.Play();           // Reproduce la música del Boss

            bossSpawned = true;
        }
    }

    void Update()
    {
        // Verifica si el Boss ha sido derrotado (ya no está activo)
        if (bossSpawned && !bossDefeated && (!boss.activeSelf || boss == null))
        {
            bossDefeated = true;

            // Desaparecer los muros al eliminar el Boss
            foreach (GameObject wall in walls)
            {
                wall.SetActive(false);
            }

            // Detener la música del Boss y volver a la música del nivel
            audioSource.Stop();
            levelMusic.UnPause();         // Regresa la música original del nivel

            // Desactivar el script para que no se ejecute más
            this.enabled = false;
        }
    }
}



