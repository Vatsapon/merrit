using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossProjectile : MonoBehaviour
{

    private Damage projectileDamage;
    private float projectileSpeed;
    private Vector2 direction;
    private Rigidbody2D rigidbody;



    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        projectileDamage = BossFightController.instance.projectileDamage;
        projectileSpeed = BossFightController.instance.projectileSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Move();
    }

    public void Move() {
        rigidbody.velocity = direction * projectileSpeed;
    }

    public void SetDirection(Vector2 direction) {
        this.direction = direction;
        float angle = Mathf.Atan(direction.y / direction.x) * 180 / Mathf.PI;
        this.transform.localEulerAngles = new Vector3(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.tag.Equals("Player")) {
            other.gameObject.GetComponent<Player>().TakeDamage(projectileDamage, this.gameObject);
            Destroy(gameObject);
        } else if (other.gameObject.tag.Equals("Ground")) {
            Destroy(gameObject);
        }
    }

}
