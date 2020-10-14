using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class kill : MonoBehaviour
{
    private void OnCollisionEnter(Collision other) {
        if(other.gameObject.tag == "Enemy" && this.gameObject.tag == "Player"){
            Destroy(other.gameObject);
        }
    }
}
