using UnityEngine;


public class FireAlarmSwitch : MonoBehaviour
{
  public void OnSwitch()
    {
        FireAlarmSystem.Instance.ToggleAlarm();
    }
}
