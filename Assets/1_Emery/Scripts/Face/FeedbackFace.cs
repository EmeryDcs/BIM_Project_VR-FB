using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackFace : MonoBehaviour
{
	[SerializeField]
	float evaluationTime = 10f;
	[SerializeField]
	DetectFaceNegativeAffect SC_detectFaceNegativeAffect;
	[SerializeField]
	GameObject feedbackImage;

	private bool isDistracted = false;
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

		cpt = (int)(evaluationTime / 2f);
	}

	void FixedUpdate()
	{
		timer += Time.deltaTime;

		if (timer < evaluationTime)
		{
			lastMinute.Add(SC_detectFaceNegativeAffect.GetIsExpressionActive() ? 1 : -1);
		} 
		else
		{
			lastMinute.Add(SC_detectFaceNegativeAffect.GetIsExpressionActive() ? 1 : -1);
			lastMinute.RemoveAt(0);

			cpt = 0;
			foreach (int f in lastMinute)
			{
				cpt += f;
			}

			float t = (float)(cpt + lastMinute.Count) / (2f * lastMinute.Count);
			feedbackImage.GetComponent<Image>().color = new Color(t, t, t);
			//text.text = $"Cpt : {cpt}";
			//if (cpt > (int)(evaluationTime / 2f))
			//{
			//	feedbackImage.SetActive(true);
			//	isDistracted = true;
			//}
			//else if (cpt <= (int)(evaluationTime / 2f))
			//{
			//	feedbackImage.SetActive(false);
			//	isDistracted = false;
			//}
		}
	}

	public bool GetIsDistracted()
	{
		return isDistracted;
	}
}
