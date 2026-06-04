using Fusion;
using System.Collections;
using UnityEngine;

public class GroupFaceFeedback : NetworkBehaviour
{
	public static GroupFaceFeedback Instance { get; private set; }

	[SerializeField]
	private GameObject feedbackGroupImage;
	[SerializeField]
	private float timerBeforeDisablingFeedback = 5f;
	[SerializeField]
	private AudioSource announcementFeedback;

	private bool isLecteurFocused = true;
	private bool isCalculateurFocused = true;
	private bool isModelisateurFocused = true;

	private void Awake()
	{
		announcementFeedback = GetComponent<AudioSource>();
	}

	public void SetFocus(int roleIndex, bool isFocused)
	{
		switch (roleIndex)
		{
			case 0:
				isLecteurFocused = isFocused;
				break;
			case 1:
				isCalculateurFocused = isFocused;
				break;
			case 2:
				isModelisateurFocused = isFocused;
				break;
			default:
				Debug.LogWarning("[Emery] Invalid role index: " + roleIndex);
				break;
		}
	}

	public override void Render()
	{
		if (!isCalculateurFocused && !isLecteurFocused && !isModelisateurFocused)
		{
			if (!feedbackGroupImage.activeSelf)
			{
				feedbackGroupImage.SetActive(true);
				if (announcementFeedback != null)
				{
					announcementFeedback.Play();
				}
				StartCoroutine(DisableFeedbackGroupImage());
			}
		}
	}

	private IEnumerator DisableFeedbackGroupImage()
	{
		yield return new WaitForSeconds(timerBeforeDisablingFeedback);
		feedbackGroupImage.SetActive(false);
	}

	/// <summary>
	/// Création de l'objet et récupération de tous les objets ciblés par chacun des rôles.
	/// </summary>
	public override void Spawned()
	{
		Debug.Log("[Emery] GroupFaceFeedback properly Spawned via Fusion.");

		// Pour un objet de scène unique (comportement de Singleton)
		if (GroupFaceFeedback.Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			// Si un autre existe déjà en scène, on retire silencieusement celui-ci du réseau
			Runner.Despawn(Object);
		}
	}
}
