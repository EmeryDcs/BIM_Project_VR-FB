using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;

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

	[SerializeField]
	private int index = 0;

	private float currentSpeakingTime = 0f;

	// Amplitude de référence permettant de calibrer l'échelle en décibels positifs (0 dB = silence).
	// A ajuster si le microphone est très peu ou très sensible.
	private float refAmplitude = 0.0001f; 

	private AudioClip micClip;
	private string micDevice;
	private readonly int sampleWindow = 256;

	[Tooltip("Mot-clé du microphone à utiliser (ex: 'Quest', 'Oculus').")]
	[SerializeField]
	private string preferredMicKeyword = "Oculus";

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

		if (!SpeakingInterruption.Instance.IsSpeakingSetInTab(index))
			SpeakingInterruption.Instance.AffectSpeakerToTab(index, this);

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
					Debug.Log("[Emery] Player " + index + " is speaking! (Volume: " + currentDb.ToString("F1") + " dB)");
					isSpeaking = true;
				}
			}
			else
			{
				// Si le son baisse, on réinitialise
				currentSpeakingTime = 0f;
				isSpeaking = false;
				Debug.Log("[Emery] Player " + index + " is not speaking! (Volume: " + currentDb.ToString("F1") + " dB)");
			}
		}
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

	public void SetIndex(int newIndex)
	{
		index = newIndex;
	}
}
