using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    public float speed;
    public DamagePackage _Damage;

    [Header("Depreciated")]
    public int damageAmount = 1;
    [SerializeField] private DamageType.AttackType dType;
    public List<ElementType.Element> element;
    public GameObject instigator;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == instigator)
        {
            return;
        }

        IDamagable damagable = other.GetComponent<IDamagable>();
        if (damagable == null) 
        { 
            print("Detecting hit " + other.gameObject.name); 
            Destroy(gameObject); 
            return; 
        }

        //spawn hit effects here
        print("Detecting hit " + other.gameObject.name);
        damagable.TakeDamage(_Damage);
        Destroy(gameObject);
        
    }

}
