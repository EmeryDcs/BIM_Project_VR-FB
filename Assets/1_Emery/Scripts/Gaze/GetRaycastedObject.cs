using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GetRaycastedObject : MonoBehaviour
{
	private int index = 0;
	private NetworkObject lastSentObject = null;

	public NetworkObject GetObject()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, transform.rotation * Vector3.forward, out hit))
		{
			return hit.collider.GetComponent<NetworkObject>();
		}
		return null;
	}

	public void SetIndex(int index)
	{
		Debug.Log($"[Emery] Index set to {index}");
		this.index = index;
	}

	public void FixedUpdate()
	{
		NetworkObject currentObject = GetObject();

		// Optimisation réseau : On n'envoie la valeur que s'il y a un changement d'objet ciblé
		if (currentObject != lastSentObject && GlowObjectRaycasted.Instance != null)
		{
			lastSentObject = currentObject;
			GlowObjectRaycasted.Instance.RPC_SetGazedObject(index, currentObject);
		}
	}

	public int GetIndex()
	{
		return index;
	}

	public NetworkObject GetLastSentObject()
	{
		return lastSentObject;
	}
}
