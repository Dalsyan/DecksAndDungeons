using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    public void Init(int dir)
    {
        transform.Rotate(new Vector3(0,0,90 * dir));
    }

    public void Attack()
    {
        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject);
        seq.Append(transform.DORotate(new Vector3(0, 0, transform.rotation.z + 90), 0.25f));
        seq.Append(transform.DORotate(new Vector3(0, 0, transform.rotation.z), 0.25f));
        seq.OnComplete(() =>
            Destroy(gameObject));
    }
}
