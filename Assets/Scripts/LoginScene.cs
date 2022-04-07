using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.Invoke("InvokeEnterGame", 2.0f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void InvokeEnterGame()
    {
        SceneManager.LoadScene("MainScene");
    }
}
