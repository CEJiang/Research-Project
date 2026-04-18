using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hypothesis", menuName = "Narrative/Hypothesis")]
public class Hypothesis : ScriptableObject
{
    public string id;
    public string category;
    public string description;
}