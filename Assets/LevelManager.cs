using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelObject
    {
        public GameObject levelObject; // The main level object
        public SpriteRenderer[] sprites; // Array of 5 sprites under the level object
    }

    public LevelObject[] levels; // Array of level objects
    public SpriteRenderer[] sprites; // Array of 5 sprites for current level
    private int currentLevelIndex = 0;
    private int triesLeft = 5;
    private float timeLeft = 10f;
    private bool canInteract = true;
    private bool timerStarted = false;
    
    public GameObject StartPanel;
    public GameObject GameOverPanel;
    public GameObject PausePanel;
    public GameObject WinPanel;
    public AudioSource audioSource; // Audio source for sound effects
    public AudioClip wrong; // Sound for incorrect click
    public AudioClip gameover; // Sound for game over
    public AudioClip point; // Sound for correct click
    public AudioClip winscreensfx; // Sound for win screen
    public AudioClip tap; // Sound for tap
    public Text timerText; // UI Text component for timer display
    public GameObject clickEffectPrefab; // Prefab to spawn on sprite click
    private int correctSpritesClicked = 0;
    private List<GameObject> spawnedPrefabs = new List<GameObject>(); // Track spawned prefabs
    
    public GameObject[] correctImage; // UI images for correct clicks
    public GameObject[] incorrectImage; // UI images for incorrect clicks

    private Vector3 originalCameraPosition; // To store the camera's original position

    void Start()
    {
        // Show start panel and initialize timer display
        StartPanel.SetActive(true);
        canInteract = false;
        UpdateTimerDisplay();
        originalCameraPosition = Camera.main.transform.position; // Store initial camera position
    }

    void Update()
    {
        if (!canInteract || !timerStarted) return;

        // Update timer
        timeLeft -= Time.deltaTime;
        UpdateTimerDisplay();
        
        if (timeLeft <= 0)
        {
            GameOver();
        }

        // Handle mouse click
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 rayPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.zero);
            audioSource.PlayOneShot(tap);
            if (hit.collider != null)
            {
                Debug.Log("Clicked on: " + hit.collider.name);
                if (hit.collider.CompareTag("bg"))
                {
                    WrongTouch();
                }
                else if (hit.collider.CompareTag("Sprite"))
                {
                    HandleSpriteClick(hit.collider.gameObject);
                }
            }
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            if (!timerStarted)
            {
                timerText.text = "00:10";
            }
            else
            {
                int minutes = Mathf.FloorToInt(timeLeft / 60);
                int seconds = Mathf.FloorToInt(timeLeft % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }
        }
    }

    void SetupLevel(int levelIndex)
    {
        // Deactivate all levels first
        foreach (LevelObject level in levels)
        {
            level.levelObject.SetActive(false);
        }

        // Activate current level
        levels[levelIndex].levelObject.SetActive(true);
        sprites = levels[levelIndex].sprites;
        Debug.Log($"Level {levelIndex} has {sprites.Length} sprites"); // Debug sprite count
        
        // Reset level variables
        triesLeft = 5;
        timeLeft = 10f;
        correctSpritesClicked = 0;
        canInteract = true;
        timerStarted = true;

        // Ensure all sprites are visible and interactable
        foreach (SpriteRenderer sprite in sprites)
        {
            sprite.enabled = true;
            Collider2D collider = sprite.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        // Clear any existing prefabs
        ClearSpawnedPrefabs();

        UpdateTimerDisplay();
    }

    void HandleSpriteClick(GameObject clickedSprite)
    {
        if (!canInteract) return;

        triesLeft--;
        Debug.Log("Right");
        audioSource.PlayOneShot(point);

        // Spawn prefab at the clicked sprite's position
        if (clickEffectPrefab != null)
        {
            Vector3 spawnPosition = clickedSprite.transform.position;
            GameObject spawnedPrefab = Instantiate(clickEffectPrefab, spawnPosition, Quaternion.identity);
            spawnedPrefabs.Add(spawnedPrefab);
        }

        // Update UI for correct click
        if (triesLeft >= 0 && triesLeft < correctImage.Length)
        {
            correctImage[triesLeft].SetActive(true);
        }

        // Disable the clicked sprite's renderer and collider
        clickedSprite.GetComponent<SpriteRenderer>().enabled = false;
        Collider2D collider = clickedSprite.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false; // Prevent further clicks
        }
        correctSpritesClicked++;

        // Check if all sprites are clicked
        if (correctSpritesClicked >= 5)
        {
            NextLevel();
        }
        else if (triesLeft <= 0)
        {
            GameOver();
        }
    }

    public void resetcorrectImage()
    {
        for (int i = 0; i < correctImage.Length; i++)
        {
            correctImage[i].SetActive(false);
        }
    }

    public void resetincorrectImage()
    {
        for (int i = 0; i < incorrectImage.Length; i++)
        {
            incorrectImage[i].SetActive(false);
        }
    }

    void WrongTouch()
    {
        triesLeft--;
        Debug.Log("Wrong touch");
        audioSource.PlayOneShot(wrong);

        // Update UI for incorrect click
        if (triesLeft >= 0 && triesLeft < incorrectImage.Length)
        {
            incorrectImage[triesLeft].SetActive(true);
        }

        if (triesLeft <= 0)
        {
            GameOver();
        }
    }

    void ClearSpawnedPrefabs()
    {
        foreach (GameObject prefab in spawnedPrefabs)
        {
            if (prefab != null)
            {
                Destroy(prefab);
            }
        }
        spawnedPrefabs.Clear();
    }

    void NextLevel()
    {
        canInteract = false;
        timerStarted = false;
        currentLevelIndex++;
        resetincorrectImage();
        resetcorrectImage();
        ClearSpawnedPrefabs(); // Destroy all spawned prefabs
        UpdateTimerDisplay(); // Reset timer display to 00:10
        if (currentLevelIndex < levels.Length)
        {
            SetupLevel(currentLevelIndex);
            audioSource.PlayOneShot(winscreensfx);
        }
        else
        {
            Debug.Log("Game Completed!");
            audioSource.PlayOneShot(winscreensfx);
            WinPanel.SetActive(true);
            StartCoroutine(ShakeCamera(0.5f, 0.2f)); // Shake camera for win screen
        }
    }

    void GameOver()
    {
        audioSource.PlayOneShot(gameover);
        canInteract = false;
        timerStarted = false;
        Debug.Log("Game Over!");
        GameOverPanel.SetActive(true);
        ClearSpawnedPrefabs(); // Destroy all spawned prefabs
        UpdateTimerDisplay(); // Reset timer display
        StartCoroutine(ShakeCamera(0.3f, 0.15f)); // Shake camera for game over screen
    }

    // Coroutine to shake the camera
    private IEnumerator ShakeCamera(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.position = new Vector3(
                originalCameraPosition.x + x,
                originalCameraPosition.y + y,
                originalCameraPosition.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset camera position
        Camera.main.transform.position = originalCameraPosition;
    }

    // Called when start button is clicked
    public void StartGame()
    {
        StartPanel.SetActive(false);
        timerStarted = true;
        if (levels.Length > 0)
        {
            SetupLevel(currentLevelIndex);
        }
    }

    // Called when restart button is clicked
    public void RestartGame()
    {
        GameOverPanel.SetActive(false);
        WinPanel.SetActive(false);
        currentLevelIndex = 0;
        resetcorrectImage();
        resetincorrectImage();
        ClearSpawnedPrefabs(); // Destroy all spawned prefabs

        // Reset all sprites for all levels
        foreach (LevelObject level in levels)
        {
            foreach (SpriteRenderer sprite in level.sprites)
            {
                sprite.enabled = true;
                Collider2D collider = sprite.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.enabled = true; // Ensure collider is enabled
                }
            }
        }

        if (levels.Length > 0)
        {
            SetupLevel(currentLevelIndex);
        }
    }
}