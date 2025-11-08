using System.Collections.Generic;
using UnityEngine;
using InControl;
using ProjectDawn.SplitScreen;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private SplitScreenEffect splitScreenEffect;
    [SerializeField] private Transform spawnPoint;
    public GameObject playerPrefab1; // Assign first player prefab in the inspector
    public GameObject playerPrefab2; // Assign second player prefab in the inspector
    public GameObject cameraPrefab;
    private List<GameObject> spawnedPlayers = new List<GameObject>();
    private int playersCount = 0;

    private static PlayerSpawner _instance;
    public static PlayerSpawner Instance { get { return _instance; } }
    private void Awake()
    {
        // Ensure only one instance of GameManager exists
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    void Start()
    {
        // Initial spawn based on connected devices at start
        SpawnPlayers();

        // Subscribe to device attached event
        InputManager.OnDeviceDetached += OnDeviceDetached;
        InputManager.OnDeviceAttached += OnDeviceAttached;
    }

    public void ResetPlayerPosition(PlayerController playerController)
    {
        playerController.transform.position = spawnPoint.position;
    }

    void SpawnPlayers()
    {
        foreach (var device in InputManager.Devices)
        {
            if (spawnedPlayers.Count < InputManager.Devices.Count)
            {
                SpawnPlayer(device);
            }
        }
    }

    void SpawnPlayer(InputDevice device)
    {
     //   PadScreen.gameObject.SetActive(false);
        playersCount++;

        // Choose prefab based on playersCount
        GameObject playerPrefab = playersCount == 1 ? playerPrefab1 : playerPrefab2;
        GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position + new Vector3(0,0, -playersCount), Quaternion.identity);
        PlayerController playerController = newPlayer.GetComponent<PlayerController>();
        GameObject newCamera = Instantiate(cameraPrefab, spawnPoint.position, Quaternion.identity);

        splitScreenEffect.AddScreen(newCamera.GetComponent<Camera>(), playerController.transform);

        playerController.playerID = playersCount;
        playerController.InputDevice = device; // Assign the device

        spawnedPlayers.Add(newPlayer);
    }

    void OnDeviceAttached(InputDevice device)
    {
    
        // Spawn a new player for the newly attached device
        SpawnPlayer(device);
    }
    public GameObject PadScreen;
    void OnDeviceDetached(InputDevice device)
    {
      
        // Find the player with the matching input device
        var playerToRemove = spawnedPlayers.Find(player => player.GetComponent<PlayerController>().InputDevice == device);
        if (playerToRemove != null)
        {
            // Optionally, handle other cleanup before destroying the player object
            splitScreenEffect.RemoveScreen(playerToRemove.transform); // Assuming the camera is a child of the player

            spawnedPlayers.Remove(playerToRemove);
            Destroy(playerToRemove);
            playersCount--; // Update players count
        }
    }
   
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        InputManager.OnDeviceDetached -= OnDeviceDetached;
        InputManager.OnDeviceAttached -= OnDeviceAttached;
    }
}
