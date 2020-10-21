using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a collection of flower plants and attached flowers
/// </summary>
public class FlowerArea : MonoBehaviour
{
    // The diameter of the area where the agent and points can be
    // used for observing relative distance from agent to flower
    public const float AreaDiameter = 20f;

    public GameObject[] targetPoints;

}
