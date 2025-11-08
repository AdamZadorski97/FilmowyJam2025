using UnityEngine;

public class InteractiveHint :MonoBehaviour
{
    public void SpawnActionHint(string message)
    {
        InputHintController.Show("Interaction", message);
    }
    public void HideHint()
    {
        InputHintController.Hide();
    }
}
