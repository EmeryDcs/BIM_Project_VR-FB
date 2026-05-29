using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System.Linq;

public class FeedbackFace : MonoBehaviour
{
	[Tooltip("Time range to evaluate the affect.")]
	[SerializeField]
	float evaluationTime = 10f;
	[Tooltip("Threshold to consider the user as focused.")]
	[SerializeField]
	float faceThreshold = 0.012f;
	[Tooltip("You can let it empty if Component already on GameObject.")]
	[SerializeField]
	DetectFaceNegativeAffect SC_detectFaceNegativeAffect;
	[SerializeField]
	GameObject feedbackImage;

	private int index = 0;
	private bool hasBeenFocusedInLastElapsedTime = true;
	List<int> lastMinute = new List<int>();
	float timer = 0f;

	// Variables de tests
	//[SerializeField]
	//TextMeshProUGUI text;

	int cpt = 0;
	private void Awake()
	{
		if (SC_detectFaceNegativeAffect == null)
		{
			SC_detectFaceNegativeAffect = GetComponent<DetectFaceNegativeAffect>();
		}
	}

	private float timerFocusInLastElapsedTime = 0f;
	void FixedUpdate()
	{
		if (NetworkManager.Instance.Runner.ActivePlayers.Count() < 3)
			return;

		timer += Time.deltaTime;

		if (timer < evaluationTime)
		{
			lastMinute.Add(SC_detectFaceNegativeAffect.GetIsExpressionActive() ? 1 : 0);
			timerFocusInLastElapsedTime = evaluationTime;
		} 
		else
		{
			lastMinute.Add(SC_detectFaceNegativeAffect.GetIsExpressionActive() ? 1 : 0);
			lastMinute.RemoveAt(0);

			cpt = 0;
			foreach (int f in lastMinute)
			{
				cpt += f;
			}

			float t = (float)(cpt + lastMinute.Count) / (2f * lastMinute.Count);
			feedbackImage.GetComponent<Image>().color = new Color(t, t, t);

			if (cpt/lastMinute.Count >= faceThreshold)
			{
				hasBeenFocusedInLastElapsedTime = true;
				GroupFaceFeedback.Instance.SetFocus(index, hasBeenFocusedInLastElapsedTime);
				timerFocusInLastElapsedTime = 0f;
			}

			if (hasBeenFocusedInLastElapsedTime)
			{
				timerFocusInLastElapsedTime += Time.deltaTime;
				if (timerFocusInLastElapsedTime >= evaluationTime)
				{
					hasBeenFocusedInLastElapsedTime = false;
					GroupFaceFeedback.Instance.SetFocus(index, hasBeenFocusedInLastElapsedTime);
				}
			}
		}
	}

	public void SetIndex(int i)
	{
		index = i;
	}
}
