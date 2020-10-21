using System.Collections;
using UnityEngine;
using System.Collections.Generic;

/// Manages game logic and controls the UI
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Tooltip("Game ends when an agent collects this much nectar")]
    public float maxNectar = 8f;

    public int mosquitos = 10;

    public GameObject mosquitoPrefab;

    [Tooltip("Game ends after this many seconds have elapsed")]
    public float timerAmount = 60f;

    [Tooltip("The UI Controller")]
    public UIController uiController;

    [Tooltip("The player")]
    public GameObject player;

    [Tooltip("The flower area")]
    public FlowerArea flowerArea;

    [Tooltip("The main camera for the scene")]
    public Camera mainCamera;

    // When the game timer started
    private float gameTimerStartTime;

    public List<GameObject> mosquitoList = new List<GameObject>();

    public int enemyScore;

    /// All possible game states
    public enum GameState
    {
        Default,
        MainMenu,
        Preparing,
        Playing,
        Gameover
    }

    /// The current game state
    public GameState State { get; private set; } = GameState.Default;

    /// Gets the time remaining in the game
    public float TimeRemaining
    {
        get
        {
            if (State == GameState.Playing)
            {
                float timeRemaining = timerAmount - (Time.time - gameTimerStartTime);
                return Mathf.Max(0f, timeRemaining);
            }
            else
            {
                return 0f;
            }
        }
    }

    /// <summary>
    /// Handles a button click in different states
    /// </summary>
    public void ButtonClicked()
    {
        if (State == GameState.Gameover)
        {
            // In the Gameover state, button click should go to the main menu
            MainMenu();
        }
        else if (State == GameState.MainMenu)
        {
            // In the MainMenu state, button click should start the game
            StartCoroutine(StartGame());
        }
        else
        {
            Debug.LogWarning("Button clicked in unexpected state: " + State.ToString());
        }
    }

    /// Called when the game starts
    private void Start()
    {        
        instance = this;

        Cursor.lockState = CursorLockMode.Confined;

        for (int i = 0; i < mosquitos; i++)
        {
            GameObject go = Instantiate(mosquitoPrefab, transform.parent);
            mosquitoList.Add(go);
        }

        // Subscribe to button click events from the UI
        uiController.OnButtonClicked += ButtonClicked;

        // Start the main menu
        MainMenu();
    }

    /// Called on destroy
    private void OnDestroy()
    {
        // Unsubscribe from button click events from the UI
        uiController.OnButtonClicked -= ButtonClicked;
    }

    /// Shows the main menu
    private void MainMenu()
    {
        // Set the state to "main menu"
        State = GameState.MainMenu;

        // Update the UI
        uiController.ShowBanner("");
        uiController.ShowButton("Start");

        // Use the main camera, disable agent cameras
        mainCamera.gameObject.SetActive(true);
        player.SetActive(false);
        player.GetComponent<Rigidbody>().isKinematic = true;

        // Freeze the agents
        //player.FreezeAgent();
        foreach (GameObject mosquito in mosquitoList)
        {
            mosquito.GetComponent<HummingbirdAgent>().OnEpisodeBegin();
            mosquito.GetComponent<HummingbirdAgent>().FreezeAgent();
        }
    }

    /// Starts the game with a countdown
    private IEnumerator StartGame()
    {
        foreach (GameObject mosquito in mosquitoList)
        {
            Destroy(mosquito);
        }
        mosquitoList.Clear();
        for (int i = 0; i < mosquitos; i++)
        {
            GameObject go = Instantiate(mosquitoPrefab, transform.parent);
            mosquitoList.Add(go);
        }

        // Set the state to "preparing"
        State = GameState.Preparing;

        // Update the UI (hide it)
        uiController.ShowBanner("");
        uiController.HideButton();
        uiController.SetOpponentNectar(enemyScore / maxNectar);

        // Use the player camera, disable the main camera
        mainCamera.gameObject.SetActive(false);
        player.SetActive(true);

        // Show countdown
        uiController.ShowBanner("3");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("2");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("1");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("Go!");
        yield return new WaitForSeconds(1f);
        uiController.ShowBanner("");

        // Set the state to "playing"
        State = GameState.Playing;

        // Start the game timer
        gameTimerStartTime = Time.time;

        // Unfreeze the agents
        //player.UnfreezeAgent();
        foreach (GameObject mosquito in GameManager.instance.mosquitoList)
        {
            mosquito.GetComponent<HummingbirdAgent>().UnfreezeAgent();
        }

        player.GetComponent<Rigidbody>().isKinematic = false;

    }

    /// Ends the game
    private void EndGame()
    {
        // Set the game state to "game over"
        State = GameState.Gameover;

        // Freeze the agents
        //player.FreezeAgent();
        foreach (GameObject mosquito in GameManager.instance.mosquitoList)
        {
            mosquito.GetComponent<HummingbirdAgent>().FreezeAgent();
        }

        // Update banner text depending on win/lose
        if (enemyScore < maxNectar)
        {
            uiController.ShowBanner("You win!");
        }
        else
        {
            uiController.ShowBanner("You Lose!");
        }

        // Update button text
        uiController.ShowButton("Main Menu");
    }

    /// Called every frame
    private void Update()
    {
        if (State == GameState.Playing)
        {
            // Check to see if time has run out or either agent got the max nectar amount
            if (enemyScore >= maxNectar)
            {
                EndGame();
            }

            if (mosquitoList.Count == 0)
            {
                EndGame();
            }

            // Update the timer and nectar progress bars
            uiController.SetTimer(TimeRemaining);
            //uiController.SetPlayerNectar(player.NectarObtained / maxNectar);
            uiController.SetOpponentNectar(enemyScore / maxNectar);
        }
        else if (State == GameState.Preparing || State == GameState.Gameover)
        {
            // Update the timer
            uiController.SetTimer(TimeRemaining);
        }
        else
        {
            // Hide the timer
            uiController.SetTimer(-1f);

            // Update the progress bars
            enemyScore = 0;
            uiController.SetOpponentNectar(enemyScore / maxNectar);
        }

    }
}
