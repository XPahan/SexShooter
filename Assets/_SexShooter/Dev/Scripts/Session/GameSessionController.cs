using cowsins;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SexShooter.Dev
{
    /// <summary>
    /// Session glue: start waves, handle player death, pause, restart, quit.
    /// </summary>
    public class GameSessionController : MonoBehaviour
    {
        [SerializeField] private PlayerDependencies playerDependencies;
        [SerializeField] private EnemyWaveSpawner spawner;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private bool pauseOnDeath = true;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.35f;

        private PlayerStats playerStats;
        private bool playerDead;
        private bool menuOpen;
        private CursorLockMode previousLock;
        private bool previousCursorVisible;
        private AudioSource musicSource;

        private void Start()
        {
            if (playerDependencies == null)
                playerDependencies = FindFirstObjectByType<PlayerDependencies>();

            if (playerDependencies == null)
            {
                Debug.LogError("[GameSession] PlayerDependencies not found.");
                enabled = false;
                return;
            }

            playerStats = playerDependencies.GetComponentInChildren<PlayerStats>(true);
            if (playerStats == null)
                playerStats = FindFirstObjectByType<PlayerStats>();

            if (playerStats == null)
            {
                Debug.LogError("[GameSession] PlayerStats not found.");
                enabled = false;
                return;
            }

            playerStats.AddOnDieListener(OnPlayerDied);
            StartMusic();

            if (spawner != null)
                spawner.Begin(playerStats.transform);
        }

        private void StartMusic()
        {
            if (backgroundMusic == null) return;
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = backgroundMusicVolume;
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        private void OnDestroy()
        {
            if (playerStats != null)
                playerStats.RemoveOnDieListener(OnPlayerDied);
        }

        private void Update()
        {
            if (KeyboardEscapePressed())
                ToggleMenu();
        }

        private void OnPlayerDied()
        {
            if (playerDead) return;
            playerDead = true;
            spawner?.StopSpawning();

            if (pauseOnDeath)
            {
                Time.timeScale = 0f;
                OpenMenu(force: true);
            }
        }

        private void ToggleMenu()
        {
            if (menuOpen) CloseMenu();
            else OpenMenu(force: false);
        }

        private void OpenMenu(bool force)
        {
            if (menuOpen && !force) return;
            menuOpen = true;
            previousLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (!playerDead) Time.timeScale = 0f;
        }

        private void CloseMenu()
        {
            if (!menuOpen) return;
            if (playerDead) return; // stay open while dead

            menuOpen = false;
            Time.timeScale = 1f;
            Cursor.lockState = previousLock;
            Cursor.visible = previousCursorVisible;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static bool KeyboardEscapePressed()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        private void OnGUI()
        {
            if (!menuOpen && !playerDead) return;

            float w = 280f;
            float h = playerDead ? 160f : 120f;
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.Box(box, playerDead ? "YOU DIED" : "PAUSE");

            Rect r1 = new Rect(box.x + 40f, box.y + 50f, w - 80f, 30f);
            if (GUI.Button(r1, "Перезапуск"))
                Restart();

            Rect r2 = new Rect(box.x + 40f, box.y + 90f, w - 80f, 30f);
            if (GUI.Button(r2, "Выход"))
                Quit();

            if (playerDead)
            {
                GUI.Label(new Rect(box.x + 20f, box.y + 28f, w - 40f, 20f), "Session Interrupted");
            }
        }
    }
}
