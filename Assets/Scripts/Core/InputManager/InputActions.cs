using InControl;

public class InputActions : PlayerActionSet
{
    public PlayerAction menuAction;
    public PlayerAction crowlAction;
    public PlayerAction jumpAction;
    public PlayerAction interactionAction;
    public PlayerAction dashAction;
    public PlayerAction kickAction;
    public PlayerAction punchAction;
    // public PlayerAction dashAction; // DODANE
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
        // Menu
        menuAction = CreatePlayerAction("Menu");
        menuUpAction = CreatePlayerAction("Menu Up");
        menuDownAction = CreatePlayerAction("Menu Down");
        menuEnterAction = CreatePlayerAction("Menu Enter");

        // Movement
        goLeftAction = CreatePlayerAction("Go Left");
        goRightAction = CreatePlayerAction("Go Right");
        goUpAction = CreatePlayerAction("Go Up");
        goDownAction = CreatePlayerAction("Go Down");
        jumpAction = CreatePlayerAction("Jump");
        crowlAction = CreatePlayerAction("Crouch");
        interactionAction = CreatePlayerAction("Interaction");
        dashAction = CreatePlayerAction("Dash");
        kickAction = CreatePlayerAction("Kick");
        punchAction = CreatePlayerAction("Punch");
        //dashAction = CreatePlayerAction("Dash"); // DODANE

        // Look
        lookLeftAction = CreatePlayerAction("Look Left");
        lookRightAction = CreatePlayerAction("Look Right");
        lookUpAction = CreatePlayerAction("Look Up");
        lookDownAction = CreatePlayerAction("Look Down");
    }

    public static InputActions CreateWithDefaultBindings(float minDeadzone, float maxDeadzone)
    {
        var actions = new InputActions();
        var bindings = ScriptableManager.Instance.bindingProperties;

        void Bind(PlayerAction action, string name)
        {
            action.AddDefaultBinding(bindings.GetBinding(name).key);
            action.AddDefaultBinding(bindings.GetBinding(name).inputControlType);
        }

        // Menu
        Bind(actions.menuAction, "Menu");
        Bind(actions.menuUpAction, "Menu Up");
        Bind(actions.menuDownAction, "Menu Down");
        Bind(actions.menuEnterAction, "Menu Enter");

        // Movement
        Bind(actions.goLeftAction, "Go Left");
        Bind(actions.goRightAction, "Go Right");
        Bind(actions.goUpAction, "Go Up");
        Bind(actions.goDownAction, "Go Down");

        // Look
        Bind(actions.lookLeftAction, "Look Left");
        Bind(actions.lookRightAction, "Look Right");
        Bind(actions.lookUpAction, "Look Up");
        Bind(actions.lookDownAction, "Look Down");

        // Actions
        Bind(actions.jumpAction, "Jump");
        Bind(actions.crowlAction, "Crouch");
        Bind(actions.interactionAction, "Interaction");
        Bind(actions.dashAction, "Dash");
        Bind(actions.kickAction, "Kick");
        Bind(actions.punchAction, "Punch");
        //  Bind(actions.dashAction, "Dash"); // DODANE

        actions.moveAction = actions.CreateTwoAxisPlayerAction(
            actions.goLeftAction, actions.goRightAction,
            actions.goDownAction, actions.goUpAction);

        actions.lookAction = actions.CreateTwoAxisPlayerAction(
            actions.lookLeftAction, actions.lookRightAction,
            actions.lookDownAction, actions.lookUpAction);

        return actions;
    }
}
