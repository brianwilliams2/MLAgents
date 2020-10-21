using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public bool trainingMode;
    public GameObject mosquito;
    public float moveSpeed;
    public float turnSpeed;
    public Vector2 yClamp;

    private void Start()
    {
    }

    private void Update()
    {

        if(trainingMode)
        {
            transform.LookAt(mosquito.transform);
            GetComponent<Rigidbody>().AddForce(transform.forward * moveSpeed);
        }

        else
        {
            float moveX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * turnSpeed;
            float moveY = transform.localEulerAngles.x + -Input.GetAxis("Mouse Y") * turnSpeed;
            if (moveY > 180f) moveY -= 360f;
            moveY = Mathf.Clamp(moveY, yClamp.x, yClamp.y);

            transform.localEulerAngles = new Vector3(moveY, moveX, 0);

            GetComponent<Rigidbody>().AddForce(transform.forward * Input.GetAxis("Vertical") * moveSpeed);
        }
    }
}
