using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rolling : MonoBehaviour
{ 
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 1, 0);
        transform.Translate(0, -1, 0);
        if(transform.position.y < -500)
        {
            Destroy(this.gameObject);
        }
    }
}
