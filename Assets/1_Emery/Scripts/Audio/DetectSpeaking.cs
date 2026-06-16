using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

public class DetectSpeaking : MonoBehaviour
{
	[Header("Variables")]
	[Tooltip("Indique si le joueur est en train de parler depuis un certain temps.")]
	private bool isSpeaking = false;

	[Tooltip("Le seuil de volume en décibels (dBFS) pour détecter la voix (ex: -30 pour un son modéré, 0 est le volume maximum).")]
	[SerializeField]
	private float dbThreshold = -30f;

	[Tooltip("Le temps en secondes pendant lequel le joueur doit parler en continu pour valider la détection.")]
	[SerializeField]
	private float timeRequiredToSpeak = 1.0f;

	[Tooltip("Mot-clé du microphone à utiliser (ex: 'Quest', 'Oculus').")]
	[SerializeField]
	private string preferredMicKeyword = "Oculus";

	[SerializeField]
	private float lengthDisplayFeedback = 1.0f;

	[Header("Visual Feedback")]
	[SerializeField]
	private GameObject feedbackImage;
	[SerializeField]
	private GameObject feedbackIsSpeaking;

	private int index = 0;
	private float currentSpeakingTime = 0f;
	private float currentSilentTime = 0f;

	private AudioClip micClip;
	private string micDevice;
	private readonly int sampleWindow = 256;

	private bool hasInterrupted = false;
	private int interruptionCount = 0;

	// Start is called before the first frame update
	void Start()
	{
		if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
			Permission.RequestUserPermission(Permission.Microphone);

		if (Microphone.devices.Length > 0)
		{
			// 1. Afficher tous les microphones pour le débogage
			Debug.Log("Microphones disponibles :");
			foreach (var device in Microphone.devices)
			{
				Debug.Log("[Emery] - " + device);
			}

			// 2. Par défaut, on prend le premier
			micDevice = Microphone.devices[0];

			// 3. Chercher le microphone du casque via le mot-clé
			if (!string.IsNullOrEmpty(preferredMicKeyword))
			{
				foreach (var device in Microphone.devices)
				{
					if (device.ToLower().Contains(preferredMicKeyword.ToLower()))
					{
						micDevice = device;
						break; // On a trouvé le bon, on arrête la boucle
					}
				}
			}

			Debug.Log("Microphone sélectionné : " + micDevice);
			
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
		if (SpeakingInterruption.Instance == null)
			return;

		if (SpeakingInterruption.Instance.GetLastInterrupter() == index)
		{
			if (!hasInterrupted)
			{
				hasInterrupted = true;
				ShowFeedback();
				interruptionCount++;
				if (interruptionCount > 3)
				{
					SpeakingInterruption.Instance.RPC_ShowFeedbackInterruptionToAll(index);
					interruptionCount = 0; // Réinitialiser le compteur après le feedback
				}
			} 
		} else 
			hasInterrupted = false;


		if (micClip != null)
		{
			float currentVolume = GetMicrophoneVolume();

			// Calcul des décibels en échelle dBFS (0 dB = Volume Max, nombres négatifs pour plus faible)
			float currentDb = -140f; // Considérer -80 dB comme du silence absolu pour éviter -Infinity
			if (currentVolume > 0)
			{
				currentDb = 20f * Mathf.Log10(currentVolume);
			}

			// Si le volume actuel dépasse (est supérieur à) notre seuil cible (ex: > -30 dB)
			if (currentDb >= dbThreshold)
			{
				currentSpeakingTime += Time.deltaTime;

				if (currentSpeakingTime >= timeRequiredToSpeak)
				{
					if (!isSpeaking)
					{
						currentSilentTime = 0f; // Réinitialiser le temps de silence
						isSpeaking = true;
						SpeakingInterruption.Instance.SetIsSpeaking(index, isSpeaking);
					}
				}
			}
			else
			{
				if (currentSilentTime >= timeRequiredToSpeak)
				{
					// Si le son baisse, on réinitialise
					currentSpeakingTime = 0f;
					if (isSpeaking)
					{
						isSpeaking = false;
						SpeakingInterruption.Instance.SetIsSpeaking(index, isSpeaking);
					}
				}
				else
				{
					currentSilentTime += Time.deltaTime;
				}
			}
		}

		feedbackIsSpeaking.SetActive(isSpeaking);
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
		DataFeedbacks.Instance.AddFeedbackLog(1, "Affichage du feedback");
		StartCoroutine(FeedbackCoroutine());
	}

	private IEnumerator FeedbackCoroutine()
	{
		yield return new WaitForSeconds(lengthDisplayFeedback);
		feedbackImage.SetActive(false);
		DataFeedbacks.Instance.AddFeedbackLog(1, "Désactivation du feedback");
		SpeakingInterruption.Instance.ResetLastInterrupter();
	}

	public void SetIndex(int newIndex)
	{
		index = newIndex;
	}
}
