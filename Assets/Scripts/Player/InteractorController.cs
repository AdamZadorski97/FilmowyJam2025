using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class InteractorController : MonoBehaviour
{
    public UnityEvent OnTriggerEnterEvent;
    public UnityEvent OnTriggerExitEvent;
    public UnityEvent OnInteractEvent;
    public PlayerController playerController;
    public bool isOnlyForMan;

    public void OnInteract(PlayerController _playerController)
    {
        playerController = _playerController;
        OnInteractEvent.Invoke();
    }

    




    private void OnTriggerEnter(Collider other)
    {
        if(other != null && other.GetComponent<PlayerController>())
        {
            other.GetComponent<PlayerController>().tempInteractorController = this;
            OnTriggerEnterEvent.Invoke();
        }
    }

    private void OnTriggerExit(Collider other) 
    {
        if (other != null && other.GetComponent<PlayerController>())
        {
        
                other.GetComponent<PlayerController>().tempInteractorController = null;
            OnTriggerExitEvent.Invoke();
        }
    }
}
