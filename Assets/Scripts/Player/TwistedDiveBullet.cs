using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwistedDiveBullet : MonoBehaviour
{
    // Start is called before the first frame update

    private float speed = 10f;
    private float maxDistance = 5f;
    private float damage;
    private Vector2 direction;
    private float KnockbackForce;
    private Vector2 initPlayerPos;
    private Vector2 initPos;
    private Vector2 colliderSize;
    public BoxCollider2D boxCollider2D;
    public SpriteRenderer spriteRenderer;
    private static HashSet<GameObject> collidedWith;
    private Rigidbody2D rigidbody2D;

    private void Start() {
        rigidbody2D = GetComponent<Rigidbody2D>();
        initPos = this.transform.position;
        collidedWith = new HashSet<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        rigidbody2D.velocity = direction * speed;
//        Debug.Log("dist: " + Vector2.Distance(initPos, transform.position) + " max dist: " + maxDistance);

        if (Vector2.Distance(initPos, transform.position) > maxDistance) {
            Destroy(gameObject);
        }
    }

    public void SetDirection(Vector2 vector) {
        this.direction = vector;
    }

    public void SetDamage(float damage) {
        this.damage = damage;
    }

    public void SetSpeed(float speed) {
        this.speed = speed;
    }

    public void SetDistance(float dist) {
        this.maxDistance = dist;
    }

    public void SetKnockback(float knockbackForce) {
        this.KnockbackForce = knockbackForce;
    }
    
    public void SetPlayerTransform(Vector2 position) { 
        initPlayerPos = position;
    }

    public void SetColliderSize(Vector2 vector2) {
        this.colliderSize = vector2;
        this.transform.position += new Vector3(0, (colliderSize.y/2), 0);
        this.gameObject.transform.localScale = new Vector3(colliderSize.x, colliderSize.y, 1);
    }

    private void OnTriggerEnter2D(Collider2D other) {

        if (other.tag.Equals("Ground")) {
            collidedWith.Clear();
            Debug.Log("Ground");
            Destroy(gameObject);
        }

        Entity entity;

        if (other.TryGetComponent(out entity) && other.CompareTag("Enemy") && !collidedWith.Contains(other.gameObject))
        {
            entity.TakeDamage(new Damage(damage, 0f), Player.instance.gameObject);
            entity.Knockback(initPlayerPos, Vector2.one * KnockbackForce);
            collidedWith.Add(other.gameObject);
            Debug.Log(collidedWith.Count);
        }

        Prop prop;

        if (other.TryGetComponent(out prop))
        {
            prop.TakeHit();
        }

        OutputInteractable interactable;

        if (other.TryGetComponent(out interactable))
        {
            interactable.SetActivate(true);
        }
    

    }
}
