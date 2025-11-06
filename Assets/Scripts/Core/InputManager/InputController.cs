using System.Collections;
using UnityEngine;
using InControl;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    private InputActions player1Actions;
    private InputActions player2Actions;

    public InputActions Player1Actions => player1Actions;
    public InputActions Player2Actions => player2Actions;

    public Vector2 player1MoveValue;
    public Vector2 player2MoveValue;

    [SerializeField] private float minDeadzone = 0.3f;
    [SerializeField] private float maxDeadzone = 1.0f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializePlayerActions();
    }

    private void Update()
    {
        if (player1Actions != null)
        {
            player1MoveValue = GetSmoothedInput(player1Actions);
        }

        if (player2Actions != null)
        {
            player2MoveValue = GetSmoothedInput(player2Actions);
        }
    }

    private void InitializePlayerActions()
    {
        // Gracz 1
        player1Actions = InputActions.CreateWithDefaultBindings(minDeadzone, maxDeadzone);
        if (InputManager.Devices.Count > 0)
        {
            player1Actions.Device = InputManager.Devices[0];
        }

        // Gracz 2
        player2Actions = InputActions.CreateWithDefaultBindings(minDeadzone, maxDeadzone);
        if (InputManager.Devices.Count > 1)
        {
            player2Actions.Device = InputManager.Devices[1];
        }
    }

    public void Vibrate(float intensity, InputActions actions, float time)
    {
        if (actions.Device == null) return;
        actions.Device.Vibrate(intensity, intensity);
        StartCoroutine(StopVibrationAfterTime(actions, time));
    }

    private IEnumerator StopVibrationAfterTime(InputActions actions, float time)
    {
        yield return new WaitForSeconds(time);
        actions.Device?.StopVibration();
    }

    private Vector2 GetSmoothedInput(InputActions actions)
    {
        float x = Utility.ApplyDeadZone(actions.moveAction.Value.x, minDeadzone, maxDeadzone);
        float y = Utility.ApplyDeadZone(actions.moveAction.Value.y, minDeadzone, maxDeadzone);
        return new Vector2(x, y).normalized;
    }
}
