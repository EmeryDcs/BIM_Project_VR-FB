using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectSpeaking : MonoBehaviour
{
	[Tooltip("Indique si le joueur est en train de parler depuis un certain temps.")]
	private bool isSpeaking = false;

	[Tooltip("Le seuil de volume en décibels positifs (dB) pour détecter la voix (ex: 30 pour un son faible).")]
	[SerializeField]
	private float dbThreshold = 30f;

	[Tooltip("Le temps en secondes pendant lequel le joueur doit parler en continu pour valider la détection.")]
	[SerializeField]
	private float timeRequiredToSpeak = 1.0f;

	[SerializeField]
	private GameObject feedbackImage;

	[SerializeField]
	private float lengthDisplayFeedback = 1.0f;

	private float currentSpeakingTime = 0f;

	// Amplitude de référence permettant de calibrer l'échelle en décibels positifs (0 dB = silence).
	// A ajuster si le microphone est très peu ou très sensible.
	private float refAmplitude = 0.0001f; 

	private AudioClip micClip;
	private string micDevice;
	private readonly int sampleWindow = 256;

	// Start is called before the first frame update
	void Start()
	{
		if (Microphone.devices.Length > 0)
		{
			micDevice = Microphone.devices[0];
			// Enregistrement continu (boucle)
			micClip = Microphone.Start(micDevice, true, 10, 44100);
		}
		else
		{
			Debug.LogWarning("Aucun microphone détecté !");
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (micClip != null)
		{
			float currentVolume = GetMicrophoneVolume();
			
			// Calcul des décibels sur une échelle positive par rapport à l'amplitude de référence
			float currentDb = 0f; 
			if (currentVolume > refAmplitude)
			{
				currentDb = 20f * Mathf.Log10(currentVolume / refAmplitude);
			}

			// Si le volume actuel dépasse notre seuil cible (ex: 30 dB)
			if (currentDb >= dbThreshold)
			{
				currentSpeakingTime += Time.deltaTime;

				if (currentSpeakingTime >= timeRequiredToSpeak)
				{
					isSpeaking = true;
				}
			}
			else
			{
				// Si le son baisse, on réinitialise
				currentSpeakingTime = 0f;
				isSpeaking = false;
			}
		}

		//if (isSpeaking)
		//{
		//	feedbackImage.SetActive(true);
		//} else
		//{
		//	feedbackImage.SetActive(false);
		//}
	}

	private float GetMicrophoneVolume()
	{
		int micPosition = Microphone.GetPosition(micDevice) - sampleWindow + 1;

		if (micPosition < 0)
			return 0f;

		float[] waveData = new float[sampleWindow];
		micClip.GetData(waveData, micPosition);

		float levelMax = 0f;
		for (int i = 0; i < sampleWindow; i++)
		{
			float wavePeak = waveData[i] * waveData[i];
			if (levelMax < wavePeak)
			{
				levelMax = wavePeak;
			}
		}

		return Mathf.Sqrt(levelMax);
	}

	public bool GetIsSpeaking()
	{
		return isSpeaking;
	}

	public void ShowFeedback()
	{
		feedbackImage.SetActive(true);
		StartCoroutine(FeedbackCoroutine());
	}

	private IEnumerator FeedbackCoroutine()
	{
		yield return new WaitForSeconds(lengthDisplayFeedback);
		feedbackImage.SetActive(false);
	}
}
