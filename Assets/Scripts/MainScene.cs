using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScene : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var cube = ResourcesManager.GetInstance().LoadPrefab("Cube");
        Instantiate(cube);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
