using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavAgentMovement : MonoBehaviour
{
	public float range = 10.0f;
	private NavMeshAgent agent;
	private Vector3 point;
	bool RandomPoint(Vector3 center, float range, out Vector3 result)
	{
		for (int i = 0; i < 30; i++)
		{
			Vector3 randomPoint = center + Random.insideUnitSphere * range;
			NavMeshHit hit;
			if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
			{
				result = hit.position;
				return true;
			}
		}
		result = Vector3.zero;
		return false;
	}

	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		if (RandomPoint(transform.position, range, out point))
		{
			agent.destination = point;
			Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
		}
	}

	void Update()
	{
		if (agent.remainingDistance <= 0.5)
		{
			if (RandomPoint(transform.position, range, out point))
			{
				agent.destination = point;
				Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
			}
		}
	}
}
