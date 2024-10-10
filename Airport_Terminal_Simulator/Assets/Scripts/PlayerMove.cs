using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class playermove : MonoBehaviour
{
    new private Rigidbody rigidbody;
    public float speed = 4.0f;
    public float rotSpeed = 12f;
    private Animator animator;
    private Vector3 dir = Vector3.zero;

    void Start()
    {
        rigidbody = this.GetComponent<Rigidbody>();
    }

    void Update()
    {
        dir.x = Input.GetAxis("Horizontal");
        dir.z = Input.GetAxis("Vertical");
        dir.Normalize();
    }

    private void FixedUpdate()
    {
        if(dir != Vector3.zero){
            if (Mathf.Sign(transform.forward.x) != Mathf.Sign(dir.x) || Mathf.Sign(transform.forward.z) != Mathf.Sign(dir.z)){
                transform.Rotate(0, 1, 0);
            }
            transform.forward = Vector3.Lerp(transform.forward, dir, rotSpeed*Time.deltaTime);
        }
        rigidbody.MovePosition(this.gameObject.transform.position + dir * speed * Time.deltaTime);

    }

}
