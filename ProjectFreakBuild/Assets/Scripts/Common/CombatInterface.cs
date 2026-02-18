using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamagable
{
    bool TakeDamage(int damage, DamageType.Type type, GameObject agressor, ElementType.Element element);
}
