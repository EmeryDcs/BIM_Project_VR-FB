using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupFaceFeedback : MonoBehaviour
{
	[SerializeField]
	private GameObject feedbackGroupImage;
	[SerializeField]
	private float timerBeforeDisablingFeedback = 5f;
	[SerializeField]
	private AudioSource announcementFeedback;

	private List<FeedbackFace> feedbackFaces = new List<FeedbackFace>();

	private void Awake()
	{
		announcementFeedback = GetComponent<AudioSource>();
	}

	public void GetAllScriptsFeedbackFace()
	{
		foreach (FeedbackFace script in FindObjectsOfType<FeedbackFace>())
		{
			feedbackFaces.Add(script);
		}
	}

	private void Update()
	{
		if (feedbackFaces.Count == 0) return;

		int howManyDistracted = 0;
		foreach (FeedbackFace script in feedbackFaces)
		{
			if (script != null)
			{
				howManyDistracted += script.GetIsDistracted() ? 1 : 0;
			}
		}

		if (howManyDistracted >= feedbackFaces.Count)
		{
			feedbackGroupImage.SetActive(true);
			announcementFeedback.Play();
			StartCoroutine(DisableFeedbackGroupImage());
		}
	}

	private IEnumerator DisableFeedbackGroupImage()
	{
		yield return new WaitForSeconds(timerBeforeDisablingFeedback);
		feedbackGroupImage.SetActive(false);
	}
}
