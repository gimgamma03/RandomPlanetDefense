using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class EnemyData : ScriptableObject
{
    public int gold;
    public int scorePoint;
    public float maxHp;
    public float moveSpeed;
    public float rotateSpeed;
}
