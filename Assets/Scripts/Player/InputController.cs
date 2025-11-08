using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using System;

public class InputController : MonoBehaviour
{

    public static InputController Instance { get; private set; }
    private InputActions player1Actions;
    private InputActions player2Actions;
    public InputActions Player1Actions => player1Actions;
    public InputActions Player2Actions => player2Actions;

    public Vector2 player1MoveValue;
    public Vector2 player2MoveValue;
    public Vector2 player3MoveValue;
    [SerializeField] private float minDeadzone = .3f;
    [SerializeField] private float maxDeadzone = 1;
    public InControlManager controlManager;

    public Vector2 movevalue;
    private void Update()
    {
        if (player1Actions != null)
        {
            player1MoveValue = MoveValue(player1Actions);
        }

        if (player2Actions != null)
        {
            player2MoveValue = MoveValue(player2Actions);
        }
    }


    private void InitializePlayerActions()
    {
        // Utworzenie akcji
        player1Actions = InputActions.CreateWithDefaultBindings(minDeadzone, maxDeadzone);
        player2Actions = InputActions.CreateWithDefaultBindings(minDeadzone, maxDeadzone);

        // Przypisanie urządzeń
        if (InputManager.Devices.Count > 0)
        {
            player1Actions.Device = InputManager.Devices[0];
            Debug.Log($"Player 1 Device: {InputManager.Devices[0].Name}");
        }

        // Użyj pętli lub bardziej pewnego sposobu na znalezienie unikalnego drugiego urządzenia
        if (InputManager.Devices.Count > 1)
        {
            // Sprawdź, czy drugie urządzenie jest unikalne i przypisz je
            if (InputManager.Devices[1] != InputManager.Devices[0])
            {
                player2Actions.Device = InputManager.Devices[1];
                Debug.Log($"Player 2 Device: {InputManager.Devices[1].Name}");
            }
            else
            {
                // Zdarza się w przypadku urządzeń wirtualnych lub klawiatury
                Debug.LogError("Drugie urządzenie jest identyczne z pierwszym! Sprawdź konfigurację kontrolerów.");
            }
        }
    }
    public void Vibrate(float intencity,  InputActions actions, float time)
    {
        actions.Device.Vibrate(intencity, intencity);
        StartCoroutine(OnParticleSystemStopped(actions, time));
    }

    IEnumerator OnParticleSystemStopped(InputActions actions, float time)
    {
        yield return new WaitForSeconds(time);
        actions.Device.StopVibration();
    }


    private Vector2 MoveValue(InputActions actions)
    {
        // Użyj pełnych wartości z martwą strefą
        float horizontalValue = Utility.ApplyDeadZone(actions.moveAction.Value.x, minDeadzone, maxDeadzone);
        float verticalValue = Utility.ApplyDeadZone(actions.moveAction.Value.y, minDeadzone, maxDeadzone);

        Vector2 moveVector = new Vector2(horizontalValue, verticalValue);

        // Normalizacja wektora, aby zapobiec szybszemu ruchowi po przekątnej
        if (moveVector.sqrMagnitude > 1)
        {
            moveVector.Normalize();
        }

        return moveVector;
    }


    private void Awake()
    {
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
}

public class InputActions : PlayerActionSet
{
    public PlayerAction menuAction;
    public PlayerAction crowlAction;
    public PlayerAction jumpAction;
    public PlayerAction interactionAction;
    public PlayerTwoAxisAction moveAction;
    public PlayerTwoAxisAction lookAction;

    public PlayerAction lookLeftAction;
    public PlayerAction lookRightAction;
    public PlayerAction lookUpAction;
    public PlayerAction lookDownAction;


    public PlayerAction goLeftAction;
    public PlayerAction goRightAction;
    public PlayerAction goUpAction;
    public PlayerAction goDownAction;

    public PlayerAction menuUpAction;
    public PlayerAction menuDownAction;
    public PlayerAction menuEnterAction;

    public InputActions()
    {

        //Menu
        menuAction = CreatePlayerAction("Menu");
        menuUpAction = CreatePlayerAction("Menu Up");
        menuDownAction = CreatePlayerAction("Menu Down");
        menuEnterAction = CreatePlayerAction("Menu Enter");
        //Movement
        goLeftAction = CreatePlayerAction("Go Left");
        goRightAction = CreatePlayerAction("Go Right");
        goUpAction = CreatePlayerAction("Go Up");
        goDownAction = CreatePlayerAction("Go Down");
        jumpAction = CreatePlayerAction("Jump");
        crowlAction = CreatePlayerAction("Crouch");
        interactionAction = CreatePlayerAction("Interaction");
        //Look
        lookLeftAction = CreatePlayerAction("Look Left");
        lookRightAction = CreatePlayerAction("Look Right");
        lookUpAction = CreatePlayerAction("Look Up");
        lookDownAction = CreatePlayerAction("Look Down");
    }

    public static InputActions CreateWithDefaultBindings(float minDeadzone, float maxDeadzone)
    {
        var playerActions = new InputActions();
        BindingProperties bindingsScriptable = ScriptableManager.Instance.bindingProperties;

        playerActions.menuAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu").key);
        playerActions.menuAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu").inputControlType);

        playerActions.menuUpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu Up").key);
        playerActions.menuUpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu Up").inputControlType);

        playerActions.menuDownAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu Down").key);
        playerActions.menuDownAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu Down").inputControlType);

        playerActions.menuEnterAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu Enter").key);
        playerActions.menuEnterAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu Enter").inputControlType);

        playerActions.menuAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu").key);
        playerActions.menuAction.AddDefaultBinding(bindingsScriptable.GetBinding("Menu").inputControlType);

        playerActions.goLeftAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Left").key);
        playerActions.goLeftAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Left").inputControlType);

        //Movement
        playerActions.goLeftAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Left").key);
        playerActions.goLeftAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Left").inputControlType);

        playerActions.goRightAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Right").key);
        playerActions.goRightAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Right").inputControlType);

        playerActions.goUpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Up").key);
        playerActions.goUpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Up").inputControlType);

        playerActions.goDownAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Down").key);
        playerActions.goDownAction.AddDefaultBinding(bindingsScriptable.GetBinding("Go Down").inputControlType);


        //Look
        playerActions.lookLeftAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Left").key);
        playerActions.lookLeftAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Left").inputControlType);

        playerActions.lookRightAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Right").key);
        playerActions.lookRightAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Right").inputControlType);

        playerActions.lookUpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Up").key);
        playerActions.lookUpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Up").inputControlType);

        playerActions.lookDownAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Down").key);
        playerActions.lookDownAction.AddDefaultBinding(bindingsScriptable.GetBinding("Look Down").inputControlType);

        playerActions.jumpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Jump").key);
        playerActions.jumpAction.AddDefaultBinding(bindingsScriptable.GetBinding("Jump").inputControlType);

        playerActions.crowlAction.AddDefaultBinding(bindingsScriptable.GetBinding("Crouch").key);
        playerActions.crowlAction.AddDefaultBinding(bindingsScriptable.GetBinding("Crouch").inputControlType);

        playerActions.interactionAction.AddDefaultBinding(bindingsScriptable.GetBinding("Interaction").key);
        playerActions.interactionAction.AddDefaultBinding(bindingsScriptable.GetBinding("Interaction").inputControlType);

        playerActions.moveAction = playerActions.CreateTwoAxisPlayerAction(playerActions.goLeftAction, playerActions.goRightAction, playerActions.goDownAction, playerActions.goUpAction);
        playerActions.lookAction = playerActions.CreateTwoAxisPlayerAction(playerActions.lookLeftAction, playerActions.lookRightAction, playerActions.lookDownAction, playerActions.lookUpAction);
        return playerActions;
    }
}


