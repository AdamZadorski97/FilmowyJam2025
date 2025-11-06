using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableManager : MonoBehaviour
{
    private static ScriptableManager _instance;
    public static ScriptableManager Instance { get { return _instance; } }

    public BindingProperties bindingProperties;

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
}
