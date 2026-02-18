using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    public int damageAmount = 1;
    public DamageType.Type dType;
    public ElementType.Element element;
    public GameObject instigator;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        print("Bonk!");
        IDamagable damagable = collision.collider.GetComponent<IDamagable>();
        if (damagable == null) { Destroy(gameObject); return; }

        damagable.TakeDamage(damageAmount,dType, instigator, element);
        Destroy(gameObject);
    }
}
